namespace Taskify.Services
{
    public interface ISkillEvaluationService
    {
        Task CheckAndSuggestSkillUpdateAsync(Guid taskId, Guid userId);
        Task<Dictionary<string, int>> EvaluateAllSkillsAsync(Guid userId);
    }
}
