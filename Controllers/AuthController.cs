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
            if (_authService.Login(model.Email, model.Password))
            {
                ViewBag.Message = "Login correcto";
                return View();
            }

            ViewBag.Message = "Credenciales incorrectas";
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(LoginModel model)
        {
            _authService.Register(model.Email, model.Password);
            ViewBag.Message = "Usuario registrado";

            return View();
        }
    }
}