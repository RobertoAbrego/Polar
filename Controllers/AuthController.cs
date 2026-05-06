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
                // 🔥 AQUÍ SÍ VA REGISTER
                _authService.Register(model.Nombre, model.Email, model.Password);

                TempData["Success"] = "✅ Usuario registrado correctamente";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"❌ Error al registrar: {ex.Message}";
                return View();
            }
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