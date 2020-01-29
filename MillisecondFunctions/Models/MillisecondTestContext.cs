using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MillisecondFunctions.Models
{
    public partial class MillisecondTestContext : DbContext
    {
        public MillisecondTestContext()
        {
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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=tcp:millisecondsqlserver.database.windows.net,1433;Initial Catalog=MillisecondTest;Persist Security Info=False;User ID=Urho;Password=Millisecond20%;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
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
