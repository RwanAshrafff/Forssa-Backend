using AutoMapper;
using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Forssa.Domain.Entities;
using Forssa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Forssa.Infrastructure.Services;

public class SuccessStoryService : ISuccessStoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public SuccessStoryService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SuccessStoryDTO>> GetAllAsync()
    {
        var stories = await _context.SuccessStories
            .Include(s => s.User)
            .Include(s => s.Course)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<SuccessStoryDTO>>(stories);
    }

    public async Task<SuccessStoryDTO> CreateAsync(string supabaseId, CreateSuccessStoryDTO createDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Points < 100)
        {
            throw new InvalidOperationException("You need at least 100 points to share a success story.");
        }

        var existingStory = await _context.SuccessStories.AnyAsync(s => s.UserId == user.Id);
        if (existingStory)
        {
            throw new InvalidOperationException("You have already shared a success story.");
        }

        var story = _mapper.Map<SuccessStory>(createDto);
        story.UserId = user.Id;
        story.CreatedAt = DateTime.UtcNow;

        _context.SuccessStories.Add(story);
        await _context.SaveChangesAsync();

        // Load relationships for the return DTO
        await _context.Entry(story).Reference(s => s.User).LoadAsync();
        if (story.CourseId.HasValue)
        {
            await _context.Entry(story).Reference(s => s.Course).LoadAsync();
        }

        return _mapper.Map<SuccessStoryDTO>(story);
    }

    public async Task<bool> CanUserSubmitStoryAsync(string supabaseId)
    {
        var user = await _context.Users
            .Where(u => u.SupabaseId == supabaseId)
            .Select(u => new { u.Id, u.Points })
            .FirstOrDefaultAsync();

        if (user == null || user.Points < 100) return false;

        var alreadySubmitted = await _context.SuccessStories.AnyAsync(s => s.UserId == user.Id);
        return !alreadySubmitted;
    }
}
