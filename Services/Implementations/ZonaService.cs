using RestauranteAPI.Data;
using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;


namespace RestauranteAPI.Servicios.Implementaciones
{
    public class ZonaService : IZonaService
    {
        private readonly MyAppDbContext _db;

        public ZonaService(MyAppDbContext db)
        {
            _db = db;
        }

        // obtener todas las zonas
        public List<ZonaResponseDTO> GetAllZonas()
        {
            return _db.Zonas
                .Select(zona => new ZonaResponseDTO
                {
                    Id = zona.Id,
                    Nombre = zona.Nombre,
                    Disponibilidad = zona.Disponibilidad
                })
                .ToList();
        }

        // obtener zona por nombre
        public ZonaResponseDTO GetZonaByNombre(string Nombre)
        {
            var zona = _db.Zonas.FirstOrDefault(zona => zona.Nombre == Nombre );
            if (zona == null)
            {
                return null;
            }

            return new ZonaResponseDTO
            {
                Id = zona.Id,
                Nombre = zona.Nombre,
                Disponibilidad = zona.Disponibilidad
            };
        }

        // Crear Zona
        public ZonaResponseDTO CrearZona(ZonaCreateDTO zonaDTO)
        {
            var zona = new Zona
            {
                Nombre = zonaDTO.Nombre,
                Disponibilidad = zonaDTO.Disponibilidad
            };

            _db.Zonas.Add(zona);
            _db.SaveChanges();

            return new ZonaResponseDTO
            {
                Id = zona.Id,
                Nombre = zona.Nombre,
                Disponibilidad = zona.Disponibilidad
            };

        }

        //Actualizar zona
        public ZonaResponseDTO ActualizarZona(int zonaId, ZonaCreateDTO zonaDTO)
        {
            var zona = _db.Zonas.Find(zonaId);

            if (zona == null)
            {
                return null;
            }

            zona.Nombre = zonaDTO.Nombre;
            zona.Disponibilidad = zonaDTO.Disponibilidad;

            _db.SaveChanges();

            return new ZonaResponseDTO
            {
                Id = zona.Id,
                Nombre = zona.Nombre,
                Disponibilidad = zona.Disponibilidad
            };
        }


        // Eliminar zona
        public void EliminarZona(int zonaId)
        {
            var result = _db.Zonas.Find(zonaId);

            if (result == null)
            {
                return;
            }

            _db.Zonas.Remove(result);
            _db.SaveChanges();
        }


    }
}