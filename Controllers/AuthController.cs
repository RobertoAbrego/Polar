using Microsoft.AspNetCore.Mvc;
using Polar.Models;
using Polar.Services;

namespace Polar.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Message = "⚠️ Debes completar todos los campos";
                return View();
            }

            if (_authService.Login(model.Email, model.Password))
            {
                ViewBag.Message = "✅ Inicio de sesión exitoso";
                return View();
            }

            ViewBag.Message = "❌ Cuenta o contraseña incorrecta";
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Message = "⚠️ Debes completar todos los campos";
                return View();
            }

            try
            {
                _authService.Register(model.Email, model.Password);
                ViewBag.Message = "✅ Usuario registrado correctamente";
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"❌ Error al registrar: {ex.Message}";
            }

            return View();
        }
    }
}