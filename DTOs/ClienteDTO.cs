namespace RestauranteAPI.DTOs
{
    
    public class ClienteCreateDTO //para crear un cliente
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public int Telefono { get; set; }
        public int Cedula { get; set; }
    }

    public class ClienteResponseDTO //para devolver datos de un cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public int Telefono { get; set; }
        public int Cedula { get; set; }
    }

    public class ClienteReservaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<ReservaCreateDTO> Reservas { get; set; } = new();
    }

    public class ClienteListaEsperaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<ListaEsperaCreateDTO> ListaEspera { get; set; } = new();
    }


}
