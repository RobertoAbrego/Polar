using Microsoft.AspNetCore.Mvc;
using Polar.Services;

namespace Polar.Controllers
{
    public class EvidenciaController : Controller
    {
        private readonly GeminiService _geminiService;
        private readonly EvidenciaService _service; 

        public EvidenciaController(GeminiService geminiService, EvidenciaService service)
        {
            _geminiService = geminiService;
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
        public async Task<IActionResult> Crear(int misionId, IFormFile imagen, string descripcion)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return RedirectToAction("Login", "Auth");

            if (imagen == null || imagen.Length == 0)
            {
                ViewBag.Message = "⚠️ Debes subir una imagen";
                return View();
            }

            if (string.IsNullOrWhiteSpace(descripcion))
            {
                ViewBag.Message = "⚠️ La descripción de la evidencia no puede estar vacía.";
                return View();
            }    

            // Llamada a la API de Gemini
            var evaluacionIA = await _geminiService.EvaluarPublicacionAsync(descripcion);

            if (evaluacionIA != null)
            {
                // Si Gemini determina que el texto es spam o vacío (Puntos == 0)
                if (evaluacionIA.Puntos == 0)
                {
                    ViewBag.Message = "⚠️ La IA detectó que tu descripción está vacía, contiene spam o no es válida para la misión.";
                    return View(); 
                }

                // Pasamos los 6 parámetros con los puntos y el comentario de la IA
                _service.Create(email, misionId, descripcion, imagen, evaluacionIA.Puntos, evaluacionIA.Respuesta);
            }
            else
            {
                // En caso de que la API de Gemini falle por red o problemas externos, 
                // guardamos de forma tradicional enviando 0 puntos y un comentario por defecto
                _service.Create(email, misionId, descripcion, imagen, 0, "Evidencia registrada correctamente.");
            }

            return RedirectToAction("Index", "Feed");
        }
    }
}