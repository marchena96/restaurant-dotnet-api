using RestauranteAPI.Data;
using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Servicios.Implementaciones
{
    public class ClienteService : IClienteService
    {
        private readonly MyAppDbContext _db;

        public ClienteService(MyAppDbContext db)
        {
            _db = db;
        }

        //obtener todos los clientes
        public List<ClienteResponseDTO> GetAllClientes()
        {
            return _db.Clientes
                .Select(cliente => new ClienteResponseDTO
                {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    Apellido = cliente.Apellido,
                    Telefono = cliente.Telefono,
                    Cedula = cliente.Cedula
                })
                .ToList();
        }
        //obtener cliente por id
        public ClienteResponseDTO GetClienteById(int clienteId)
        {
            var result = _db.Clientes.Find(clienteId);
            if (result == null)
            {
                return null;
            }

            return new ClienteResponseDTO
            {
                Id = result.Id,
                Nombre = result.Nombre,
                Apellido = result.Apellido,
                Telefono = result.Telefono,
                Cedula = result.Cedula
            };
        }

        //crear cliente

        public ClienteResponseDTO CrearCliente(ClienteCreateDTO clienteDTO)
        {
            var result = new Cliente
            {
                Nombre = clienteDTO.Nombre,
                Apellido = clienteDTO.Apellido,
                Telefono = clienteDTO.Telefono,
                Cedula = clienteDTO.Cedula
            };

            _db.Clientes.Add(result);
            _db.SaveChanges();

            return new ClienteResponseDTO
            {
                Id = result.Id,
                Nombre = result.Nombre,
                Apellido = result.Apellido,
                Telefono = result.Telefono,
                Cedula = result.Cedula
            };
        }


        //actualizar cliente
        public ClienteResponseDTO ActualizarCliente(int clienteId, ClienteCreateDTO clienteDTO)
        {
            var result = _db.Clientes.Find(clienteId);

            if (result == null)
            {
                return null;
            }

            result.Nombre = clienteDTO.Nombre;
            result.Apellido = clienteDTO.Apellido;
            result.Telefono = clienteDTO.Telefono;
            result.Cedula = clienteDTO.Cedula;

            _db.SaveChanges();

            return new ClienteResponseDTO
            {
                Id = result.Id,
                Nombre = result.Nombre,
                Apellido = result.Apellido,
                Telefono = result.Telefono,
                Cedula = result.Cedula
            };
        }


        //eliminar cliente
        public void EliminarCliente(int clienteId)
        {
            var result = _db.Clientes.Find(clienteId);

            if (result == null)
            {
                return;
            }

            _db.Clientes.Remove(result);
            _db.SaveChanges();
        }
    }
}