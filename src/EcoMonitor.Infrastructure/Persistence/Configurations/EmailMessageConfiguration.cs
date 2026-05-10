using EcoMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoMonitor.Infrastructure.Persistence.Configurations;

public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.Property(m => m.ToAddress).IsRequired().HasMaxLength(320);
        builder.Property(m => m.ToName).HasMaxLength(200);
        builder.Property(m => m.Subject).IsRequired().HasMaxLength(300);
        builder.Property(m => m.HtmlBody).IsRequired();
        builder.Property(m => m.TemplateName).HasMaxLength(100);
        builder.Property(m => m.LastError).HasMaxLength(1000);

        // Worker polls (Status = Pending AND NextAttemptAt <= now); compound index
        // makes that scan cheap once the table grows.
        builder.HasIndex(m => new { m.Status, m.NextAttemptAt });
        builder.HasIndex(m => m.RelatedEntityId);
    }
}
