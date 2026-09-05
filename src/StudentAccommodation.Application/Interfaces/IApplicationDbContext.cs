using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentAccommodation.Domain.Entities;

namespace StudentAccommodation.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; set; }
        DbSet<AccommodationRequest> AccommodationRequests { get; set; }
        DbSet<Room> Rooms { get; set; }
        DbSet<Bed> Beds { get; set; }
        DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public interface ITokenService
    {
        string GenerateJwtToken(User user);
    }
}
