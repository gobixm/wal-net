using AutoFixture;
using FluentAssertions;
using Wal.Net.Exceptions;
using Wal.Net.Ios;
using Wal.Net.Records;

namespace Wal.Net.Tests;

public sealed class WalReaderTests
{
    [Fact]
    public async void Read_Records_Read()
    {
        // arrange
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions());
        var body = new byte[] {4, 2, 42};
        var seqNo = 11;

        var record = new WalRecord(seqNo, body);
        await writer.WriteAsync(record);
        memoryStream.Position = 0;

        var reader = new WalReader(memoryStream, new WalOptions());

        // act
        var writtenRecord = await reader.ReadAsync();

        // assert
        writtenRecord.Should().NotBeNull();
        writtenRecord!.Body.ToArray().Should().BeEquivalentTo(record.Body.ToArray());
        writtenRecord.SeqNo.Should().Be(record.SeqNo);
    }

    [Fact]
    public async void Read_RecordTooLarge_Throws()
    {
        // arrange
        const long maxRecordSize = 10;
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions(maxRecordSize + 1));
        var record = new Fixture().Build<WalRecord>().With(x => x.Body, new byte[maxRecordSize + 1])
            .Create();
        await writer.WriteAsync(record);
        memoryStream.Position = 0;
        var reader = new WalReader(memoryStream, new WalOptions(maxRecordSize));

        // act
        var action = () => reader.ReadAsync();

        // assert
        await action.Should().ThrowAsync<WalRecordTooLargeException>();
    }

    [Fact]
    public async void Read_CorruptedRecord_Throws()
    {
        // arrange
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions());
        var record = new Fixture().Build<WalRecord>().With(x => x.Body, new byte[10])
            .Create();
        await writer.WriteAsync(record);
        memoryStream.Position -= 1;
        memoryStream.WriteByte((byte) (memoryStream.GetBuffer()[memoryStream.Length - 1] ^ 13));
        memoryStream.Position = 0;
        var reader = new WalReader(memoryStream, new WalOptions());

        // act
        var action = () => reader.ReadAsync();

        // assert
        await action.Should().ThrowAsync<WalCorruptedException>();
    }

    [Fact]
    public async void Read_PartialRecord_Skipped()
    {
        // arrange
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions());
        const int recordsCount = 10;
        const int recordSize = 10;
        const int stripSize = 5;

        var records = new Fixture().Build<WalRecord>().With(x => x.Body, new byte[recordSize])
            .CreateMany(recordsCount)
            .ToArray();
        await writer.WriteAsync(records);
        memoryStream.Position = 0;
        memoryStream.SetLength(memoryStream.Length - stripSize);
        var reader = new WalReader(memoryStream, new WalOptions());

        // act
        var readRecords = await reader.ReadAllAsync().ToArrayAsync();

        // assert
        readRecords.Should().HaveCount(recordsCount - 1);
        reader.Offset.Should().Be((24 + recordSize) * (recordsCount - 1));
    }

    [Fact]
    public async void ReadAll_Record_Read()
    {
        // arrange
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions());
        const int recordsCount = 10;
        const int recordSize = 10;

        var records = new Fixture().Build<WalRecord>().With(x => x.Body, new byte[recordSize])
            .CreateMany(recordsCount)
            .ToArray();
        await writer.WriteAsync(records);
        memoryStream.Position = 0;
        var reader = new WalReader(memoryStream, new WalOptions());

        // act
        var readRecords = await reader.ReadAllAsync().ToArrayAsync();

        // assert
        readRecords.Should().HaveCount(recordsCount);
    }
}