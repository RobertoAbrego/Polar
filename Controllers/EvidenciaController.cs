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
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return RedirectToAction("Login", "Auth");

            var misiones = _service.GetMisiones();

            return View(misiones);
        }

        // =========================
        // 📸 SUBMIT
        // =========================
        [HttpPost]
        public IActionResult Crear(
            int misionId,
            IFormFile imagen,
            string descripcion)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return RedirectToAction("Login", "Auth");

            if (imagen == null || imagen.Length == 0)
            {
                ViewBag.Message = "⚠️ Debes subir una imagen";
                return View();
            }

            _service.Create(email, misionId, descripcion, imagen);

            return RedirectToAction("Index", "Feed");
        }
    }
}