using Forssa.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Forssa.Domain.Entities;

namespace Forssa.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuizzesController : BaseController
{
    private readonly ApplicationDbContext _context;

    public QuizzesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Quizzes/{id} - For students to take the quiz (no correct answers exposed)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuiz(Guid id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qn => qn.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
            return NotFoundResult("Quiz not found");

        // Strip IsCorrect from options so students can't cheat
        var result = new
        {
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.PassingScore,
            quiz.Position,
            Questions = quiz.Questions.Select(q => new
            {
                q.Id,
                q.Text,
                q.Points,
                Options = q.Options.Select(o => new
                {
                    o.Id,
                    o.Text
                    // IsCorrect intentionally excluded
                })
            })
        };

        return HandleResult(result, "Quiz retrieved");
    }

    // POST: api/Quizzes - Instructor creates a quiz
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizRequest request)
    {
        var quiz = new Quiz
        {
            ModuleId = request.ModuleId,
            Title = request.Title,
            Description = request.Description,
            PassingScore = request.PassingScore,
            Position = request.Position
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        // Add questions and options
        foreach (var qReq in request.Questions)
        {
            var question = new Question
            {
                QuizId = quiz.Id,
                Text = qReq.Text,
                Points = qReq.Points
            };
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            foreach (var oReq in qReq.Options)
            {
                var option = new Option
                {
                    QuestionId = question.Id,
                    Text = oReq.Text,
                    IsCorrect = oReq.IsCorrect
                };
                _context.Options.Add(option);
            }
        }

        await _context.SaveChangesAsync();

        return Created(new { quiz.Id }, "Quiz created successfully");
    }

    // POST: api/Quizzes/{id}/submit - Student submits quiz answers
    [HttpPost("{id}/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitQuiz(Guid id, [FromBody] SubmitQuizRequest request)
    {
        // Get internal user id from claims
        var supabaseId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (supabaseId == null) return UnauthorizedResult("Not authenticated");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);
        if (user == null) return NotFoundResult("User not found");

        // Load quiz with correct answers
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qn => qn.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null) return NotFoundResult("Quiz not found");

        // Calculate score
        int totalPoints = quiz.Questions.Sum(q => q.Points);
        int earnedPoints = 0;

        foreach (var answer in request.Answers)
        {
            var question = quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId);
            if (selectedOption != null && selectedOption.IsCorrect)
            {
                earnedPoints += question.Points;
            }
        }

        decimal scorePercent = totalPoints > 0 ? (decimal)earnedPoints / totalPoints * 100 : 0;
        bool isPassed = scorePercent >= quiz.PassingScore;

        // Save submission
        var submission = new QuizSubmission
        {
            UserId = user.Id,
            QuizId = id,
            Score = scorePercent,
            IsPassed = isPassed,
            SubmittedAt = DateTime.UtcNow,
            Feedback = isPassed ? "Excellent work! You've mastered this topic." : "Close, but you need a bit more practice. Review the lessons and try again!"
        };

        _context.QuizSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        return HandleResult(new
        {
            Score = scorePercent,
            IsPassed = isPassed,
            EarnedPoints = earnedPoints,
            TotalPoints = totalPoints,
            PassingScore = quiz.PassingScore,
            Feedback = isPassed ? "Excellent work! You've mastered this topic." : "Close, but you need a bit more practice. Review the lessons and try again!"
        }, isPassed ? "Congratulations! You passed the quiz!" : "Quiz submitted. You did not meet the passing score.");
    }

    // GET: api/Quizzes/{id}/latest-submission - Get latest submission for the current user
    [HttpGet("{id}/latest-submission")]
    [Authorize]
    public async Task<IActionResult> GetLatestSubmission(Guid id)
    {
        var supabaseId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (supabaseId == null) return UnauthorizedResult("Not authenticated");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);
        if (user == null) return NotFoundResult("User not found");

        var submission = await _context.QuizSubmissions
            .Where(s => s.QuizId == id && s.UserId == user.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new
            {
                s.Score,
                s.IsPassed,
                s.SubmittedAt,
                s.Feedback
            })
            .FirstOrDefaultAsync();

        if (submission == null)
            return NotFoundResult("No submissions found");

        // Calculate points based on the quiz metadata since we don't store individual question results yet
        var quiz = await _context.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == id);
        int totalPoints = quiz?.Questions.Sum(q => q.Points) ?? 0;
        int earnedPoints = (int)Math.Round((submission.Score / 100) * totalPoints);

        return HandleResult(new
        {
            submission.Score,
            submission.IsPassed,
            submission.SubmittedAt,
            EarnedPoints = earnedPoints,
            TotalPoints = totalPoints,
            PassingScore = quiz?.PassingScore ?? 80,
            Feedback = submission.Feedback ?? (submission.IsPassed ? "Excellent work! You've mastered this topic." : "Close, but you need a bit more practice. Review the lessons and try again!")
        }, "Latest submission retrieved");
    }

    // GET: api/Quizzes/module/{moduleId} - Get all quizzes for a module
    [HttpGet("module/{moduleId}")]
    public async Task<IActionResult> GetQuizzesByModule(Guid moduleId)
    {
        var quizzes = await _context.Quizzes
            .Where(q => q.ModuleId == moduleId)
            .OrderBy(q => q.Position)
            .Select(q => new
            {
                q.Id,
                q.Title,
                q.Description,
                q.PassingScore,
                q.Position,
                QuestionCount = q.Questions.Count()
            })
            .ToListAsync();

        return HandleResult(quizzes, "Quizzes retrieved");
    }

    // GET: api/Quizzes/{id}/full - Instructor view with answers
    [HttpGet("{id}/full")]
    [Authorize]
    public async Task<IActionResult> GetQuizFull(Guid id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qn => qn.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null) return NotFoundResult("Quiz not found");

        return HandleResult(quiz, "Quiz retrieved");
    }
}

// Request DTOs
public class CreateQuizRequest
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal PassingScore { get; set; } = 80;
    public int Position { get; set; }
    public List<CreateQuestionRequest> Questions { get; set; } = new();
}

public class CreateQuestionRequest
{
    public string Text { get; set; } = "";
    public int Points { get; set; } = 1;
    public List<CreateOptionRequest> Options { get; set; } = new();
}

public class CreateOptionRequest
{
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
}

public class SubmitQuizRequest
{
    public List<QuizAnswer> Answers { get; set; } = new();
}

public class QuizAnswer
{
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
}
