using Microsoft.AspNetCore.Mvc;
using Polar.Services;

namespace Polar.Controllers
{
    public class EvidenciaController : Controller
    {
        private readonly EvidenciaService _service;

        public EvidenciaController(EvidenciaService service)
        {
            _service = service;
        }

        // =========================
        // 📸 FORM
        // =========================
        [HttpGet]
        public IActionResult Crear()
        {
            // validar sesión
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // =========================
        // 📸 SUBMIT
        // =========================
        [HttpPost]
        public IActionResult Crear(int misionId, IFormFile imagen)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return RedirectToAction("Login", "Auth");

            if (imagen == null || imagen.Length == 0)
            {
                ViewBag.Message = "⚠️ Debes subir una imagen";
                return View();
            }

            _service.Create(email, misionId, imagen);

            return RedirectToAction("Index", "Feed");
        }
    }
}