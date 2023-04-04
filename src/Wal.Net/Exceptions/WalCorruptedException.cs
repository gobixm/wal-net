namespace Wal.Net.Exceptions;

public class WalCorruptedException : WalException
{
    public WalCorruptedException() : base("Wal corrupted.")
    {
    }
}