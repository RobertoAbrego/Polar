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

        [HttpPost]
        public IActionResult Eliminar(int id)
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

            _service.DeleteComment(id);

            return RedirectToAction(
                "Index",
                "Feed");
        }

    }
}