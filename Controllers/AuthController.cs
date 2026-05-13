using Microsoft.AspNetCore.Mvc;
using Polar.Models;
using Polar.Services;
using System.Net;
using System.Net.Mail;

namespace Polar.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // 🔥 GENERAR CÓDIGO
        private string GenerateCode()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        private void SendEmail(string toEmail, string code)
        {
            var fromEmail = "max502991@gmail.com";
            var password = "mvegarmwrfyfguuc";

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
             Credentials = new NetworkCredential(fromEmail, password),
             EnableSsl = true,
             DeliveryMethod = SmtpDeliveryMethod.Network,
             UseDefaultCredentials = false
            };
            var mail = new MailMessage(fromEmail, toEmail)
            {
                Subject = "Código de verificación",
                Body = $"Tu código de verificación es: {code}"
            };

            client.Send(mail);
        }

        // 🔹 GET LOGIN
        public IActionResult Login()
        {
            return View();
        }

        // 🔹 POST LOGIN
        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Message = "⚠️ Debes completar todos los campos";
                return View();
            }

            // 🔥 AQUÍ VA LOGIN (NO REGISTER)
            if (_authService.Login(model.Email, model.Password))
            {
                HttpContext.Session.SetString("UserEmail", model.Email);

                // 🔥 obtener nombre desde DB
                var nombre = _authService.GetNombreByEmail(model.Email);

                if (nombre != null)
                {
                    HttpContext.Session.SetString("UserName", nombre);
                }

                TempData["Success"] = "✅ Inicio de sesión exitoso";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Message = "❌ Cuenta o contraseña incorrecta";
            return View();
        }

        // 🔹 GET REGISTER
        public IActionResult Register()
        {
            return View();
        }

        // 🔹 POST REGISTER
        [HttpPost]
        public IActionResult Register(RegisterModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Message = "⚠️ Debes completar todos los campos";
                return View();
            }

            try
            {
                // 🔥 generar código
                string code = GenerateCode();

                // 🔥 guardar temporalmente datos del usuario
                HttpContext.Session.SetString("VerificationCode", code);
                HttpContext.Session.SetString("TempNombre", model.Nombre);
                HttpContext.Session.SetString("VerificationEmail", model.Email);
                HttpContext.Session.SetString("TempPassword", model.Password);
                
                // 🔥 enviar correo
                SendEmail(model.Email, code);

                // 🔥 ir a verificar código
                return RedirectToAction("VerifyCode");
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.ToString();
                return View();
            }
        }

        // 🔹 GET VERIFY CODE
        public IActionResult VerifyCode()
        {
            return View();
        }

        // 🔹 POST VERIFY CODE
        [HttpPost]
        public IActionResult VerifyCode(string code)
        {
            var savedCode = HttpContext.Session.GetString("VerificationCode");

            if (code == savedCode)
            {
                // 🔥 obtener datos guardados temporalmente
                var nombre = HttpContext.Session.GetString("TempNombre");
                var email = HttpContext.Session.GetString("VerificationEmail");
                var password = HttpContext.Session.GetString("TempPassword");

                // 🔥 crear cuenta
                _authService.Register(nombre, email, password);

                TempData["Success"] = "✅ Cuenta creada correctamente";

                return RedirectToAction("Login");
            }

            ViewBag.Message = "❌ Código incorrecto";

            return View();
        }

        // 🔹 LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["Success"] = "Sesión cerrada";
            return RedirectToAction("Index", "Home");
        }
    }
}