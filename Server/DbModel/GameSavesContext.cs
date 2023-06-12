using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Server.DbModel {
    public sealed class GameSavesContext : DbContext {
        public DbSet<Save> Saves { get; set; } = null!; // Automagically provided by the DbContext

        public GameSavesContext() {
        }

        public GameSavesContext(DbContextOptions<GameSavesContext> options)
            : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Save>()
                .Property(e => e.SerializedGame)
                .HasDefaultValue(null);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            if (optionsBuilder.IsConfigured) return;
            ConfigureOptions(optionsBuilder);
        }

        public static void ConfigureOptions(DbContextOptionsBuilder optionsBuilder) {
            if (!DotEnv.Loaded) DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            if (isDevelopment) {
                // Use SQLite for development
                optionsBuilder.UseSqlite("Data Source=localdatabase.db");
            } else {
                var host = Environment.GetEnvironmentVariable("POSTGRE_URL");
                var dbname = Environment.GetEnvironmentVariable("POSTGRE_DBNAME");
                var username = Environment.GetEnvironmentVariable("POSTGRE_USERNAME");
                var password = Environment.GetEnvironmentVariable("POSTGRE_PASSWORD");
                Console.WriteLine("Configuring DB");
                optionsBuilder.UseNpgsql($"Host={host};Username={username};Password={password};Database={dbname}", builder => {
                    builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(15), null);
                });
            }
#if DEBUG
            optionsBuilder.EnableDetailedErrors();
#endif
        }
    }

    public class Save {
        public int SaveId { get; set; }
        public byte[]? SerializedGame { get; set; }
    }
}