using Forssa.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Forssa.Application.DTOs;

namespace Forssa.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CertificatesController : BaseController
{
    private readonly ICertificateService _certificateService;

    public CertificatesController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [HttpGet("completed-courses")]
    public async Task<IActionResult> GetCompletedCourses()
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(supabaseId))
                return BadRequestResult("User ID (sub) not found in token.");

            var courses = await _certificateService.GetCompletedCoursesAsync(supabaseId);

            var response = courses.Select(c => new
            {
                c.CourseId,
                c.CourseTitle,
                c.CompletedAt,
                CertificateUrl = Url.Action(nameof(GenerateCourseCertificate), "Certificates", new { courseId = c.CourseId }, Request.Scheme)
            });

            return HandleResult(response, "Completed courses retrieved successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [HttpGet("course/{courseId:guid}")]
    public async Task<IActionResult> GenerateCourseCertificate(
        Guid courseId,
        [FromQuery] string? studentFullName = null,
        [FromQuery] string? instructorFullName = null,
        [FromQuery] string? instructorPosition = null,
        [FromQuery] string? courseTitle = null)
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(supabaseId))
                return BadRequestResult("User ID (sub) not found in token.");

            var input = new CertificateTemplateInputDTO
            {
                StudentFullName = FirstNonEmpty(studentFullName, ReadHeader("X-Student-Full-Name"), ReadHeader("studentFullName")),
                InstructorFullName = FirstNonEmpty(instructorFullName, ReadHeader("X-Instructor-Full-Name"), ReadHeader("instructorFullName")),
                InstructorPosition = FirstNonEmpty(instructorPosition, ReadHeader("X-Instructor-Position"), ReadHeader("instructorPosition")),
                CourseTitle = FirstNonEmpty(courseTitle, ReadHeader("X-Course-Title"), ReadHeader("courseTitle"))
            };

            var certificate = await _certificateService.GenerateCourseCertificateAsync(supabaseId, courseId, input);

            return File(certificate.Content, certificate.ContentType, certificate.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenResult(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    private string? ReadHeader(string key)
    {
        if (!Request.Headers.TryGetValue(key, out var values))
            return null;

        var value = values.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }
}
