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

    private static void ValidateName(SpecialityModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 100)
            throw new ValidationException(ErrorCodes.SPECIALITY_INVALID_NAME, nameof(ErrorCodes.SPECIALITY_INVALID_NAME));

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length < 10 || request.Description.Length > 100)
            throw new ValidationException(ErrorCodes.SPECIALITY_INVALID_DESCRIPTION, nameof(ErrorCodes.SPECIALITY_INVALID_DESCRIPTION));
    }

}
