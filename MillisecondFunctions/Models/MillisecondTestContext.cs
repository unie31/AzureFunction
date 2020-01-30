using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace MillisecondFunctions.Models
{
    public partial class MillisecondTestContext : DbContext
    {
        private IConfigurationRoot configuration;
        public MillisecondTestContext(IConfigurationRoot config)
        {
            configuration = config;
        }

        public MillisecondTestContext(DbContextOptions<MillisecondTestContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Customer> Customer { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(configuration.GetConnectionString("connectionString"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("customer");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Attributes).HasMaxLength(1);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(1);

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(1);
            });
        }
    }
}
