namespace Polar.Models
{
public class ComentarioModel
{
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Contenido { get; set; } = "";

    public DateTime Fecha { get; set; }

    public bool EsMio { get; set; }

    public bool PuedoEliminar { get; set; }

    public string? FotoPerfil { get; set; }

    public string Email { get; set; } = "";
}
}