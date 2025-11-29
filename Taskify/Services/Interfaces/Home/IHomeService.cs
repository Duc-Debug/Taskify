using Taskify.Models;
namespace Taskify.Services
{
    public interface IHomeService
    {
        Task<LandingPageViewModel> GetLandingPageDataAsync();
    }
}
