using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

internal sealed class ExpenseHistoryConfiguration : IEntityTypeConfiguration<ExpenseHistory>
{
    public void Configure(EntityTypeBuilder<ExpenseHistory> builder)
    {
        builder.ToTable("ExpenseHistory");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.ActorId).IsRequired().HasMaxLength(450);
        builder.Property(entry => entry.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.ToStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.Justification).HasMaxLength(500);
        builder.HasIndex(entry => new { entry.ExpenseId, entry.OccurredAt });
    }
}
