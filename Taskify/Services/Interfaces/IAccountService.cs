using Taskify.Models;

namespace Taskify.Services
{
    public interface IAccountService
    {
        Task<User> RegisterAsync(string fullName, string email, string password);
        Task<User> ValidateUserAsync(string email, string password);
    }
}
