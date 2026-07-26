namespace Dsw2026Tpi.Domain.Entities;

public class Patient : EntityBase
{
    public string Dni { get; init; }
    public string UserId { get; init; }
    public string Name { get; private set; }
    public string PhoneNumber { get; private set; }

    public ICollection<Date> Appointments { get; private set; } = new List<Date>();

    #region Constructor for EF
#pragma warning disable CS8618
    private Patient() { }
#pragma warning restore CS8618
    #endregion

    public Patient(string dni, string userId, string? name = null, string? phoneNumber = null, Guid? id = null) : base(id)
    {
        Dni = dni;
        UserId = userId;
        Name = name ?? string.Empty;
        PhoneNumber = phoneNumber ?? string.Empty;
    }

    public void UpdateProfile(string name, string phoneNumber)
    {
        Name = name;
        PhoneNumber = phoneNumber;
    }
}