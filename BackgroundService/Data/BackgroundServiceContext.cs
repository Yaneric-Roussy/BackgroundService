using Microsoft.EntityFrameworkCore;
using BackgroundService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BackgroundService.Data
{
    public class BackgroundServiceContext(DbContextOptions<BackgroundServiceContext> options)
        : IdentityDbContext(options)
    {
        public DbSet<Player> Player { get; set; }
    }
}