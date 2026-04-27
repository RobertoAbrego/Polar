using System.Security.Cryptography;
using System.Text;

namespace Polar.Services
{
    public class CryptoService
    {
        public static string Encrypt(string password, string key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
                aes.GenerateIV();

                var encryptor = aes.CreateEncryptor();

                byte[] input = Encoding.UTF8.GetBytes(password);
                byte[] encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);

                return Convert.ToBase64String(aes.IV.Concat(encrypted).ToArray());
            }
        }

        public static string Decrypt(string encryptedData, string key)
        {
            byte[] full = Convert.FromBase64String(encryptedData);

            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));

                byte[] iv = full.Take(16).ToArray();
                byte[] cipher = full.Skip(16).ToArray();

                aes.IV = iv;

                var decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }
}