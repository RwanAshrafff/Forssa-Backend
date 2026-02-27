using AutoMapper;
using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Forssa.Domain.Entities;
using Forssa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Forssa.Infrastructure.Services;

public class UserService : BaseService<User, UserDTO>, IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(IBaseRepository<User> repository, IMapper mapper, ApplicationDbContext context)
        : base(repository, mapper)
    {
        _context = context;
    }

    public async Task<UserDTO> SyncUserAsync(string supabaseId, string email, string? role = null, string? fullName = null, string? bio = null, string? avatarUrl = null, string? username = null, string? instructorPosition = null)
    {
        // Use direct DbContext to avoid any caching/tracking issues
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);

        // Standardize input role to lowercase
        string? standardizedInputRole = role?.Trim().ToLowerInvariant();
        bool isRequestingInstructor = standardizedInputRole == "instructor";
        
        // Determine the role the user SHOULD have in our DB
        string currentDbRole = existingUser?.Role?.ToLowerInvariant() ?? "student";
        string effectiveRole = standardizedInputRole ?? currentDbRole;

        // Force role to student if they are requesting instructor but aren't approved yet
        if (isRequestingInstructor && currentDbRole != "instructor")
        {
            effectiveRole = "student";
        }

        if (existingUser == null)
        {
            // New user - create with effective role
            existingUser = new User(supabaseId, email, effectiveRole);
            _context.Users.Add(existingUser);
        }
        else
        {
            // Update email if changed
            if (!string.IsNullOrEmpty(email) && existingUser.Email != email)
            {
                existingUser.UpdateEmail(email);
            }

            // Role update logic
            if (!string.IsNullOrEmpty(standardizedInputRole) && existingUser.Role != effectiveRole)
            {
                existingUser.UpdateRole(effectiveRole);
            }
        }

        // Handle Instructor Approval Request if they wanted to be an instructor
        if (isRequestingInstructor && existingUser.Role != "instructor")
        {
            var pendingRequest = await _context.InstructorApprovalRequests
                .FirstOrDefaultAsync(r => r.UserId == existingUser.Id && r.Status == "Pending");

            if (pendingRequest == null)
            {
                pendingRequest = new InstructorApprovalRequest
                {
                    UserId = existingUser.Id,
                    RequestedPosition = string.IsNullOrWhiteSpace(instructorPosition) ? null : instructorPosition.Trim(),
                    Status = "Pending"
                };
                _context.InstructorApprovalRequests.Add(pendingRequest);
            }
            else if (!string.IsNullOrWhiteSpace(instructorPosition))
            {
                // Update position if provided in a later sync
                pendingRequest.RequestedPosition = instructorPosition.Trim();
            }
        }

        // --- POSITION HANDLING ---
        // Preserve position if they are an instructor OR if they are currently requesting it
        string? effectivePosition = NormalizePositionForRole(existingUser.Role, instructorPosition ?? existingUser.InstructorPosition, isRequestingInstructor);

        // Update profile properties (preserving existing if new ones are null)
        existingUser.UpdateProfile(
            fullName ?? existingUser.FullName, 
            bio ?? existingUser.Bio, 
            avatarUrl ?? existingUser.AvatarUrl, 
            username ?? existingUser.Username, 
            effectivePosition);
        
        await _context.SaveChangesAsync();

        var userDto = _mapper.Map<UserDTO>(existingUser);
        
        // Populate Pending/Approved statuses
        userDto.HasPendingInstructorRequest = await _context.InstructorApprovalRequests
            .AnyAsync(r => r.UserId == existingUser.Id && r.Status == "Pending");

        userDto.HasPendingFinancialAid = await _context.FinancialAidDocuments
            .AnyAsync(r => r.UserId == existingUser.Id && r.Status == "Pending");

        userDto.HasApprovedFinancialAid = await _context.FinancialAidDocuments
            .AnyAsync(r => r.UserId == existingUser.Id && r.Status == "Approved");

        userDto.HasPendingSuccessStory = await _context.SuccessStories
            .AnyAsync(r => r.UserId == existingUser.Id && r.ApprovalStatus == "Pending");

        return userDto;
    }

    private static string? NormalizePositionForRole(string role, string? instructorPosition, bool isRequestingInstructor = false)
    {
        var normalizedRole = role.Trim().ToLowerInvariant();
        var normalizedPosition = string.IsNullOrWhiteSpace(instructorPosition)
            ? null
            : instructorPosition.Trim();

        if (normalizedRole == "instructor" || isRequestingInstructor)
        {
            return normalizedPosition;
        }

        return null;
    }
}

