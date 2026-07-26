namespace Dsw2026Tpi.Domain.Entities;

public class Patient : EntityBase
{
    public string Dni { get; init; }
    public string Name { get; init; }
    public string PhoneNumber { get; init; }

    public ICollection<Date> Appointments { get; private set; } = new List<Date>();

    #region Constructor for EF
#pragma warning disable CS8618
    private Patient()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Patient(string dni, string name, string phoneNumber, Guid? id = null) : base(id)
    {
        Dni = dni;
        Name = name;
        PhoneNumber = phoneNumber;
    }
}