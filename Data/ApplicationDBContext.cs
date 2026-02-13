using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Models;

namespace PickPlace.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms { get; set; }
    public DbSet<Booking> Bookings { get; set; }

public DbSet<PickPlace.Api.Models.Booking> Booking { get; set; } = default!;
}