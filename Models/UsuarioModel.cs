namespace Polar.Models
{
    public class UsuarioModel
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        public string Email { get; set; }

        public string? FotoPerfil { get; set; }

        public int Nivel { get; set; }

        public int PuntosTotales { get; set; }
    }
}