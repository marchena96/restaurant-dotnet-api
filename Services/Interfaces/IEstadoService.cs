using RestauranteAPI.Entidades;

namespace RestauranteAPI.Servicios.Interfaces
{
    public interface IEstadoService
    {
        List<Estado> GetEstados();
        public Estado GetEstadoById(int estadoId);
        public Estado CrearEstado(Estado estado);
        public Estado ActualizarEstado(int estadoId, Estado estado);
        public void EliminarEstado(int estadoId);
    }
}
