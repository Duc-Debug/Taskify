namespace Taskify.Services
{
    public interface IPerformanceService
    {
        Task<int> CalculateSuccessProbabilityAsync(Guid userId, string taskTitle, string taskDescription);
    }
}
