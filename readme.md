[![build and test](https://github.com/gobixm/wal-net/actions/workflows/build-and-test.yml/badge.svg?branch=master)](https://github.com/gobixm/wal-net/actions/workflows/build-and-test.yml)

# Usage example
```csharp

using System.Text;
using Wal.Net.Ios;
using Wal.Net.Records;

const long maxRecordSize = 1_000_000;
using var stream = new MemoryStream();

// Create writer on stream.
var writer = new WalWriter(stream, new WalOptions(maxRecordSize));

// Create write record.
await writer.WriteAsync(WalRecord.Create(42, "Wal.Net"u8.ToArray()));

// We'll use same stream for reading.
stream.Position = 0;

// Create reader on stream.
var reader = new WalReader(stream, new WalOptions(maxRecordSize));

// Read all records as IAsyncEnumerable.
var record = (await reader.ReadAllAsync().ToListAsync()).Single();

Console.WriteLine($"{record}, {Encoding.UTF8.GetString(record.Body.Span)}");

```
