namespace RestauranteAPI.Entidades
{
    public class Zona
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public bool Disponibilidad { get; set; } 

        // Relaciones        
        public List<Mesa> Mesas { get; set; } = new();
    }
}