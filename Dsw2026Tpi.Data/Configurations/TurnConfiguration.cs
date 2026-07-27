using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class TurnConfiguration : IEntityTypeConfiguration<Turn>
{
    public void Configure(EntityTypeBuilder<Turn> builder)
    {
        builder.ToTable("AvailabilitySlots");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ScheduledDate)
            .IsRequired();

        builder.Property(t => t.StartTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(t => t.EndTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(t => t.State)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(t => t.Availability)
            .WithMany(a => a.Turns)
            .HasForeignKey(t => t.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);

      
        builder.Ignore(t => t.DateId);
    }
}