namespace Wal.Net.Exceptions;

public class WalRecordTooLargeException : WalException
{
    public WalRecordTooLargeException(long size) : base($"Wal record too large. Supported record size: {size}")
    {
    }
}