using RestauranteAPI.DTOs;

namespace RestauranteAPI.Entidades
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public int Telefono { get; set; }
        public int Cedula { get; set; }

        public List<Reserva> Reservas { get; set; } = new();
        public List<ListaEspera> ListaEspera { get; set; } = new();
    }
}