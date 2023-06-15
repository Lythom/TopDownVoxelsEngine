using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Server.DbModel {
    public class GameSavesContext : IdentityDbContext {
        public virtual DbSet<DbPlayer> Players { get; set; }
        public virtual DbSet<DbCharacter> Characters { get; set; }
        public virtual DbSet<DbChunk> Chunks { get; set; }
        public virtual DbSet<DbGame> Games { get; set; }
        public virtual DbSet<DbLevel> Levels { get; set; }
        public virtual DbSet<DbNpc> Npcs { get; set; }

        public GameSavesContext() {
        }

        public GameSavesContext(DbContextOptions<GameSavesContext> options)
            : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<DbPlayer>()
                .HasIndex(p => p.IdentityUserId)
                .IsUnique();

            modelBuilder.Entity<DbCharacter>()
                .HasIndex(c => c.Name);

            modelBuilder.Entity<DbChunk>()
                .HasIndex(ch => new {chX = ch.ChX, chZ = ch.ChZ});

            modelBuilder.Entity<DbLevel>()
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

    public class DbPlayer {
        [Key]
        public Guid PlayerId { get; set; } // Assuming you are using Guids for Identity

        // Navigation property for a list of characters associated with a player
        public ICollection<DbCharacter> Characters { get; set; }

        public Guid IdentityUserId { get; set; }
        public IdentityUser IdentityUser { get; set; } // Navigation property
    }

    public class DbCharacter {
        [Key]
        public Guid CharacterId { get; set; }

        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public byte[] SerializedData { get; set; } // Shared.Character

        // Foreign key to player
        public Guid PlayerId { get; set; }
        public DbPlayer DbPlayer { get; set; } // Navigation property

        // Foreign key to Level
        public Guid LevelId { get; set; }
        public DbLevel DbLevel { get; set; } // Navigation property
    }

    public class DbNpc {
        [Key]
        public Guid NpcId { get; set; }

        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public byte[] SerializedData { get; set; } // Shared.Npc

        // Foreign key to Level
        public Guid LevelId { get; set; }
        public DbLevel DbLevel { get; set; } // Navigation property
    }

    public class DbLevel {
        [Key]
        public Guid LevelId { get; set; }

        public string Name { get; set; }

        public int Seed { get; set; }
        public float SpawnPointX { get; set; }
        public float SpawnPointY { get; set; }
        public float SpawnPointZ { get; set; }

        // Navigation property for a list of chunks associated with a level
        public ICollection<DbChunk> Chunks { get; set; }

        // Navigation property for a list of chunks associated with a level
        public ICollection<DbNpc> Npcs { get; set; }

        // Foreign key to game
        public Guid GameId { get; set; }
        public DbGame DbGame { get; set; } // Navigation property
    }

    public class DbChunk {
        [Key]
        public Guid ChunkId { get; set; }

        public short ChX { get; set; }
        public short ChZ { get; set; }
        public byte[] Cells { get; set; }
        public bool IsGenerated { get; set; }

        // Foreign key to level
        public Guid LevelId { get; set; }
        public DbLevel DbLevel { get; set; } // Navigation property
    }

    public class DbGame {
        [Key]
        public Guid GameId { get; set; }

        public int Seed { get; set; }
        public int DataVersion { get; set; }

        // Navigation property for a list of levels associated with a game
        public ICollection<DbLevel> Levels { get; set; }
    }
}