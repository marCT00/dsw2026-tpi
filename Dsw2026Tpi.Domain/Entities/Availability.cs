using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities;

public class Availability : EntityBase
{
    public int Month { get; init; }
    public int Year { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }

    public Guid? DoctorId { get; set; }
    public Doctor? Doctor { get; private set; }

    public ICollection<Turn> Turns { get; private set; } = new List<Turn>();

    #region Constructor for EF
#pragma warning disable CS8618
    private Availability()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Availability(
        int month,
        int year,
        DayOfWeek dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime,
        Doctor doctor,
        Guid? id = null) : base(id)
    {
        Month = month;
        Year = year;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        Doctor = doctor;
    }
}
