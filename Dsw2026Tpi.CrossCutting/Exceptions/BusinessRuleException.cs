using System.Net;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción que se lanza cuando se viola una regla de negocio.
/// </summary>
public class BusinessRuleException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.UnprocessableEntity;

    public BusinessRuleException(string message, string errorCode)
        : base(message, errorCode)
    {
    }
}
