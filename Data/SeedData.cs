using RestauranteAPI.Entidades;

namespace RestauranteAPI.Data
{
    public static class SeedData
    {
        public static void Initialize(MyAppDbContext context)
        {
            // CLIENTES
            if (!context.Clientes.Any())
            {
                context.Clientes.AddRange(
                    new Cliente
                    {
                        Id = 1,
                        Nombre = "Juan",
                        Apellido = "Jimenez",
                        Telefono = 88881111,
                        Cedula = 504580546
                    },

                    new Cliente
                    {
                        Id = 2,
                        Nombre = "Maria",
                        Apellido = "Lopez",
                        Telefono = 88882222,
                        Cedula = 504580547
                    },

                    new Cliente
                    {
                        Id = 3,
                        Nombre = "Carlos",
                        Apellido = "Fernandez",
                        Telefono = 88883333,
                        Cedula = 504580548
                    }
                );
            }

            // ZONAS
            if (!context.Zonas.Any())
            {
                context.Zonas.AddRange(
                    new Zona
                    {
                        Id = 1,
                        Nombre = "Terraza",
                        Disponibilidad = true
                    },

                    new Zona
                    {
                        Id = 2,
                        Nombre = "VIP",
                        Disponibilidad = true
                    },

                    new Zona
                    {
                        Id = 3,
                        Nombre = "Interior",
                        Disponibilidad = true
                    },
                    new Zona
                    {
                        Id = 4,
                        Nombre = "Exterior",
                        Disponibilidad = true
                    }
                );
            }

            // TURNO GENERAL
            if (!context.Turnos.Any())
            {
                context.Turnos.Add(
                    new Turno
                    {
                        Id = 1,
                        Nombre = "Turno General",
                        HoraInicio = new TimeOnly(8, 0),
                        HoraFin = new TimeOnly(23, 0),
                        Activo = true
                    }
                );
            }

            // MESAS
            if (!context.Mesas.Any())
            {
                context.Mesas.AddRange(
                    new Mesa
                    {
                        Id = 1,
                        Numero = 1,
                        Capacidad = 2,
                        ZonaId = 1
                    },

                    new Mesa
                    {
                        Id = 2,
                        Numero = 2,
                        Capacidad = 4,
                        ZonaId = 1
                    },

                    new Mesa
                    {
                        Id = 3,
                        Numero = 3,
                        Capacidad = 6,
                        ZonaId = 2
                    },

                    new Mesa
                    {
                        Id = 4,
                        Numero = 4,
                        Capacidad = 8,
                        ZonaId = 3
                    }
                );
            }

            context.SaveChanges();
        }
    }
}