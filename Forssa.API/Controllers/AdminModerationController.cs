using Forssa.Infrastructure.Data;
using Forssa.API.Security;
using Forssa.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/moderation")]
public class AdminModerationController : BaseController
{
    private readonly ApplicationDbContext _context;

    public AdminModerationController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("instructor-requests/pending")]
    public async Task<IActionResult> GetPendingInstructorRequests()
    {
        if (!TryValidateAdmin(out var unauthorizedResult, out _))
            return unauthorizedResult;

        var requests = await _context.InstructorApprovalRequests
            .Where(r => r.Status == "Pending")
            .Join(_context.Users,
                r => r.UserId,
                u => u.Id,
                (r, u) => new
                {
                    r.Id,
                    r.UserId,
                    u.Email,
                    u.FullName,
                    r.RequestedPosition,
                    r.Status,
                    r.CreatedAt
                })
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        return HandleResult(requests, "Pending instructor requests retrieved.");
    }

    [HttpPost("instructor-requests/{requestId:guid}/approve")]
    public async Task<IActionResult> ApproveInstructorRequest(Guid requestId, [FromBody] AdminReviewRequest? request)
    {
        if (!TryValidateAdmin(out var unauthorizedResult, out var adminEmail))
            return unauthorizedResult;

        var approvalRequest = await _context.InstructorApprovalRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (approvalRequest == null)
            return NotFoundResult("Instructor request not found.");

        if (approvalRequest.Status != "Pending")
            return BadRequestResult("Instructor request has already been reviewed.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == approvalRequest.UserId);
        if (user == null)
            return NotFoundResult("User not found.");

        user.UpdateRole("instructor");
        var position = string.IsNullOrWhiteSpace(request?.InstructorPositionOverride)
            ? approvalRequest.RequestedPosition
            : request!.InstructorPositionOverride!.Trim();
        user.UpdateProfile(user.FullName, user.Bio, user.AvatarUrl, user.Username, position);

        approvalRequest.Status = "Approved";
        approvalRequest.ReviewedByAdminEmail = adminEmail;
        approvalRequest.ReviewedAt = DateTime.UtcNow;
        approvalRequest.ReviewNote = string.IsNullOrWhiteSpace(request?.ReviewNote) ? null : request!.ReviewNote!.Trim();

        await _context.SaveChangesAsync();

        // --- NEW: Securely Update Supabase Metadata via SQL ---
        try
        {
            // Update the auth.users table directly to set the role and remove the pending flag
            // This ensures the middleware immediately recognizes the new role
            var sql = @"
                UPDATE auth.users 
                SET raw_user_metadata = (
                    coalesce(raw_user_metadata, '{}'::jsonb) || jsonb_build_object('role', 'instructor')
                ) - 'is_pending_instructor'
                WHERE id = {0}::uuid";

            await _context.Database.ExecuteSqlRawAsync(sql, user.SupabaseId);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the whole request since the DB role is already updated
            Console.WriteLine($"Warning: Failed to update Supabase metadata for user {user.SupabaseId}: {ex.Message}");
        }
        // -----------------------------------------------------

        return HandleResult(new { requestId, status = approvalRequest.Status }, "Instructor request approved.");
    }

    [HttpPost("instructor-requests/{requestId:guid}/reject")]
    public async Task<IActionResult> RejectInstructorRequest(Guid requestId, [FromBody] AdminReviewRequest? request)
    {
        if (!TryValidateAdmin(out var unauthorizedResult, out var adminEmail))
            return unauthorizedResult;

        var approvalRequest = await _context.InstructorApprovalRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (approvalRequest == null)
            return NotFoundResult("Instructor request not found.");

        if (approvalRequest.Status != "Pending")
            return BadRequestResult("Instructor request has already been reviewed.");

        approvalRequest.Status = "Rejected";
        approvalRequest.ReviewedByAdminEmail = adminEmail;
        approvalRequest.ReviewedAt = DateTime.UtcNow;
        approvalRequest.ReviewNote = string.IsNullOrWhiteSpace(request?.ReviewNote) ? null : request!.ReviewNote!.Trim();

        await _context.SaveChangesAsync();

        return HandleResult(new { requestId, status = approvalRequest.Status }, "Instructor request rejected.");
    }

    [HttpGet("financial-aid/pending")]
    public async Task<IActionResult> GetPendingFinancialAidDocuments()
    {
        if (!TryValidateAdmin(out var unauthorizedResult, out _))
            return unauthorizedResult;

        var documents = await _context.FinancialAidDocuments
            .Where(d => d.Status == "Pending")
            .Join(_context.Users,
                d => d.UserId,
                u => u.Id,
                (d, u) => new
                {
                    d.Id,
                    d.UserId,
                    u.Email,
                    u.FullName,
                    d.DocumentUrl,
                    d.Description,
                    d.Status,
                    d.CreatedAt
                })
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();

        return HandleResult(documents, "Pending financial aid documents retrieved.");
    }

    [HttpPost("financial-aid/{documentId:guid}/approve")]
    public async Task<IActionResult> ApproveFinancialAidDocument(Guid documentId, [FromBody] AdminReviewRequest? request)
    {
        if (!TryValidateAdmin(out var unauthorizedResult, out var adminEmail))
            return unauthorizedResult;

        var document = await _context.FinancialAidDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
        if (document == null)
            return NotFoundResult("Financial aid document not found.");

        if (document.Status != "Pending")
            return BadRequestResult("Financial aid document has already been reviewed.");

        document.Status = "Approved";
        document.ReviewedByAdminEmail = adminEmail;
        document.ReviewedAt = DateTime.UtcNow;
        document.ReviewNote = string.IsNullOrWhiteSpace(request?.ReviewNote) ? null : request!.ReviewNote!.Trim();

        await _context.SaveChangesAsync();

        return HandleResult(new { documentId, status = document.Status }, "Financial aid document approved.");
    }

    [HttpPost("financial-aid/{documentId:guid}/reject")]
    public async Task<IActionResult> RejectFinancialAidDocument(Guid documentId, [FromBody] AdminReviewRequest? request)
    {
        if (!TryValidateAdmin(out var unauthorizedResult, out var adminEmail))
            return unauthorizedResult;

        var document = await _context.FinancialAidDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
        if (document == null)
            return NotFoundResult("Financial aid document not found.");

        if (document.Status != "Pending")
            return BadRequestResult("Financial aid document has already been reviewed.");

        document.Status = "Rejected";
        document.ReviewedByAdminEmail = adminEmail;
        document.ReviewedAt = DateTime.UtcNow;
        document.ReviewNote = string.IsNullOrWhiteSpace(request?.ReviewNote) ? null : request!.ReviewNote!.Trim();

        await _context.SaveChangesAsync();

        return HandleResult(new { documentId, status = document.Status }, "Financial aid document rejected.");
    }

    [HttpGet("success-stories/pending")]
    public async Task<IActionResult> GetPendingSuccessStories()
    {
        if (!TryValidateAdmin(out var unauthorizedResult, out _))
            return unauthorizedResult;

        var stories = await _context.SuccessStories
            .Where(s => s.ApprovalStatus == "Pending")
            .Join(_context.Users,
                s => s.UserId,
                u => u.Id,
                (s, u) => new
                {
                    s.Id,
                    s.UserId,
                    u.Email,
                    u.FullName,
                    s.Content,
                    s.Rating,
                    s.CourseId,
                    s.Achievement,
                    s.CreatedAt
                })
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        return HandleResult(stories, "Pending success stories retrieved.");
    }

    [HttpPost("success-stories/{storyId:guid}/approve")]
    public async Task<IActionResult> ApproveSuccessStory(Guid storyId, [FromBody] AdminReviewRequest? request)
    {
        if (!TryValidateAdmin(out var unauthorizedResult, out var adminEmail))
            return unauthorizedResult;

        var story = await _context.SuccessStories.FirstOrDefaultAsync(s => s.Id == storyId);
        if (story == null)
            return NotFoundResult("Success story not found.");

        if (story.ApprovalStatus != "Pending")
            return BadRequestResult("Success story has already been reviewed.");

        story.ApprovalStatus = "Approved";
        story.ReviewedByAdminEmail = adminEmail;
        story.ReviewedAt = DateTime.UtcNow;
        story.ReviewNote = string.IsNullOrWhiteSpace(request?.ReviewNote) ? null : request!.ReviewNote!.Trim();

        await _context.SaveChangesAsync();

        return HandleResult(new { storyId, status = story.ApprovalStatus }, "Success story approved.");
    }

    [HttpPost("success-stories/{storyId:guid}/reject")]
    public async Task<IActionResult> RejectSuccessStory(Guid storyId, [FromBody] AdminReviewRequest? request)
    {
        if (!TryValidateAdmin(out var unauthorizedResult, out var adminEmail))
            return unauthorizedResult;

        var story = await _context.SuccessStories.FirstOrDefaultAsync(s => s.Id == storyId);
        if (story == null)
            return NotFoundResult("Success story not found.");

        if (story.ApprovalStatus != "Pending")
            return BadRequestResult("Success story has already been reviewed.");

        story.ApprovalStatus = "Rejected";
        story.ReviewedByAdminEmail = adminEmail;
        story.ReviewedAt = DateTime.UtcNow;
        story.ReviewNote = string.IsNullOrWhiteSpace(request?.ReviewNote) ? null : request!.ReviewNote!.Trim();

        await _context.SaveChangesAsync();

        return HandleResult(new { storyId, status = story.ApprovalStatus }, "Success story rejected.");
    }

    private bool TryValidateAdmin(out IActionResult unauthorizedResult, out string adminEmail)
    {
        unauthorizedResult = UnauthorizedResult("Unauthorized admin access.");
        adminEmail = string.Empty;

        var tokenEmail = User.FindFirst(ClaimTypes.Email)?.Value
                        ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(tokenEmail))
        {
            unauthorizedResult = UnauthorizedResult("Email claim is missing in token.");
            return false;
        }

        var normalizedTokenEmail = tokenEmail.Trim().ToLowerInvariant();
        var adminRecord = _context.Set<AdminCredential>()
            .AsNoTracking()
            .FirstOrDefault(a => a.Email == normalizedTokenEmail && a.IsActive);

        if (adminRecord == null)
        {
            unauthorizedResult = UnauthorizedResult("Token user is not configured admin.");
            return false;
        }

        if (!Request.Headers.TryGetValue("X-Admin-Password", out var passwordHeader)
            || string.IsNullOrWhiteSpace(passwordHeader))
        {
            unauthorizedResult = UnauthorizedResult("X-Admin-Password header is required.");
            return false;
        }

        if (!PasswordHashing.VerifyPassword(passwordHeader.ToString(), adminRecord.PasswordHash))
        {
            unauthorizedResult = UnauthorizedResult("Invalid admin password.");
            return false;
        }

        adminEmail = adminRecord.Email;
        return true;
    }
}

public record AdminReviewRequest(string? ReviewNote, string? InstructorPositionOverride);
