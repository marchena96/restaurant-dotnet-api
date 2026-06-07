namespace RestauranteAPI.Entidades
{
    public class Turno
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public bool Activo { get; set; }
    }
}
