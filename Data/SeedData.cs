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

            // CLIENTS (independent)
            if (!context.Clients.Any())
            {
                context.Clients.AddRange(
                    new Client
                    {
                        FirstName = "Juan",
                        LastName = "Jimenez",
                        PhoneNumber = "88881111",
                        IdCard = "504580546"
                    },
                    new Client
                    {
                        FirstName = "Maria",
                        LastName = "Lopez",
                        PhoneNumber = "88882222",
                        IdCard = "504580547"
                    },
                    new Client
                    {
                        FirstName = "Carlos",
                        LastName = "Fernandez",
                        PhoneNumber = "88883333",
                        IdCard = "504580548"
                    }
                );
            }

            // ZONES (independent, referenced by Tables)
            if (!context.Zones.Any())
            {
                context.Zones.AddRange(
                    new Zone { Name = "Terrace", IsAvailable = true },
                    new Zone { Name = "VIP", IsAvailable = true },
                    new Zone { Name = "Indoor", IsAvailable = true },
                    new Zone { Name = "Outdoor", IsAvailable = true }
                );
            }

            // TURNS (independent)
            if (!context.Turns.Any())
            {
                context.Turns.Add(
                    new Turn
                    {
                        Name = "General Turn",
                        StartTime = new TimeOnly(8, 0),
                        EndTime = new TimeOnly(23, 0),
                        IsActive = true
                    }
                );
            }

            // Persist independent entities so EF generates their IDs
            context.SaveChanges();

            // TABLES (depend on Zones — use navigation property)
            if (!context.Tables.Any())
            {
                var terraceZone = context.Zones.First(z => z.Name == "Terrace");
                var vipZone = context.Zones.First(z => z.Name == "VIP");
                var indoorZone = context.Zones.First(z => z.Name == "Indoor");

                context.Tables.AddRange(
                    new Table
                    {
                        TableNumber = "1",
                        Capacity = 2,
                        Zone = terraceZone
                    },
                    new Table
                    {
                        TableNumber = "2",
                        Capacity = 4,
                        Zone = terraceZone
                    },
                    new Table
                    {
                        TableNumber = "3",
                        Capacity = 6,
                        Zone = vipZone
                    },
                    new Table
                    {
                        TableNumber = "4",
                        Capacity = 8,
                        Zone = indoorZone
                    }
                );
            }

            // SEED USER (Admin)
            if (!context.Users.Any())
            {
                context.Users.Add(
                    new User
                    {
                        Username = "admin",
                        FullName = "Administrator",
                        Email = "admin@restaurant.com",
                        PasswordHash = "AQAAAAEAACcQAAAAEGlvbmVlL3Bhc3N3b3Jk",
                        Role = "Admin"
                    }
                );
            }

            context.SaveChanges();
        }
    }
}
