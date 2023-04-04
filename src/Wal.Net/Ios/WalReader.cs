using System.Runtime.CompilerServices;
using Standart.Hash.xxHash;
using Wal.Net.Exceptions;
using Wal.Net.Records;

namespace Wal.Net.Ios;

public class WalReader : IWalReader
{
    private readonly WalOptions _options;
    private readonly Stream _reader;

    public WalReader(Stream reader, WalOptions options)
    {
        _reader = reader;
        _options = options;
        if (_options.MaxRecordBodySize <= 0)
            throw new WalException("Invalid options. MaxRecordBodySize must be grater than 0.");
    }

    public long Offset { get; private set; }

    public async IAsyncEnumerable<WalRecord> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = await ReadAsync(cancellationToken);
            if (record is null)
                break;

            yield return record;
        }
    }

    public async Task<WalRecord?> ReadAsync(CancellationToken cancellationToken = default)
    {
        // Size
        var headerBuffer = new byte[WalConstants.HashSize + WalConstants.SeqNoSize + WalConstants.RecordSizeSize]
            .AsMemory();
        var read = await _reader.ReadAsync(headerBuffer, cancellationToken);
        if (read < headerBuffer.Length)
            return null; // partial record

        var hash = BitConverter.ToUInt64(headerBuffer[..WalConstants.HashSize].Span);
        var seqNo = BitConverter.ToInt64(
            headerBuffer[WalConstants.HashSize..(WalConstants.HashSize + WalConstants.SeqNoSize)].Span);
        var recordSize = BitConverter.ToInt64(headerBuffer[(WalConstants.HashSize + WalConstants.SeqNoSize)..].Span);
        if (recordSize > _options.MaxRecordBodySize)
            throw new WalRecordTooLargeException(_options.MaxRecordBodySize);
        var buffer = new byte[WalConstants.SeqNoSize + WalConstants.RecordSizeSize + recordSize].AsMemory();
        headerBuffer[WalConstants.HashSize..].CopyTo(buffer);

        read = await _reader.ReadAsync(buffer[(WalConstants.SeqNoSize + WalConstants.RecordSizeSize)..],
            cancellationToken); //seqno + size
        if (read < recordSize)
            return null; // partial record

        if (hash != xxHash3.ComputeHash(buffer.Span, buffer.Length))
            throw new WalCorruptedException();

        var record = new WalRecord(
            seqNo,
            buffer[(WalConstants.SeqNoSize + WalConstants.RecordSizeSize)..]
        );

        // move offset to last successfully read record
        Offset +=
            WalConstants.HashSize + WalConstants.SeqNoSize + WalConstants.RecordSizeSize +
            recordSize;

        return record;
    }
}