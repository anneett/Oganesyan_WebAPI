using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }
        public DbSet<Oganesyan_WebAPI.Models.DbMeta> Meta { get; set; }
        public DbSet<Oganesyan_WebAPI.Models.User> Users { get; set; }
        public DbSet<Oganesyan_WebAPI.Models.Solution> Solutions { get; set; }
        public DbSet<Oganesyan_WebAPI.Models.Exercise> Exercises { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<Exercise>()
                .HasIndex(e => e.Title)
                .IsUnique();
        }
    }
}