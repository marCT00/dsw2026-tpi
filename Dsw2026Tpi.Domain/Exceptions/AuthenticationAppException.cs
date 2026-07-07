namespace Dsw2026Tpi.Domain.Exceptions;

public class AuthenticationAppException : Exception
{
    public AuthenticationAppException() : base("Usuario o contraseña incorrectos")
    {
    }
}

