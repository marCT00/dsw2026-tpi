using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción que se lanza cuando el usuario no tiene permisos suficientes.
/// </summary>
public class AuthorizationException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;

    public AuthorizationException()
        : base(ErrorCodes.AUTHORIZATION_FAILED, nameof(ErrorCodes.AUTHORIZATION_FAILED))
    {
    }
}
