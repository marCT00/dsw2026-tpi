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
}
