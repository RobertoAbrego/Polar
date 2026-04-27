using Microsoft.AspNetCore.Mvc;
using Polar.Services;

namespace Polar.Controllers
{
    public class TestCryptoController : Controller
    {
        public IActionResult Index()
        {
            string imagePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/keys/key.png"
            );

            string key = ImageKeyService.GetKeyFromImage(imagePath);

            string encrypted = CryptoService.Encrypt("MiPassword123", key);
            string decrypted = CryptoService.Decrypt(encrypted, key);

            return Content(
                $"KEY:\n{key}\n\n" +
                $"ENCRYPTED:\n{encrypted}\n\n" +
                $"DECRYPTED:\n{decrypted}"
            );
        }
    }
}