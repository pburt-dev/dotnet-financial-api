using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransactionReference)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(t => t.TransactionReference)
            .IsUnique();

        builder.Property(t => t.AccountId)
            .IsRequired();

        builder.HasIndex(t => t.AccountId);

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(t => new { t.AccountId, t.IdempotencyKey })
            .IsUnique();

        builder.Property(t => t.ProcessedAt)
            .IsRequired();

        builder.Property(t => t.CounterpartyAccountId);

        builder.HasIndex(t => t.CounterpartyAccountId);

        // Money value object mapping for Amount
        builder.OwnsOne(t => t.Amount, amountBuilder =>
        {
            amountBuilder.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            amountBuilder.Property(m => m.CurrencyCode)
                .HasColumnName("AmountCurrencyCode")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Money value object mapping for BalanceAfter
        builder.OwnsOne(t => t.BalanceAfter, balanceBuilder =>
        {
            balanceBuilder.Property(m => m.Amount)
                .HasColumnName("BalanceAfter")
                .HasPrecision(18, 2)
                .IsRequired();

            balanceBuilder.Property(m => m.CurrencyCode)
                .HasColumnName("BalanceAfterCurrencyCode")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Audit fields
        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100);

        builder.Property(t => t.LastModifiedAt);

        builder.Property(t => t.LastModifiedBy)
            .HasMaxLength(100);
    }
}
