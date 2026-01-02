using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcessMonitorApi.Contracts;

namespace ProcessMonitorApi.Repository;

public class AnalysisConfiguration : IEntityTypeConfiguration<Analysis>
{
    public void Configure(EntityTypeBuilder<Analysis> builder)
    {
        builder.ToTable("Analysis"); // Custom table name

        builder.HasKey(p => p.Id); // I will use Id as primary key and not a composite key

        builder.Property(p => p.Action)
            .IsRequired();

        builder.Property(p => p.Guideline)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.Confidence)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(p => new { p.Action, p.Guideline })
            .HasDatabaseName("IX_Analysis_Action_Guideline")
            .IsUnique(); // Makes the combination of Action and Guideline unique

        // Index for performance
        builder.HasIndex(p => p.Id).IsUnique();
    }
}
