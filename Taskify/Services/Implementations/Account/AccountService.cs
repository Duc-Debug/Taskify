using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Utilities;

namespace Taskify.Services
{
    public class AccountService: IAccountService
    {
        private readonly AppDbContext _context;
        public AccountService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<User> RegisterAsync(string fullName, string email, string password)
        {
            // Kiem tra email da ton tai chua
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (existingUser != null) return null;

            string salt = PasswordHelper.GenerateSalt();
            string hashedPassword = PasswordHelper.HashPassword(password, salt);
            // Tao user moi
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                Salt = salt,
                PasswordHash = hashedPassword,
                AvatarUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(fullName)
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return newUser;
        }

      

        public async Task<User> ValidateUserAsync(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null) return null;

            bool isValid = PasswordHelper.VerifyPassword(password, user.PasswordHash, user.Salt);
            if(isValid) return user;
            
            return null;

        }
        public async Task<ProfileViewModel> GetUserProfileAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;
            var profile = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                DateOfBirth = DateTime.Now,
                Address = "",
                Bio = "",
                AvatarUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(user.FullName)
            };
            return profile;

        }
        public Task<bool> UpdateProfileAsync(Guid userId, ProfileViewModel model)
        {
            throw new NotImplementedException();
        }

    }
}
