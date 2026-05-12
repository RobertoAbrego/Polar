namespace Polar.Models
{
    public class FeedPost
    {
        public int EvidenciaId { get; set; }

        public string Nombre { get; set; } = "";

        public string Titulo { get; set; } = "";

        public string Tipo { get; set; } = "";

        public int Puntos { get; set; }

        public string Imagen { get; set; } = "";

        public DateTime Fecha { get; set; }

        public bool EsMia { get; set; }

        public List<ComentarioModel> Comentarios { get; set; }
            = new List<ComentarioModel>();
    }
}