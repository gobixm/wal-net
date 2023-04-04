using AutoFixture;
using FluentAssertions;
using Wal.Net.Exceptions;
using Wal.Net.Ios;
using Wal.Net.Records;

namespace Wal.Net.Tests;

public class WalWriterTests
{
    [Fact]
    public async void Write_Record_Written()
    {
        // arrange
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions());
        var body = new byte[] {4, 2, 42};
        var seqNo = 11;

        var record = new WalRecord(seqNo, body);

        // act
        await writer.WriteAsync(record);

        // assert
        memoryStream.Position = 0;
        var reader = new WalReader(memoryStream, new WalOptions());
        var writtenRecord = await reader.ReadAsync();
        writtenRecord.Should().NotBeNull();
        writtenRecord!.Body.ToArray().Should().BeEquivalentTo(record.Body.ToArray());
        writtenRecord.SeqNo.Should().Be(record.SeqNo);
    }

    [Fact]
    public async void Write_MultipleRecords_Written()
    {
        // arrange
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions());
        var fixture = new Fixture();
        const int expectedCount = 100;
        var records = fixture.CreateMany<WalRecord>(expectedCount).ToArray();

        // act
        await writer.WriteAsync(records);

        // assert
        memoryStream.Position = 0;
        var reader = new WalReader(memoryStream, new WalOptions());
        var writtenRecords = await reader.ReadAllAsync().ToArrayAsync();
        writtenRecords.Should().HaveCount(expectedCount);
        writtenRecords.First().Body.ToArray().Should().BeEquivalentTo(records.First().Body.ToArray());
        writtenRecords.Last().Body.ToArray().Should().BeEquivalentTo(records.Last().Body.ToArray());
    }

    [Fact]
    public async void Write_RecordTooLarge_Throws()
    {
        // arrange
        const long maxRecordSize = 10;
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions(10));
        var record = new Fixture().Build<WalRecord>().With(x => x.Body, new byte[maxRecordSize + 1])
            .Create();

        // act
        var action = () => writer.WriteAsync(record);

        // assert
        await action.Should().ThrowAsync<WalRecordTooLargeException>();
    }

    [Fact]
    public async void Write_MaxRecordSize_NotThrows()
    {
        // arrange
        const long maxRecordSize = 10;
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions(10));
        var record = new Fixture().Build<WalRecord>().With(x => x.Body, new byte[maxRecordSize])
            .Create();

        // act
        var action = () => writer.WriteAsync(record);

        // assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async void Write_Offset_Updated()
    {
        // arrange
        const int bodySize = 10;
        const int recordsCount = 10;
        using var memoryStream = new MemoryStream();
        var writer = new WalWriter(memoryStream, new WalOptions());
        var records = new Fixture().Build<WalRecord>().With(x => x.Body, new byte[bodySize])
            .CreateMany(recordsCount)
            .ToArray();

        // act
        foreach (var walRecord in records) await writer.WriteAsync(walRecord);

        // assert
        const int expectedRecordSize = 8 + 8 + 8 + bodySize; // hash + seqno + size + body
        writer.Offset.Should().Be(expectedRecordSize * recordsCount);
    }
}