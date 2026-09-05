using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentAccommodation.Application.Interfaces;
using StudentAccommodation.Domain.Entities;

namespace StudentAccommodation.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<AccommodationRequest> AccommodationRequests { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Bed> Beds { get; set; }
        public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().HasKey(u => u.Id);
            modelBuilder.Entity<AccommodationRequest>().HasKey(r => r.Id);
            modelBuilder.Entity<Room>().HasKey(r => r.Id);
            modelBuilder.Entity<Bed>().HasKey(b => b.Id);
            modelBuilder.Entity<MaintenanceTicket>().HasKey(t => t.Id);

            modelBuilder.Entity<AccommodationRequest>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId);

            modelBuilder.Entity<Bed>()
                .HasOne(b => b.Room)
                .WithMany()
                .HasForeignKey(b => b.RoomId);
        }
    }

    public static class ApplicationDbContextSeed
    {
        public static async Task SeedSampleDataAsync(ApplicationDbContext context)
        {
            // 1. حساب المشرف الإداري فقط
            if (!await context.Users.AnyAsync(u => u.Id == "admin"))
            {
                context.Users.Add(new User
                {
                    Id = "admin",
                    Name = "أ. فهد الأحمد (المشرف المالي)",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("adminpassword"),
                    Role = "Admin",
                    College = "عمادة شؤون الطلاب",
                    Department = "إدارة الإسكان"
                });
                await context.SaveChangesAsync();
            }

            // 2. الغرف والأسرة الفارغة في مباني السكن (جاهزة لاستقبال الطلاب الحقيقيين)
            if (!await context.Rooms.AnyAsync())
            {
                var r101 = new Room { RoomNumber = "101", Building = "مبنى أ", Floor = 1, Capacity = 4 };
                var r102 = new Room { RoomNumber = "102", Building = "مبنى أ", Floor = 1, Capacity = 4 };
                var r103 = new Room { RoomNumber = "103", Building = "مبنى ب", Floor = 1, Capacity = 4 };
                var r104 = new Room { RoomNumber = "104", Building = "مبنى ب", Floor = 1, Capacity = 4 };
                context.Rooms.AddRange(r101, r102, r103, r104);
                await context.SaveChangesAsync();

                context.Beds.AddRange(
                    new Bed { RoomId = r101.Id, BedNumber = "A", IsOccupied = false },
                    new Bed { RoomId = r101.Id, BedNumber = "B", IsOccupied = false },
                    new Bed { RoomId = r101.Id, BedNumber = "C", IsOccupied = false },
                    new Bed { RoomId = r101.Id, BedNumber = "D", IsOccupied = false },

                    new Bed { RoomId = r102.Id, BedNumber = "A", IsOccupied = false },
                    new Bed { RoomId = r102.Id, BedNumber = "B", IsOccupied = false },
                    new Bed { RoomId = r102.Id, BedNumber = "C", IsOccupied = false },
                    new Bed { RoomId = r102.Id, BedNumber = "D", IsOccupied = false },

                    new Bed { RoomId = r103.Id, BedNumber = "A", IsOccupied = false },
                    new Bed { RoomId = r103.Id, BedNumber = "B", IsOccupied = false },
                    new Bed { RoomId = r103.Id, BedNumber = "C", IsOccupied = false },
                    new Bed { RoomId = r103.Id, BedNumber = "D", IsOccupied = false },

                    new Bed { RoomId = r104.Id, BedNumber = "A", IsOccupied = false },
                    new Bed { RoomId = r104.Id, BedNumber = "B", IsOccupied = false },
                    new Bed { RoomId = r104.Id, BedNumber = "C", IsOccupied = false },
                    new Bed { RoomId = r104.Id, BedNumber = "D", IsOccupied = false }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}

namespace StudentAccommodation.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public TokenService(string secretKey, string issuer, string audience, int expiryMinutes = 1440)
        {
            _secretKey = secretKey;
            _issuer = issuer;
            _audience = audience;
            _expiryMinutes = expiryMinutes > 0 ? expiryMinutes : 1440;
        }

        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
