using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(expense => expense.Id);
        builder.Property(expense => expense.Id).ValueGeneratedNever();
        builder.Property(expense => expense.OwnerId).IsRequired().HasMaxLength(450);
        builder.Property(expense => expense.Description).IsRequired().HasMaxLength(500);
        builder.Property(expense => expense.Amount).HasPrecision(12, 2);
        builder.Property(expense => expense.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(expense => expense.RejectionReason).HasMaxLength(500);

        // duas decisões simultâneas no mesmo reembolso: a segunda falha no save em vez de duplicar histórico
        builder.Property(expense => expense.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(expense => expense.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(expense => expense.Category)
            .WithMany()
            .HasForeignKey(expense => expense.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // histórico e pagamento são trilha de auditoria, nunca somem em cascata
        builder.HasMany(expense => expense.History)
            .WithOne()
            .HasForeignKey(entry => entry.ExpenseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(expense => expense.Payment)
            .WithOne()
            .HasForeignKey<PaymentRecord>(payment => payment.ExpenseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(expense => expense.OwnerId);
        builder.HasIndex(expense => expense.Status);
    }
}
