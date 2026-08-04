using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace Dsw2026Tpi.Tests.Services;

public class AppointmentServiceTest
{
    private readonly Mock<IPersistence> _persistenceMock;
    private readonly AppointmentService _sut;

    public AppointmentServiceTest()
    {
        _persistenceMock = new Mock<IPersistence>();
        var loggerMock = new Mock<ILogger<AppointmentService>>();
        _sut = new AppointmentService(_persistenceMock.Object, loggerMock.Object);
    }

    private static (Doctor doctor, Turn turn) BuildDoctorAndTurn(DateTime scheduledDate)
    {
        var speciality = new Speciality("Cardiología", "Enfermedades del corazón");
        var doctor = new Doctor("Dr. Juan Perez", "MP1234", speciality);
        var availability = new Availability(scheduledDate.Month, scheduledDate.Year, scheduledDate.DayOfWeek,
            new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0), doctor);
        var turn = new Turn(scheduledDate, new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0), availability);
        return (doctor, turn);
    }

    //Camino Feliz
    [Fact]
    public async Task Create_CuandoLosDatosSonValidos_EntoncesElTurnoSeCrea()
    {

        var (doctor, turn) = BuildDoctorAndTurn(DateTime.UtcNow.Date.AddDays(1));
        var patient = new Patient("30111222", Guid.NewGuid().ToString());
        var request = new AppointmentModel.Request(doctor.Id, turn.Id,
            new AppointmentModel.PatientRequest(patient.Dni), "Control de rutina");

        _persistenceMock.Setup(p => p.GetById<Doctor>(doctor.Id, It.IsAny<string[]>()))
            .ReturnsAsync(doctor);
        _persistenceMock.Setup(p => p.First<Patient>(It.IsAny<Expression<Func<Patient, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync(patient);
        _persistenceMock.Setup(p => p.GetById<Turn>(turn.Id, It.IsAny<string[]>()))
            .ReturnsAsync(turn);
        _persistenceMock.Setup(p => p.Add(It.IsAny<Date>()))
            .Returns((Date d) => Task.FromResult(d));

       
        var result = await _sut.Create(request);

    
        Assert.Equal(DateState.BOOKED, result.Status);
        Assert.Equal(patient.Dni, result.PatientDni);
        Assert.Equal(TurnState.BOOKED, turn.State);
        _persistenceMock.Verify(p => p.Add(It.IsAny<Date>()), Times.Once);
    }

    // Camino B1: doctor inexistente
    [Fact]
    public async Task Create_CuandoElDoctorNoExiste_EntoncesLanzaEntityNotFoundException()
    {
       
        var doctorId = Guid.NewGuid();
        var request = new AppointmentModel.Request(doctorId, Guid.NewGuid(),
            new AppointmentModel.PatientRequest("30111222"), "Control de rutina");

        _persistenceMock.Setup(p => p.GetById<Doctor>(doctorId, It.IsAny<string[]>()))
            .ReturnsAsync((Doctor?)null);

      
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.Create(request));
        _persistenceMock.Verify(p => p.Add(It.IsAny<Date>()), Times.Never);
    }

    // Camino B2: turno no disponible
    [Fact]
    public async Task Create_CuandoElTurnoNoEstaDisponible_EntoncesLanzaConflictException()
    {
       
        var (doctor, turn) = BuildDoctorAndTurn(DateTime.UtcNow.Date.AddDays(1));
        turn.Block(); 
        var patient = new Patient("30111222", Guid.NewGuid().ToString());
        var request = new AppointmentModel.Request(doctor.Id, turn.Id,
            new AppointmentModel.PatientRequest(patient.Dni), "Control de rutina");

        _persistenceMock.Setup(p => p.GetById<Doctor>(doctor.Id, It.IsAny<string[]>()))
            .ReturnsAsync(doctor);
        _persistenceMock.Setup(p => p.First<Patient>(It.IsAny<Expression<Func<Patient, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync(patient);
        _persistenceMock.Setup(p => p.GetById<Turn>(turn.Id, It.IsAny<string[]>()))
            .ReturnsAsync(turn);

       
        await Assert.ThrowsAsync<ConflictException>(() => _sut.Create(request));
        _persistenceMock.Verify(p => p.Add(It.IsAny<Date>()), Times.Never);
    }

    // Camino B3: fecha pasada
    [Fact]
    public async Task Create_CuandoElTurnoEsDeUnaFechaPasada_EntoncesLanzaValidationException()
    {
       
        var (doctor, turn) = BuildDoctorAndTurn(DateTime.UtcNow.Date.AddDays(-1));
        var patient = new Patient("30111222", Guid.NewGuid().ToString());
        var request = new AppointmentModel.Request(doctor.Id, turn.Id,
            new AppointmentModel.PatientRequest(patient.Dni), "Control de rutina");

        _persistenceMock.Setup(p => p.GetById<Doctor>(doctor.Id, It.IsAny<string[]>()))
            .ReturnsAsync(doctor);
        _persistenceMock.Setup(p => p.First<Patient>(It.IsAny<Expression<Func<Patient, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync(patient);
        _persistenceMock.Setup(p => p.GetById<Turn>(turn.Id, It.IsAny<string[]>()))
            .ReturnsAsync(turn);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.Create(request));
        _persistenceMock.Verify(p => p.Add(It.IsAny<Date>()), Times.Never);
    }
}