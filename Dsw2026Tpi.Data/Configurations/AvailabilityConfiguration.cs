using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("AvailabilityRules");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Month)
            .IsRequired();

        builder.Property(a => a.Year)
            .IsRequired();

        builder.Property(a => a.DayOfWeek)
            .IsRequired();

        builder.Property(a => a.StartTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(a => a.EndTime)
            .IsRequired()
            .HasColumnType("time");

        builder.HasIndex(a => new { a.DoctorId, a.Year, a.Month, a.DayOfWeek, a.StartTime, a.EndTime })
            .IsUnique();

        builder.HasOne(a => a.Doctor)
            .WithMany(d => d.Availabilities)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
