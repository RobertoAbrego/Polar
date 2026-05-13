using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polar.Models;
using Polar.Services;

namespace Polar.Controllers
{
    public class HomeController : Controller
    {
        private readonly EvidenciaService _ev;
        private readonly AuthService _auth;

        // 🔥 INYECCIÓN DE SERVICES
        public HomeController(
            EvidenciaService ev,
            AuthService auth)
        {
            _ev = ev;
            _auth = auth;
        }

        // 🔥 INDEX CON FEED
        public IActionResult Index()
        {
            var email =
                HttpContext.Session.GetString("UserEmail") ?? "";

            ViewBag.Feed =
                _ev.GetFeed(email);

            if (!string.IsNullOrWhiteSpace(email))
            {
                ViewBag.NombreUsuario =
                    _auth.GetNombreByEmail(email) ?? email;

                ViewBag.Puntos =
                    _auth.GetPuntosByEmail(email);

                ViewBag.Nivel =
                    _auth.GetNivelByEmail(email);

                ViewBag.Progreso =
                    _auth.GetProgresoAlSiguienteNivel(email);
            }
            else
            {
                ViewBag.NombreUsuario = "";
                ViewBag.Puntos = 0;
                ViewBag.Nivel = 1;
                ViewBag.Progreso = 0;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}