using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Utilities;

namespace Taskify.Services
{
    public class AccountService: IAccountService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        public AccountService(AppDbContext context,IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
        public async Task ChangePasswordASync(Guid userId,string currentPassword,string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if(user== null) throw new Exception("User not found");
            if(!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash, user.Salt)) throw new Exception("The current password not right");

            user.Salt = PasswordHelper.GenerateSalt();
            user.PasswordHash = PasswordHelper.HashPassword(newPassword,user.Salt);
            //_context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        public async Task SendForgotPasswordOtpAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(m => m.Email == email);
            if (user == null) return;
            var otp = new Random().Next(100000, 999999).ToString();

            user.PasswordResetToken = otp;
            user.ResetTokenExperies = DateTime.Now.AddMinutes(10);
            string subject = "Taskify - Quên Mật Khẩu";
            string body = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                                <h2>Yêu cầu đặt lại mật khẩu</h2>
                                <p>Xin chào {user.FullName},</p>
                                <p>Mã xác thực (OTP) của bạn là:</p>
                                <h1 style='color: #0d6efd; letter-spacing: 5px;'>{otp}</h1>
                                <p>Mã này sẽ hết hạn sau 10 phút.</p>
                                <hr/>
                                <small>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</small>
                            </div>";
            await _emailService.SendEmailAsync(email,subject,body);
            await _context.SaveChangesAsync();

        }
        public async Task ResetPasswordWithOtpAsync(string email, string otp, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(m => m.Email == email);
            if (user == null || user.PasswordResetToken != otp) throw new Exception("The Otp is not right or enter wrong email");
            if (user.ResetTokenExperies < DateTime.Now) throw new Exception("The otp is experies,please take new otp");
            user.Salt = PasswordHelper.GenerateSalt();
            user.PasswordHash = PasswordHelper.HashPassword(newPassword, user.Salt);

            user.ResetTokenExperies = new DateTime(01/01/0001);
            user.PasswordResetToken = null;
            await _context.SaveChangesAsync();
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

        public async Task<User?> GetUserbyEmailAsync(string email)
        {
          return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }
    }
}
