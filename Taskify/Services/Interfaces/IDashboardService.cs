using Taskify.Models;

namespace Taskify.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(Guid userId);
    }
}
