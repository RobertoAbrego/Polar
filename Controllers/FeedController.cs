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

        public IActionResult Feed()
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return RedirectToAction("Login", "Auth");

            var data = _service.GetFeed();

            return View(data);
        }
    }
}