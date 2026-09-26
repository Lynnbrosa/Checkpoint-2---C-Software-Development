using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Name).IsRequired().HasMaxLength(80);
        builder.HasIndex(category => category.Name).IsUnique();

        // categorias fixas, entram pela migration e não por endpoint
        builder.HasData(
            new ExpenseCategory(1, "Transporte"),
            new ExpenseCategory(2, "Alimentação"),
            new ExpenseCategory(3, "Hospedagem"),
            new ExpenseCategory(4, "Material de escritório"),
            new ExpenseCategory(5, "Outros"));
    }
}
