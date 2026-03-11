using Microsoft.EntityFrameworkCore;
using TransactionsIngest.App.Models;

namespace TransactionsIngest.App.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionAudit> TransactionAudits => Set<TransactionAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");

            entity.HasKey(t => t.Id);

            entity.HasIndex(t => t.TransactionId).IsUnique();

            entity.Property(t => t.TransactionId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(t => t.CardLast4)
                .IsRequired()
                .HasMaxLength(4);

            entity.Property(t => t.LocationCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(t => t.ProductName)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(t => t.Amount)
                .HasPrecision(18, 2);

            entity.Property(t => t.Status)
                .IsRequired()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<TransactionAudit>(entity =>
        {
            entity.ToTable("TransactionAudits");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.TransactionId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.ChangeType)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(a => a.FieldName)
                .HasMaxLength(50);

            entity.Property(a => a.OldValue)
                .HasMaxLength(200);

            entity.Property(a => a.NewValue)
                .HasMaxLength(200);
        });
    }
}