using Challenge.Models;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Store> Stores { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Owner).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StoreId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.TransactionType);

            entity.HasOne(e => e.Store)
                .WithMany(s => s.Transactions)
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .IsRequired();

            entity.Property(e => e.Cpf)
                .IsRequired()
                .HasMaxLength(11);

            entity.Property(e => e.Card)
                .IsRequired()
                .HasMaxLength(12);

            entity.Property(e => e.Nature)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(255);
        });
    }
}

