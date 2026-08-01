using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Date : EntityBase
    {
        public DateTime AppointmentDate { get; init; }
        public DateTime? CancellationDate { get; private set; }
        public DateState Status { get; private set; }
        public string Motive { get; init; }

        public Guid? PatientId { get; set; }
        public Patient? Patient { get; private set; }

        public Guid? TurnId { get; set; }
        public Turn? Turn { get; private set; }

        #region Constructor for EF
#pragma warning disable CS8618
        private Date()
        {
        }
#pragma warning restore CS8618
        #endregion

        public Date(DateTime appointmentDate, Patient patient, Turn turn, string motive, Guid? id = null) : base(id)
        {
            AppointmentDate = appointmentDate;
            Patient = patient;
            Turn = turn;
            Motive = motive ?? string.Empty;
            Status = DateState.BOOKED;
        }

        public void Cancel(DateTime cancellationDate)
        {
            CancellationDate = cancellationDate;
            Status = DateState.CANCELLED;
        }

        public void Complete()
        {
            Status = DateState.ATTENDED;
        }

        public void NoShow()
        {
            Status = DateState.NO_SHOW;
        }
    }
}

