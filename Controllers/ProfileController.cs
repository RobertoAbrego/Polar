using Microsoft.AspNetCore.Mvc;
using Polar.Services;

namespace Polar.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AuthService _auth;
        private readonly IWebHostEnvironment _env;

        public ProfileController(
            AuthService auth,
            IWebHostEnvironment env)
        {
            _auth = auth;
            _env = env;
        }

        // =========================
        // ⚙️ CONFIG
        // =========================

        public IActionResult Settings()
        {
            var email =
                HttpContext.Session.GetString(
                    "UserEmail");

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var user =
                _auth.GetUserByEmail(email);

            return View(user);
        }

        // =========================
        // 💾 UPDATE PROFILE
        // =========================

        [HttpPost]
        public IActionResult UpdateProfile(
            string nombre,
            IFormFile? foto)
        {
            var email =
                HttpContext.Session.GetString(
                    "UserEmail");

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            string? ruta = null;

            // 🔥 GUARDAR FOTO
            if (foto != null &&
                foto.Length > 0)
            {
                var fileName =
                    Guid.NewGuid().ToString() +
                    Path.GetExtension(
                        foto.FileName);

                var uploads =
                    Path.Combine(
                        _env.WebRootPath,
                        "uploads");

                Directory.CreateDirectory(
                    uploads);

                var path =
                    Path.Combine(
                        uploads,
                        fileName);

                using var stream =
                    new FileStream(
                        path,
                        FileMode.Create);

                foto.CopyTo(stream);

                ruta =
                    "/uploads/" + fileName;
            }

            _auth.UpdateProfile(
                email,
                nombre,
                ruta);
            if (ruta != null)
            {
                HttpContext.Session.SetString(
                    "UserPhoto",
                    ruta);
            }
            HttpContext.Session.SetString(
                "UserName",
                nombre);
            

            TempData["Success"] =
                "✅ Perfil actualizado";

            return RedirectToAction(
                "Settings");
        }

        // =========================
        // 📧 ENVIAR CÓDIGO
        // =========================

        [HttpPost]
        public IActionResult SendPasswordCode()
        {
            var email =
                HttpContext.Session.GetString(
                    "UserEmail");

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var code =
                new Random()
                .Next(100000, 999999)
                .ToString();

            HttpContext.Session.SetString(
                "PasswordCode",
                code);

            _auth.SendVerificationEmail(
                email,
                code);

            TempData["Success"] =
                "📧 Código enviado al correo";

            return RedirectToAction(
                "Settings");
        }

        // =========================
        // 🔑 CHANGE PASSWORD
        // =========================

        [HttpPost]
        public IActionResult ChangePassword(
            string code,
            string newPassword)
        {
            var savedCode =
                HttpContext.Session.GetString(
                    "PasswordCode");

            if (savedCode != code)
            {
                TempData["Error"] =
                    "❌ Código inválido";

                return RedirectToAction(
                    "Settings");
            }

            var email =
                HttpContext.Session.GetString(
                    "UserEmail");

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            _auth.ChangePassword(
                email,
                newPassword);

            TempData["Success"] =
                "✅ Contraseña actualizada";

            return RedirectToAction(
                "Settings");
        }
    }
}