using Microsoft.AspNetCore.Mvc;
using Polar.Services;

namespace Polar.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _auth = new AuthService();

        // =========================
        // LOGIN VIEW
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // LOGIN POST
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            bool ok = _auth.Login(email, password);

            if (ok)
                return Content("LOGIN OK 🔥 Usuario autenticado");

            return Content("LOGIN FAILED 💀 Credenciales incorrectas");
        }

        // =========================
        // REGISTER VIEW
        // =========================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // REGISTER POST
        [HttpPost]
        public IActionResult Register(string email, string password)
        {
            _auth.Register(email, password);

            return Content("USER CREATED 🔥 Usuario registrado correctamente");
        }
    }
}