using System.Net;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

public class ConflictException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;

    public ConflictException(string errorCode, string message)
        : base(message, errorCode)
    {
    }
}
