using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Server.DbModel {
    public sealed class GameSavesContext : IdentityDbContext {
        public DbSet<Player> Players { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<Chunk> Chunks { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Level> Levels { get; set; }

        public GameSavesContext() {
        }

        public GameSavesContext(DbContextOptions<GameSavesContext> options)
            : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.IdentityUserId)
                .IsUnique();

            modelBuilder.Entity<Character>()
                .HasIndex(c => c.Name);

            modelBuilder.Entity<Chunk>()
                .HasIndex(ch => new {ch.chX, ch.chZ});
            
            modelBuilder.Entity<Level>()
                .HasIndex(l => l.Name);
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

    public class Player {
        [Key]
        public Guid PlayerId { get; set; } // Assuming you are using Guids for Identity

        public string IdentityUserId { get; set; } // This is the .NET Identity user Id. This could also be a foreign key to your identity table.

        // Navigation property for a list of characters associated with a player
        public ICollection<Character> Characters { get; set; }
    }

    public class Character {
        [Key]
        public Guid CharacterId { get; set; }

        public string Name { get; set; }

        // Foreign key to player
        public Guid PlayerId { get; set; }
        public Player Player { get; set; } // Navigation property
    }

    public class Level {
        [Key]
        public Guid LevelId { get; set; }
        public string Name { get; set; }
        
        public int Seed { get; set; }
        public int SpawnPointX { get; set; }
        public int SpawnPointY { get; set; }
        public int SpawnPointZ { get; set; }

        // Navigation property for a list of chunks associated with a level
        public ICollection<Chunk> Chunks { get; set; }

        // Foreign key to game
        public Guid GameId { get; set; }
        public Game Game { get; set; } // Navigation property
    }

    public class Chunk {
        [Key]
        public Guid ChunkId { get; set; }

        public short chX { get; set; }
        public short chZ { get; set; }
        public byte[] Cells { get; set; }

        // Foreign key to level
        public Guid LevelId { get; set; }
        public Level Level { get; set; } // Navigation property
    }

    public class Game {
        [Key]
        public Guid GameId { get; set; }

        public int Seed { get; set; }
        public int DataVersion { get; set; }

        // Navigation property for a list of levels associated with a game
        public ICollection<Level> Levels { get; set; }
    }
}