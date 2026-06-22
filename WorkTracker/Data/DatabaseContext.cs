using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace WorkTracker.Data
{
    public class DatabaseContext : DbContext
    {
        public DbSet<AppUsageLog> AppUsageLogs { get; set; } = null!;
        public DbSet<AppCategory> AppCategories { get; set; } = null!;
        public DbSet<TimeOffLog>  TimeOffLogs   { get; set; } = null!;
        public DbSet<AppSetting>  AppSettings   { get; set; } = null!;
        public DbSet<HolidayLog>  HolidayLogs   { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Define app data folder and sqlite database file
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folderPath = Path.Combine(appDataPath, "WorkTracker");
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            string dbPath = Path.Combine(folderPath, "worktracker.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed default categories
            modelBuilder.Entity<AppCategory>().HasData(
                new AppCategory { ProcessName = "devenv", CategoryName = "Work" },     // Visual Studio
                new AppCategory { ProcessName = "chrome", CategoryName = "Work" },      // Chrome
                new AppCategory { ProcessName = "msedge", CategoryName = "Work" },      // Edge
                new AppCategory { ProcessName = "teams", CategoryName = "Meeting" },    // Teams
                new AppCategory { ProcessName = "outlook", CategoryName = "Work" },    // Outlook
                new AppCategory { ProcessName = "spotify", CategoryName = "Leisure" },  // Spotify
                new AppCategory { ProcessName = "discord", CategoryName = "Leisure" }   // Discord
            );
        }
    }
}
