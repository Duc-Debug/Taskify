using System.Security.Cryptography;

namespace Taskify.Utilities
{
    public static class PasswordHelper
    {

        // Add Salt
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }
        // Hash a password using SHA256
        public static string HashPassword(string password,string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                string combinedPassword = password + salt;
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combinedPassword));

                // Convert byte array to a hexadecimal string to store DB
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
        // Verify a password against a hashed password
        public static bool VerifyPassword(string enteredPassword, string storedHashedPassword,string storedSalt)
        {
            string hashOfInput = HashPassword(enteredPassword, storedSalt);
            return string.Equals(hashOfInput, storedHashedPassword, StringComparison.OrdinalIgnoreCase);
        }
    }
}
