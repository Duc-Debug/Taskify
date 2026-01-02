using Taskify.Models;

namespace Taskify.Services
{
    public interface IGeminiService
    {
        Task<AiBoardPlan> GenerateBoardPlanAsync(string projectPrompt, List<User> teamMembers);
    }
}
