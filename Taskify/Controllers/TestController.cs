using Microsoft.AspNetCore.Mvc;
using Taskify.Services;
namespace Taskify.Controllers
{
    [Route("test")]// Duong dan
    public class TestController:Controller
    {
        private readonly IAccountService _accountService ;
        public TestController(IAccountService accountService)
        {
            _accountService = accountService;
        }
        [HttpGet("seed-users")]
        public async Task<IActionResult> SeedUsers()
        {
          var usersCreated = new List<string>();
            //Create 5 users
            for (int i = 1; i <= 5; i++)
            {
                var email = $"user{i}@taskify.com";
                var password = "123456";
                var fullName = $"Test User {i}";
                
                var user = await _accountService.RegisterAsync(fullName, email, password);
                if (user != null) usersCreated.Add($"Created: {email} | Pass: {password}");
                else usersCreated.Add($"Failed to create: {email}");
            }
            return Ok(new
            {
                Message = "User seeding completed.",
                Accounts = usersCreated
            });
        }
    }
}
