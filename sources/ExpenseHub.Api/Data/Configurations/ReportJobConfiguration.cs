using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

internal sealed class ReportJobConfiguration : IEntityTypeConfiguration<ReportJob>
{
    public void Configure(EntityTypeBuilder<ReportJob> builder)
    {
        builder.ToTable("ReportJobs");
        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).ValueGeneratedNever();
        builder.Property(job => job.OwnerId).IsRequired().HasMaxLength(450);
        builder.Property(job => job.Scope).HasConversion<string>().HasMaxLength(20);
        builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(job => job.ErrorCode).HasMaxLength(80);
        builder.Property(job => job.ErrorMessage).HasMaxLength(500);
        builder.Property(job => job.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(job => job.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(job => job.OwnerId);
        builder.HasIndex(job => job.Status);
    }
}

