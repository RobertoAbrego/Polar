using Microsoft.AspNetCore.Mvc;
using Polar.Services;

namespace Polar.Controllers
{
    public class FeedController : Controller
    {
        private readonly EvidenciaService _service;

        public FeedController(EvidenciaService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("UserEmail") ?? "";

            var feed = _service.GetFeed(email);

            return View(feed);
        }

        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return RedirectToAction("Login", "Auth");

            _service.DeletePost(id, email);

            return RedirectToAction("Index");
        }
    }
}