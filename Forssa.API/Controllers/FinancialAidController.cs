using Forssa.Domain.Entities;
using Forssa.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Authorize]
[Route("api/financial-aid")]
public class FinancialAidController : BaseController
{
    private readonly ApplicationDbContext _context;

    public FinancialAidController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromBody] UploadFinancialAidDocumentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.DocumentUrl))
                return BadRequestResult("DocumentUrl is required.");

            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(supabaseId))
                return BadRequestResult("User ID not found in token.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);
            if (user == null)
                return NotFoundResult("User not found.");

            var document = new FinancialAidDocument
            {
                UserId = user.Id,
                DocumentUrl = request.DocumentUrl.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Status = "Pending"
            };

            _context.FinancialAidDocuments.Add(document);
            await _context.SaveChangesAsync();

            return HandleResult(new
            {
                documentId = document.Id,
                status = document.Status
            }, "Financial aid document uploaded and pending review.");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyDocuments()
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

            var documents = await _context.FinancialAidDocuments
                .Where(d => d.UserId == user.Id)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.DocumentUrl,
                    d.Description,
                    d.Status,
                    d.ReviewNote,
                    d.ReviewedAt
                })
                .ToListAsync();

            return HandleResult(documents, "Financial aid documents retrieved successfully.");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
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

            var latestDocument = await _context.FinancialAidDocuments
                .Where(d => d.UserId == user.Id)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new { d.Status })
                .FirstOrDefaultAsync();

            return HandleResult(new
            {
                status = latestDocument?.Status,
                isApproved = latestDocument?.Status == "Approved",
                isPending = latestDocument?.Status == "Pending"
            }, "Financial aid status retrieved.");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }
}

public record UploadFinancialAidDocumentRequest(string DocumentUrl, string? Description);
