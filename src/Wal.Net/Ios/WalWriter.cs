using Standart.Hash.xxHash;
using Wal.Net.Exceptions;
using Wal.Net.Records;

namespace Wal.Net.Ios;

public sealed class WalWriter : IWalWriter
{
    private readonly WalOptions _options;
    private readonly Stream _stream;

    public WalWriter(Stream stream, WalOptions options)
    {
        _stream = stream;
        _options = options;
    }

    public long Offset { get; private set; }

    public async Task WriteAsync(IEnumerable<WalRecord> records, CancellationToken cancellationToken = default)
    {
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteAsync(record, cancellationToken);
        }
    }

    public async Task WriteAsync(WalRecord record, CancellationToken cancellationToken = default)
    {
        if (record.Body.Length > _options.MaxRecordBodySize)
            throw new WalRecordTooLargeException(_options.MaxRecordBodySize);

        var buffer = new byte[WalConstants.HashSize + WalConstants.SeqNoSize + WalConstants.RecordSizeSize +
                              record.Body.Length].AsMemory();

        // Sequence Number
        BitConverter.GetBytes(record.SeqNo)
            .CopyTo(buffer[WalConstants.HashSize..]);

        // Size
        BitConverter.GetBytes(record.Body.Length)
            .CopyTo(buffer[(WalConstants.HashSize + WalConstants.SeqNoSize)..]);

        // Body
        record.Body.CopyTo(buffer[(WalConstants.HashSize + WalConstants.SeqNoSize + WalConstants.RecordSizeSize)..]);

        // Hash
        var hash = xxHash3.ComputeHash(buffer[WalConstants.HashSize..].Span, buffer.Length - WalConstants.HashSize);
        BitConverter.GetBytes(hash)
            .CopyTo(buffer);
        await _stream.WriteAsync(buffer, cancellationToken);
        Offset += buffer.Length;
    }
}