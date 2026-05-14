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
        private readonly MissionService _missions;

        public HomeController(
            EvidenciaService ev,
            AuthService auth,
            MissionService missions)
        {
            _ev = ev;
            _auth = auth;
            _missions = missions;
        }

        public IActionResult Index()
        {
            var email =
                HttpContext.Session.GetString("UserEmail");

            // =========================
            // FEED
            // =========================
            ViewBag.Feed =
                _ev.GetFeed(email ?? "");

            // =========================
            // MISIONES
            // =========================
            ViewBag.Misiones =
                _missions.GetAll() ?? new List<Mision>();

            // =========================
            // RETOS (🔥 ESTE ERA EL ERROR)
            // =========================
            ViewBag.Retos = new List<object>();

            // =========================
            // USUARIO LOGUEADO
            // =========================
            if (!string.IsNullOrWhiteSpace(email))
            {
                ViewBag.NombreUsuario =
                    _auth.GetNombreByEmail(email) ?? "Usuario";

                ViewBag.Puntos =
                    _auth.GetPuntosByEmail(email);

                ViewBag.Nivel =
                    _auth.GetNivelByEmail(email);

                ViewBag.Progreso =
                    _auth.GetProgresoAlSiguienteNivel(email);
            }
            else
            {
                ViewBag.NombreUsuario = "Usuario";
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