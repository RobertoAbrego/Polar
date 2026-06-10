// ============================================================
// RUTA: Controllers/RankingController.cs  (ARCHIVO NUEVO)
// ============================================================
using Microsoft.AspNetCore.Mvc;
using Polar.Services;

namespace Polar.Controllers
{
    public class RankingController : Controller
    {
        private readonly RankingService             _ranking;
        private readonly ILogger<RankingController> _log;

        public RankingController(RankingService ranking, ILogger<RankingController> log)
        {
            _ranking = ranking;
            _log     = log;
        }

        // GET /Ranking
        public async Task<IActionResult> Index()
        {
            var top = await _ranking.ObtenerTopAsync(20);

            // Lee el UsuarioId de sesión (misma clave que usa AuthService)
            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            if (miId.HasValue)
                ViewBag.MiPosicion = await _ranking.ObtenerMiPosicionAsync(miId.Value);

            return View(top);
        }
    }
}
