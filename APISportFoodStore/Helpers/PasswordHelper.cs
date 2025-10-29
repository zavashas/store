using System.Security.Cryptography;
using System.Text;

namespace APISportFoodStore.Helpers
{
    /// <summary>
    /// хелпер для хэширования пароля
    /// </summary>
    public static class PasswordHelper
    {
        public static string Hash(string plainText)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
