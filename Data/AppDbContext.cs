using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Solution> Solutions { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<DbMeta> DbMetas { get; set; }
        public DbSet<DatabaseMeta> DatabaseMetas { get; set; }
        public DbSet<DatabaseDeployment> DatabaseDeployments { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamAvailableDeployment> ExamAvailableDeployments { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<ExamAttemptExercise> ExamAttemptExercises { get; set; }

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
                .HasIndex(d => d.Name)
                .IsUnique();

            modelBuilder.Entity<DatabaseDeployment>()
                .HasIndex(d => new { d.DatabaseMetaId, d.DbMetaId })
                .IsUnique();

            modelBuilder.Entity<DatabaseDeployment>()
                .HasOne(d => d.DatabaseMeta)
                .WithMany(dm => dm.Deployments)
                .HasForeignKey(d => d.DatabaseMetaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DatabaseDeployment>()
                .HasOne(d => d.DbMeta)
                .WithMany()
                .HasForeignKey(d => d.DbMetaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exercise>()
                .HasOne(e => e.DatabaseMeta)
                .WithMany()
                .HasForeignKey(e => e.DatabaseMetaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solution>()
                .HasOne(s => s.Deployment)
                .WithMany()
                .HasForeignKey(s => s.DeploymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solution>()
                .HasOne(s => s.Exercise)
                .WithMany()
                .HasForeignKey(s => s.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solution>()
                .HasOne(s => s.Exam)
                .WithMany()
                .HasForeignKey(s => s.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.DatabaseMeta)
                .WithMany()
                .HasForeignKey(e => e.DatabaseMetaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAvailableDeployment>()
                .HasOne(e => e.Exam)
                .WithMany(ex => ex.AvailableDeployments)
                .HasForeignKey(e => e.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamAvailableDeployment>()
                .HasOne(e => e.DatabaseDeployment)
                .WithMany()
                .HasForeignKey(e => e.DatabaseDeploymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAttempt>()
                .HasOne(e => e.Exam)
                .WithMany()
                .HasForeignKey(e => e.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAttempt>()
                .HasOne(e => e.SelectedDeployment)
                .WithMany()
                .HasForeignKey(e => e.SelectedDeploymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAttemptExercise>()
                .HasOne(e => e.ExamAttempt)
                .WithMany(a => a.SelectedExercises)
                .HasForeignKey(e => e.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamAttemptExercise>()
                .HasOne(e => e.Exercise)
                .WithMany()
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}


