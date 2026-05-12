using Microsoft.AspNetCore.Mvc;
using Polar.Services;

namespace Polar.Controllers
{
    public class ComentarioController : Controller
    {
        private readonly EvidenciaService _service;

        public ComentarioController(EvidenciaService service)
        {
            _service = service;
        }

        // =========================
        // 💬 AGREGAR COMENTARIO
        // =========================
        [HttpPost]
        public IActionResult Crear(int publicacionId, string contenido)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(contenido))
                return RedirectToAction("Index", "Feed");

            _service.AddComment(publicacionId, email, contenido);

            return RedirectToAction("Index", "Feed");
        }
    }
}