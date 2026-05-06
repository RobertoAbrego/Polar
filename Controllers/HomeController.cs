using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polar.Models;
using Polar.Services;

namespace Polar.Controllers
{
    public class HomeController : Controller
    {
        private readonly EvidenciaService _ev;

        // 🔥 INYECCIÓN DEL SERVICE
        public HomeController(EvidenciaService ev)
        {
            _ev = ev;
        }

        // 🔥 INDEX CON FEED
        public IActionResult Index()
        {
            ViewBag.Feed = _ev.GetFeed();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}