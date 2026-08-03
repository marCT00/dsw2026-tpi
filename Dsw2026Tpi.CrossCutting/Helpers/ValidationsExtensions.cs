using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using System.Text.RegularExpressions;

namespace Dsw2026Tpi.CrossCutting.Helpers;

public static class ValidationsExtensions
{
    public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$";
    public static bool IsEmailValid(this string? email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
            Regex.IsMatch(email, EmailPattern);
    }

    public static void ValidateStringLength(string value, int min, int max, string errorCodeKey, string errorMessage) // M: funcion de validación de longitud, de lo que sea
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < min || value.Length > max)
        {
            throw new ValidationException(errorCodeKey, errorMessage);
        }
    }

    public static void ValidateMonthYear(int month, int year)
    {
        if (month < 1 || month > 12)
            throw new ValidationException(ErrorCodes.AVAILABILITY_INVALID_MONTH,nameof(ErrorCodes.AVAILABILITY_INVALID_MONTH));

        var today = DateTime.UtcNow;

        if (year < today.Year || (year == today.Year && month < today.Month))
            throw new ValidationException(ErrorCodes.AVAILABILITY_INVALID_YEAR,nameof(ErrorCodes.AVAILABILITY_INVALID_YEAR));
    }

    public static bool IsAlignedToGrid(TimeSpan time, int slotDurationMinutes) =>
        time.Minutes % slotDurationMinutes == 0 && time.Seconds == 0;

    public static void ValidateDayRules(IEnumerable<(TimeSpan Start, TimeSpan End)> days, int slotDurationMinutes)
    {
        if (!days.Any())
            throw new ValidationException(ErrorCodes.VALIDATION_ERROR,"Se debe proporcionar al menos una regla de día");

        foreach (var day in days)
        {
            if (day.Start >= day.End)
                throw new ValidationException(ErrorCodes.AVAILABILITY_INVALID_RANGE,nameof(ErrorCodes.AVAILABILITY_INVALID_RANGE));

            if (!IsAlignedToGrid(day.Start, slotDurationMinutes) || !IsAlignedToGrid(day.End, slotDurationMinutes))
                throw new ValidationException( ErrorCodes.AVAILABILITY_INVALID_DURATION,nameof(ErrorCodes.AVAILABILITY_INVALID_DURATION));
        }
    }

    public static void ValidateInternalOverlap(IEnumerable<(DayOfWeek DayOfWeek, TimeSpan Start, TimeSpan End)> days)
    {
        var grouped = days.GroupBy(d => d.DayOfWeek);
        foreach (var group in grouped)
        {
            var sorted = group.OrderBy(d => d.Start).ToList();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                if (sorted[i].Start < sorted[i + 1].End && sorted[i + 1].Start < sorted[i].End)
                {
                    throw new ConflictException(nameof(ErrorCodes.AVAILABILITY_OVERLAP),ErrorCodes.AVAILABILITY_OVERLAP);
                }
            }
        }
    }
}
