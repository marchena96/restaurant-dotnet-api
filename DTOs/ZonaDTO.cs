
namespace RestauranteAPI.DTOs
{
    public class ZonaCreateDTO //para crear una zona
    {
        public string Nombre { get; set; } = string.Empty;
        public bool Disponibilidad { get; set; }
    }

    public class ZonaResponseDTO //para devolver datos de una zona
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Disponibilidad { get; set; }
    }

    // Relación
    public class ZonaMesaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public List<MesaResponseDTO> Mesas { get; set; } //no elegi el mesaCreateDTO porque no quiero que se creen mesas al crear una zona, solo quiero ver las mesas de esa zona
    }
}