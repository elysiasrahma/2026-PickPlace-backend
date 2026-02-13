using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Models;

namespace PickPlace.Api.Data;

public class ApplicationDBContext : DbContext
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms { get; set; }
}