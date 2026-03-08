using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Oganesyan_WebAPI.Models.User> Users { get; set; }
        public DbSet<Oganesyan_WebAPI.Models.Solution> Solutions { get; set; }
        public DbSet<Oganesyan_WebAPI.Models.Exercise> Exercises { get; set; }
        public DbSet<Oganesyan_WebAPI.Models.DbMeta> DbMetas { get; set; }
        public DbSet<Oganesyan_WebAPI.Models.DatabaseMeta> DatabaseMetas { get; set; }
        public DbSet<Oganesyan_WebAPI.Models.DatabaseDeployment> DatabaseDeployments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<Exercise>()
                .HasIndex(e => e.Title)
                .IsUnique();

            modelBuilder.Entity<DbMeta>()
                .HasIndex(d => d.dbType)
                .IsUnique();

            modelBuilder.Entity<DatabaseDeployment>()
                .HasIndex(d => new { d.DatabaseMetaId, d.DbMetaId })
                .IsUnique();
        }
    }
}