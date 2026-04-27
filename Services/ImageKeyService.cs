using System.Security.Cryptography;

namespace Polar.Services
{
    public class ImageKeyService
    {
        public static string GetKeyFromImage(string imagePath)
        {
            byte[] bytes = File.ReadAllBytes(imagePath);

            using (SHA256 sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}