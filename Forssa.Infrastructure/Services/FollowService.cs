using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Forssa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Forssa.Infrastructure.Services;

public class FollowService : IFollowService
{
    private readonly ApplicationDbContext _context;

    public FollowService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task FollowAsync(string currentSupabaseId, Guid targetUserId)
    {
        var currentUser = await GetUserBySupabaseIdAsync(currentSupabaseId);

        if (currentUser.Id == targetUserId)
            throw new InvalidOperationException("You cannot follow yourself.");

        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId)
            ?? throw new KeyNotFoundException("Target user not found.");

        var exists = await _context.UserFollows
            .AnyAsync(f => f.FollowerId == currentUser.Id && f.FollowedId == targetUserId);

        if (exists)
            return;

        _context.UserFollows.Add(new Domain.Entities.UserFollow
        {
            FollowerId = currentUser.Id,
            FollowedId = targetUserId
        });

        // Increment counts
        currentUser.IncrementFollowingCount();
        targetUser.IncrementFollowersCount();

        await _context.SaveChangesAsync();
    }

    public async Task UnfollowAsync(string currentSupabaseId, Guid targetUserId)
    {
        var currentUser = await GetUserBySupabaseIdAsync(currentSupabaseId);

        var follow = await _context.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FollowedId == targetUserId);

        if (follow == null)
            return;

        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);

        _context.UserFollows.Remove(follow);

        // Decrement counts
        currentUser.DecrementFollowingCount();
        if (targetUser != null)
            targetUser.DecrementFollowersCount();

        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<FollowUserDTO>> GetFollowingAsync(string currentSupabaseId)
    {
        var currentUser = await GetUserBySupabaseIdAsync(currentSupabaseId);

        return await _context.UserFollows
            .Where(f => f.FollowerId == currentUser.Id)
            .Join(_context.Users,
                f => f.FollowedId,
                u => u.Id,
                (f, u) => new FollowUserDTO
                {
                    UserId = u.Id,
                    SupabaseId = u.SupabaseId,
                    Role = u.Role,
                    FullName = u.FullName,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl,
                    InstructorPosition = u.InstructorPosition,
                    FollowedAt = f.CreatedAt
                })
            .OrderByDescending(x => x.FollowedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<FollowUserDTO>> GetFollowersAsync(string currentSupabaseId)
    {
        var currentUser = await GetUserBySupabaseIdAsync(currentSupabaseId);

        return await _context.UserFollows
            .Where(f => f.FollowedId == currentUser.Id)
            .Join(_context.Users,
                f => f.FollowerId,
                u => u.Id,
                (f, u) => new FollowUserDTO
                {
                    UserId = u.Id,
                    SupabaseId = u.SupabaseId,
                    Role = u.Role,
                    FullName = u.FullName,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl,
                    InstructorPosition = u.InstructorPosition,
                    FollowedAt = f.CreatedAt
                })
            .OrderByDescending(x => x.FollowedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<FollowUserDTO>> GetUserFollowingAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        return await _context.UserFollows
            .Where(f => f.FollowerId == userId)
            .Join(_context.Users,
                f => f.FollowedId,
                u => u.Id,
                (f, u) => new FollowUserDTO
                {
                    UserId = u.Id,
                    SupabaseId = u.SupabaseId,
                    Role = u.Role,
                    FullName = u.FullName,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl,
                    InstructorPosition = u.InstructorPosition,
                    FollowedAt = f.CreatedAt
                })
            .OrderByDescending(x => x.FollowedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<FollowUserDTO>> GetUserFollowersAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        return await _context.UserFollows
            .Where(f => f.FollowedId == userId)
            .Join(_context.Users,
                f => f.FollowerId,
                u => u.Id,
                (f, u) => new FollowUserDTO
                {
                    UserId = u.Id,
                    SupabaseId = u.SupabaseId,
                    Role = u.Role,
                    FullName = u.FullName,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl,
                    InstructorPosition = u.InstructorPosition,
                    FollowedAt = f.CreatedAt
                })
            .OrderByDescending(x => x.FollowedAt)
            .ToListAsync();
    }

    public async Task<HomeFeedDTO> GetHomeFeedAsync(string currentSupabaseId)
    {
        var currentUser = await GetUserBySupabaseIdAsync(currentSupabaseId);
        var normalizedRole = NormalizeRole(currentUser.Role);

        var followedUsers = await _context.UserFollows
            .Where(f => f.FollowerId == currentUser.Id)
            .Join(_context.Users,
                f => f.FollowedId,
                u => u.Id,
                (f, u) => new { u.Id, u.Role })
            .ToListAsync();

        var followedInstructorIds = followedUsers
            .Where(x => NormalizeRole(x.Role) == "Instructor")
            .Select(x => x.Id)
            .Distinct()
            .ToList();

        var followedStudentIds = followedUsers
            .Where(x => NormalizeRole(x.Role) == "Student")
            .Select(x => x.Id)
            .Distinct()
            .ToList();

        IReadOnlyList<HomeCourseFeedItemDTO> courses = Array.Empty<HomeCourseFeedItemDTO>();
        IReadOnlyList<HomeCertificateFeedItemDTO> certificates = Array.Empty<HomeCertificateFeedItemDTO>();

        if (followedInstructorIds.Count > 0)
        {
            courses = await _context.Courses
                .Where(c => followedInstructorIds.Contains(c.Instructor_id))
                .Join(_context.Users,
                    c => c.Instructor_id,
                    u => u.Id,
                    (c, u) => new HomeCourseFeedItemDTO
                    {
                        CourseId = c.Id,
                        CourseTitle = c.Title,
                        ThumbnailUrl = c.ThumbnailUrl,
                        CreatedAt = c.CreatedAt,
                        InstructorId = u.Id,
                        InstructorName = u.FullName,
                        InstructorUsername = u.Username
                    })
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        if (followedStudentIds.Count > 0)
        {
            var enrollments = await _context.Enrollments
                .Where(e => followedStudentIds.Contains(e.StudentId))
                .Select(e => new { e.StudentId, e.CourseId })
                .ToListAsync();

            if (enrollments.Count > 0)
            {
                var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();

                var courseLessons = await _context.Modules
                    .Where(m => courseIds.Contains(m.Course_id))
                    .SelectMany(m => m.Lessons.Select(l => new { m.Course_id, LessonId = l.Id }))
                    .ToListAsync();

                var lessonIds = courseLessons.Select(x => x.LessonId).Distinct().ToList();
                if (lessonIds.Count > 0)
                {
                    var completedProgress = await _context.LessonProgress
                        .Where(lp => followedStudentIds.Contains(lp.UserId) && lessonIds.Contains(lp.LessonId) && lp.IsCompleted)
                        .Select(lp => new { lp.UserId, lp.LessonId, lp.CompletedAt })
                        .ToListAsync();

                    var lessonsByCourse = courseLessons
                        .GroupBy(x => x.Course_id)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.LessonId).ToHashSet());

                    var completedLookup = completedProgress
                        .GroupBy(x => x.UserId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.GroupBy(p => p.LessonId)
                                  .ToDictionary(gg => gg.Key, gg => gg.Max(v => v.CompletedAt)));

                    var completedPairs = new List<(Guid StudentId, Guid CourseId, DateTime CompletedAt)>();

                    foreach (var enrollment in enrollments)
                    {
                        if (!lessonsByCourse.TryGetValue(enrollment.CourseId, out var requiredLessons) || requiredLessons.Count == 0)
                            continue;

                        if (!completedLookup.TryGetValue(enrollment.StudentId, out var studentProgress))
                            continue;

                        if (!requiredLessons.All(studentProgress.ContainsKey))
                            continue;

                        var completionAt = requiredLessons.Max(lessonId => studentProgress[lessonId]);
                        completedPairs.Add((enrollment.StudentId, enrollment.CourseId, completionAt));
                    }

                    if (completedPairs.Count > 0)
                    {
                        var studentIds = completedPairs.Select(x => x.StudentId).Distinct().ToList();
                        var completedCourseIds = completedPairs.Select(x => x.CourseId).Distinct().ToList();

                        var students = await _context.Users
                            .Where(u => studentIds.Contains(u.Id))
                            .Select(u => new { u.Id, u.FullName, u.Username })
                            .ToListAsync();

                        var certificateCourses = await _context.Courses
                            .Where(c => completedCourseIds.Contains(c.Id))
                            .Select(c => new { c.Id, c.Title })
                            .ToListAsync();

                        var studentMap = students.ToDictionary(s => s.Id, s => s);
                        var courseMap = certificateCourses.ToDictionary(c => c.Id, c => c);

                        certificates = completedPairs
                            .Where(x => studentMap.ContainsKey(x.StudentId) && courseMap.ContainsKey(x.CourseId))
                            .Select(x => new HomeCertificateFeedItemDTO
                            {
                                StudentId = x.StudentId,
                                StudentName = studentMap[x.StudentId].FullName,
                                StudentUsername = studentMap[x.StudentId].Username,
                                CourseId = x.CourseId,
                                CourseTitle = courseMap[x.CourseId].Title,
                                CompletedAt = x.CompletedAt
                            })
                            .OrderByDescending(x => x.CompletedAt)
                            .Take(50)
                            .ToList();
                    }
                }
            }
        }

        return new HomeFeedDTO
        {
            Role = normalizedRole,
            NewCourses = courses,
            NewCertificates = certificates
        };
    }

    public async Task<UserConnectionsDTO> GetUserConnectionsAsync(string? currentSupabaseId, Guid targetUserId)
    {
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId)
            ?? throw new KeyNotFoundException("User not found.");

        Guid? currentUserId = null;
        if (!string.IsNullOrWhiteSpace(currentSupabaseId))
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == currentSupabaseId);
            currentUserId = currentUser?.Id;
        }

        var isFollowedByCurrentUser = false;
        if (currentUserId.HasValue)
        {
            isFollowedByCurrentUser = await _context.UserFollows
                .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowedId == targetUserId);
        }

        var followers = await _context.UserFollows
            .Where(f => f.FollowedId == targetUserId)
            .Join(_context.Users,
                f => f.FollowerId,
                u => u.Id,
                (f, u) => new FollowUserDTO
                {
                    UserId = u.Id,
                    SupabaseId = u.SupabaseId,
                    Role = u.Role,
                    FullName = u.FullName,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl,
                    InstructorPosition = u.InstructorPosition,
                    FollowedAt = f.CreatedAt
                })
            .OrderByDescending(x => x.FollowedAt)
            .Take(50)
            .ToListAsync();

        var following = await _context.UserFollows
            .Where(f => f.FollowerId == targetUserId)
            .Join(_context.Users,
                f => f.FollowedId,
                u => u.Id,
                (f, u) => new FollowUserDTO
                {
                    UserId = u.Id,
                    SupabaseId = u.SupabaseId,
                    Role = u.Role,
                    FullName = u.FullName,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl,
                    InstructorPosition = u.InstructorPosition,
                    FollowedAt = f.CreatedAt
                })
            .OrderByDescending(x => x.FollowedAt)
            .Take(50)
            .ToListAsync();

        return new UserConnectionsDTO
        {
            UserId = targetUserId,
            FollowersCount = targetUser.FollowersCount,
            FollowingCount = targetUser.FollowingCount,
            Followers = followers,
            Following = following,
            IsFollowedByCurrentUser = isFollowedByCurrentUser
        };
    }

    private async Task<Domain.Entities.User> GetUserBySupabaseIdAsync(string supabaseId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId)
            ?? throw new KeyNotFoundException("Current user not found.");
    }

    private static string NormalizeRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return string.Empty;

        var trimmed = role.Trim();
        return char.ToUpper(trimmed[0]) + trimmed[1..].ToLower();
    }
}
