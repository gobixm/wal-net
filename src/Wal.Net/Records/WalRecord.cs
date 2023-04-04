namespace Wal.Net.Records;

public sealed record WalRecord(
    long SeqNo,
    ReadOnlyMemory<byte> Body)
{
    public static WalRecord Create(long seqNo, ReadOnlyMemory<byte> body) => new(seqNo, body);

    public static WalRecord Create(long seqNo, byte[] body) => new(seqNo, body.AsMemory());

    public override string ToString() => $"seq_no: {SeqNo}, size: {Body.Length}";
}