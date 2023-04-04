namespace Wal.Net.Ios;

public sealed record WalOptions(
    long MaxRecordBodySize = 1 * 1024 * 1024);