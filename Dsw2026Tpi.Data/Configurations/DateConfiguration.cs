using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class DateConfiguration : IEntityTypeConfiguration<Date>
{
    public void Configure(EntityTypeBuilder<Date> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.AppointmentDate)
            .IsRequired();

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.Motive)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(d => d.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(d => d.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Turn)
            .WithOne(t => t.Date)
            .HasForeignKey<Date>(d => d.TurnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.TurnId)
            .IsUnique();
    }
}