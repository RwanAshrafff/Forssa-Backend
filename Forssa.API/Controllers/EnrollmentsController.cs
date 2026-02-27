using Forssa.Infrastructure.Data;
using Forssa.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EnrollmentsController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public EnrollmentsController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("stripe-config")]
    public IActionResult GetStripeConfig()
    {
        var publishableKey = _configuration["Stripe:PublishableKey"] ?? string.Empty;
        var currency = (_configuration["Stripe:Currency"] ?? "usd").ToLowerInvariant();

        return HandleResult(new { publishableKey, currency }, "Stripe config retrieved successfully.");
    }

    [HttpPost("create-payment-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(supabaseId))
                return BadRequestResult("User ID not found in token.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);
            if (user == null)
                return NotFoundResult("User not found.");

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId);
            if (course == null)
                return NotFoundResult("Course not found.");

            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == user.Id && e.CourseId == request.CourseId);

            if (alreadyEnrolled)
                return BadRequestResult("You are already enrolled in this course.");

            var hasApprovedFinancialAid = await _context.FinancialAidDocuments
                .AnyAsync(f => f.UserId == user.Id && f.Status == "Approved");

            var amount = hasApprovedFinancialAid ? 0 : ToMinorUnits(course.Price);
            if (amount <= 0)
            {
                var enrollment = new Enrollment
                {
                    StudentId = user.Id,
                    CourseId = request.CourseId,
                    IsPointsAwarded = false
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                return HandleResult(new
                {
                    requiresPayment = false,
                    enrolled = true,
                    courseId = request.CourseId
                }, "Free course enrolled successfully.");
            }

            var secretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
                return InternalServerError("Stripe secret key is not configured.");

            StripeConfiguration.ApiKey = secretKey;
            var currency = (_configuration["Stripe:Currency"] ?? "usd").ToLowerInvariant();

            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = amount,
                Currency = currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = new Dictionary<string, string>
                {
                    ["course_id"] = request.CourseId.ToString(),
                    ["supabase_id"] = supabaseId
                }
            });

            var publishableKey = _configuration["Stripe:PublishableKey"] ?? string.Empty;

            return HandleResult(new
            {
                requiresPayment = true,
                amount,
                currency,
                paymentIntentId = paymentIntent.Id,
                clientSecret = paymentIntent.ClientSecret,
                publishableKey,
                courseId = request.CourseId
            }, "Payment intent created successfully.");
        }
        catch (StripeException ex)
        {
            return BadRequestResult($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmEnrollment([FromBody] ConfirmEnrollmentRequest request)
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(supabaseId))
                return BadRequestResult("User ID not found in token.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);
            if (user == null)
                return NotFoundResult("User not found.");

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId);
            if (course == null)
                return NotFoundResult("Course not found.");

            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == user.Id && e.CourseId == request.CourseId);

            if (alreadyEnrolled)
                return HandleResult(new { enrolled = true, courseId = request.CourseId }, "Already enrolled.");

            var hasApprovedFinancialAid = await _context.FinancialAidDocuments
                .AnyAsync(f => f.UserId == user.Id && f.Status == "Approved");

            var amount = hasApprovedFinancialAid ? 0 : ToMinorUnits(course.Price);
            if (amount > 0)
            {
                if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
                    return BadRequestResult("PaymentIntentId is required for paid courses.");

                var secretKey = _configuration["Stripe:SecretKey"];
                if (string.IsNullOrWhiteSpace(secretKey))
                    return InternalServerError("Stripe secret key is not configured.");

                StripeConfiguration.ApiKey = secretKey;
                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.GetAsync(request.PaymentIntentId);

                if (!string.Equals(paymentIntent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                    return BadRequestResult("Payment is not completed.");

                var metadataCourseId = paymentIntent.Metadata.TryGetValue("course_id", out var piCourseId)
                    ? piCourseId
                    : string.Empty;
                var metadataSupabaseId = paymentIntent.Metadata.TryGetValue("supabase_id", out var piSupabaseId)
                    ? piSupabaseId
                    : string.Empty;

                if (!string.Equals(metadataCourseId, request.CourseId.ToString(), StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(metadataSupabaseId, supabaseId, StringComparison.Ordinal))
                {
                    return BadRequestResult("Payment intent does not match this enrollment request.");
                }

                if (paymentIntent.Amount != amount)
                    return BadRequestResult("Paid amount does not match course price.");
            }

            var enrollment = new Enrollment
            {
                StudentId = user.Id,
                CourseId = request.CourseId,
                IsPointsAwarded = false
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return HandleResult(new { enrolled = true, courseId = request.CourseId }, "Enrollment confirmed successfully.");
        }
        catch (StripeException ex)
        {
            return BadRequestResult($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    private static long ToMinorUnits(decimal majorAmount)
    {
        var normalized = majorAmount < 0 ? 0 : majorAmount;
        return (long)decimal.Round(normalized * 100m, 0, MidpointRounding.AwayFromZero);
    }
}

public record CreatePaymentIntentRequest(Guid CourseId);
public record ConfirmEnrollmentRequest(Guid CourseId, string? PaymentIntentId);
