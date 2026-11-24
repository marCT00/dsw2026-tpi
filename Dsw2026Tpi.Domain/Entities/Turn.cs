using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Turn : EntityBase
    {
        public DateTime ScheduledDate { get; init; }
        public TimeSpan StartTime { get; init; }
        public TimeSpan EndTime { get; init; }
        public TurnState State { get; private set; }
        public byte[] RowVersion { get; set; } = [];

        public Guid? AvailabilityId { get; set; }
        public Availability? Availability { get; private set; }

        public ICollection<Date> Appointments { get; private set; } = new List<Date>();

        #region Constructor for EF
#pragma warning disable CS8618
        private Turn()
        {
        }
#pragma warning restore CS8618
        #endregion

        public Turn(
            DateTime scheduledDate,
            TimeSpan startTime,
            TimeSpan endTime,
            Availability availability,
            Guid? id = null) : base(id)
        {
            ScheduledDate = scheduledDate;
            StartTime = startTime;
            EndTime = endTime;
            Availability = availability;
            State = TurnState.AVAILABLE;
        }

        public void Reserve(Date date)
        {
            if (!Appointments.Contains(date))
            {
                Appointments.Add(date);
            }

            State = TurnState.BOOKED;
        }

        public void Block()
        {
            State = TurnState.BLOCKED;
        }

        public void Release()
        {
            State = TurnState.AVAILABLE;
        }
    }
}