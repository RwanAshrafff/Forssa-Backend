namespace Forssa.Application.Interfaces;

public interface IUserProgressService
{
    Task<bool> CompleteLessonAsync(string supabaseId, Guid lessonId);
    Task<bool> CompleteQuizAsync(string supabaseId, Guid quizId, decimal score, bool isPassed);
    Task<int> GetUserPointsAsync(string supabaseId);
}
