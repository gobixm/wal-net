using Wal.Net.Records;

namespace Wal.Net.Ios;

public interface IWalReader
{
    long Offset { get; }

    IAsyncEnumerable<WalRecord> ReadAllAsync(
        CancellationToken cancellationToken = default);

    Task<WalRecord?> ReadAsync(CancellationToken cancellationToken = default);
}