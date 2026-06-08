using System;
using System.Linq;
using RestauranteAPI.Models;

namespace RestauranteAPI.Data
{
    public static class SeedData
    {
        public static void Initialize(MyAppDbContext context)
        {
            context.Database.EnsureCreated();

            // CLIENTS
            if (!context.Clients.Any())
            {
                context.Clients.AddRange(
                    new Client
                    {
                        Id = 1,
                        FirstName = "Juan",
                        LastName = "Jimenez",
                        PhoneNumber = "88881111",
                        IdCard = "504580546"
                    },
                    new Client
                    {
                        Id = 2,
                        FirstName = "Maria",
                        LastName = "Lopez",
                        PhoneNumber = "88882222",
                        IdCard = "504580547"
                    },
                    new Client
                    {
                        Id = 3,
                        FirstName = "Carlos",
                        LastName = "Fernandez",
                        PhoneNumber = "88883333",
                        IdCard = "504580548"
                    }
                );
            }

            // ZONES
            if (!context.Zones.Any())
            {
                context.Zones.AddRange(
                    new Zone
                    {
                        Id = 1,
                        Name = "Terrace",
                        IsAvailable = true
                    },
                    new Zone
                    {
                        Id = 2,
                        Name = "VIP",
                        IsAvailable = true
                    },
                    new Zone
                    {
                        Id = 3,
                        Name = "Indoor",
                        IsAvailable = true
                    },
                    new Zone
                    {
                        Id = 4,
                        Name = "Outdoor",
                        IsAvailable = true
                    }
                );
            }

            // TURNS
            if (!context.Turns.Any())
            {
                context.Turns.Add(
                    new Turn
                    {
                        Id = 1,
                        Name = "General Turn",
                        StartTime = new TimeOnly(8, 0),
                        EndTime = new TimeOnly(23, 0),
                        IsActive = true
                    }
                );
            }

            // TABLES
            if (!context.Tables.Any())
            {
                context.Tables.AddRange(
                    new Table
                    {
                        Id = 1,
                        TableNumber = "1",
                        Capacity = 2,
                        ZoneId = 1
                    },
                    new Table
                    {
                        Id = 2,
                        TableNumber = "2",
                        Capacity = 4,
                        ZoneId = 1
                    },
                    new Table
                    {
                        Id = 3,
                        TableNumber = "3",
                        Capacity = 6,
                        ZoneId = 2
                    },
                    new Table
                    {
                        Id = 4,
                        TableNumber = "4",
                        Capacity = 8,
                        ZoneId = 3
                    }
                );
            }

            // SEED USER (Admin)
            if (!context.Users.Any())
            {
                context.Users.Add(
                    new User
                    {
                        Id = 1,
                        Username = "admin",
                        FullName = "Administrator",
                        Email = "admin@restaurant.com",
                        PasswordHash = "AQAAAAEAACcQAAAAEGlvbmVlL3Bhc3N3b3Jk", // dummy hash
                        Role = "Admin"
                    }
                );
            }

            context.SaveChanges();
        }
    }
}
