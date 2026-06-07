using RestauranteAPI.Data;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Servicios.Implementaciones
{
    public class EstadoService : IEstadoService
    {
        private readonly MyAppDbContext _db;

        public EstadoService(MyAppDbContext db)
        {
            _db = db;
        }
        //obtener todos los estados
        public List<Estado> GetEstados()
        {
            return _db.Estados.ToList();
        }
        //obtener un estado por id
        public Estado GetEstadoById(int estadoId)
        {
            return _db.Estados.Find(estadoId);
        }

        //crear un nuevo estado
        public Estado CrearEstado(Estado estado)
        {
            var result = new Estado
            {
                Nombre = estado.Nombre
            };

            _db.Estados.Add(result);
            _db.SaveChanges();

            return result;
        }
        //actualizar un estado existente
        public Estado ActualizarEstado(int estadoId, Estado estado)
        {
            var result = _db.Estados.Find(estadoId);

            if (result == null)
                return null;

            result.Nombre = estado.Nombre;
            _db.SaveChanges();

            return result;
        }
        //eliminar un estado por id

        public void EliminarEstado(int estadoId)
        {
            var result = _db.Estados.Find(estadoId);

            if (result == null)
                return;

            _db.Estados.Remove(result);
            _db.SaveChanges();
        }
    }
}
