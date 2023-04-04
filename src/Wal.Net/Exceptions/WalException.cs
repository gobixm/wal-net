namespace Wal.Net.Exceptions;

public class WalException : Exception
{
    public WalException(string? message, Exception? innerException = null) : base(message, innerException)
    {
    }
}