using global::Taskify.Models;
using Taskify.Models;

namespace Taskify.Services
{
    public class HomeService : IHomeService
    {
        public Task<LandingPageViewModel> GetLandingPageDataAsync()
        {
            // Sau này có thể query DB để đếm số user thật
            // Hiện tại trả về data mẫu
            var data = new LandingPageViewModel
            {
                TotalUsers = "1,000+",
                ActiveProjects = "500+",
                Uptime = "99.9%"
            };
            return Task.FromResult(data);
        }
    }

}
