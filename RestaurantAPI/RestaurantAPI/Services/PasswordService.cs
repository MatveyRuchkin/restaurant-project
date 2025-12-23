using BCrypt.Net;

namespace RestaurantAPI.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        (bool IsValid, string ErrorMessage) ValidatePasswordRequirements(string password);
    }

    public class PasswordService : IPasswordService
    {
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt());
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        public (bool IsValid, string ErrorMessage) ValidatePasswordRequirements(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Пароль не может быть пустым");

            if (password.Length < 8)
                return (false, "Пароль должен содержать минимум 8 символов");

            if (!password.Any(char.IsUpper))
                return (false, "Пароль должен содержать хотя бы одну заглавную букву");

            if (!password.Any(char.IsLower))
                return (false, "Пароль должен содержать хотя бы одну строчную букву");

            if (!password.Any(char.IsDigit))
                return (false, "Пароль должен содержать хотя бы одну цифру");

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                return (false, "Пароль должен содержать хотя бы один специальный символ");

            return (true, string.Empty);
        }
    }
}
