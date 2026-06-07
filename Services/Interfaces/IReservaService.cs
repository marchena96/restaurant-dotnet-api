using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;
namespace RestauranteAPI.Servicios.Interfaces
{
    public interface IReservaService
    {
        public List<ReservaResponseDTO> GetReservas();

        public ReservaResponseDTO GetReservaById(int reservaId);

        public ReservaResponseDTO CrearReserva(ReservaCreateDTO reservaDTO);
        public ReservaResponseDTO ActualizarReserva(int reservaId, ReservaCreateDTO reservaDTO);

        public void EliminarReserva(int reservaId);
        public List<ReservaResponseDTO> ConsultarReservasPorCliente(int clienteId);

        public ReservaResponseDTO CambiarEstadoReserva(int reservaId, int estadoId);
        public List<ReservaResponseDTO> ConsultarReservasPorFecha(DateOnly fecha);
        public List<ReservaResponseDTO> ObtenerHistorialReservasCliente(int clienteId);

        public bool VerificarEstadoReserva(int reservaId, string estadoNombre);
    }
}