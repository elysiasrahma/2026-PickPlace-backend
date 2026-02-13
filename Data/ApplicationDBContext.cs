using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Models;

namespace PickPlace.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms { get; set; }
}