using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción que se lanza cuando falla la autenticación del usuario.
/// </summary>
public class AuthenticationException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;

    public AuthenticationException()
        : base(
            ErrorCodes.AUTHENTICATION_FAILED,
            nameof(ErrorCodes.AUTHENTICATION_FAILED))
    {
    }
}
