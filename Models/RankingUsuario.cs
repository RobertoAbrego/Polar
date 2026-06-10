// ============================================================
// RUTA: Models/RankingUsuario.cs          (ARCHIVO NUEVO)
// ============================================================
namespace Polar.Models
{
    public class RankingUsuario
    {
        public int    Posicion            { get; set; }
        public int    UsuarioId           { get; set; }
        public string NombreUsuario       { get; set; } = string.Empty;
        public string FotoPerfil { get; set; } = string.Empty; 
        public int    PuntosTotales       { get; set; }   // columna PUNTOS_TOTALES de USUARIO
        public int    Nivel               { get; set; }   // columna NIVEL de USUARIO
        public int    MisionesCompletadas { get; set; }   // COUNT de EVIDENCIA donde APROBADA = 1

        // Nombre del nivel según el número
        public string NombreNivel => Nivel switch
        {
            1 => "Explorador Polar",
            2 => "Guardián Verde",
            3 => "Eco Warrior",
            4 => "Héroe del Planeta",
            5 => "Leyenda Polar",
            _ => "Polar"
        };

        // Color de insignia según nivel
        public string InsigniaColor => Nivel switch
        {
            1 => "#78c043",
            2 => "#4caf50",
            3 => "#2196f3",
            4 => "#9c27b0",
            5 => "#ffc107",
            _ => "#4caf50"
        };

        // Emoji del nivel
        public string IconoNivel => Nivel switch
        {
            1 => "🌱",
            2 => "🌿",
            3 => "🍃",
            4 => "🌳",
            5 => "🌲",
            _ => "⭐"
        };

        // Medalla de podio para los top 3
        public string EtiquetaPodio => Posicion switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => $"#{Posicion}"
        };
        public string Inicial => 
        string.IsNullOrWhiteSpace(NombreUsuario)
            ? "?"
            : NombreUsuario.Substring(0, 1).ToUpper();
    }
}
