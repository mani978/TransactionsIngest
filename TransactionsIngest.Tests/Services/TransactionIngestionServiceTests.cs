using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TransactionsIngest.App.Data;
using TransactionsIngest.App.Dtos;
using TransactionsIngest.App.Models;
using TransactionsIngest.App.Services;
using TransactionsIngest.Tests.Helpers;
using Microsoft.Data.Sqlite;


namespace TransactionsIngest.Tests.Services;

public class TransactionIngestionServiceTests
{
    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite(connection)
        .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task ProcessSnapshotAsync_ShouldInsertNewTransactions()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var dbContext = CreateDbContext(connection);

        var feedItems = new List<TransactionFeedItemDto>
        {
            new()
            {
                TransactionId = 1001,
                CardNumber = "4111111111111111",
                LocationCode = "STO-01",
                ProductName = "Wireless Mouse",
                Amount = 19.99m,
                Timestamp = "2026-03-10T21:20:00Z"
            },
            new()
            {
                TransactionId = 1002,
                CardNumber = "4000000000000002",
                LocationCode = "STO-02",
                ProductName = "USB-C Cable",
                Amount = 25.00m,
                Timestamp = "2026-03-10T21:25:00Z"
            }
        };

        var feedService = new FakeTransactionFeedService(feedItems);
        var service = new TransactionIngestionService(dbContext, feedService);

        await service.ProcessSnapshotAsync();

        dbContext.Transactions.Count().Should().Be(2);
        dbContext.TransactionAudits.Count().Should().Be(2);
    }

    [Fact]
    public async Task ProcessSnapshotAsync_ShouldUpdateExistingTransaction_WhenAmountChanges()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var dbContext = CreateDbContext(connection);

        dbContext.Transactions.Add(new Transaction
        {
            TransactionId = 1001,
            CardLast4 = "1111",
            LocationCode = "STO-01",
            ProductName = "Wireless Mouse",
            Amount = 19.99m,
            TransactionTimeUtc = DateTime.Parse("2026-03-10T21:20:00Z").ToUniversalTime(),
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            LastSeenInSnapshotAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var feedItems = new List<TransactionFeedItemDto>
        {
            new()
            {
                TransactionId = 1001,
                CardNumber = "4111111111111111",
                LocationCode = "STO-01",
                ProductName = "Wireless Mouse",
                Amount = 29.99m,
                Timestamp = "2026-03-10T21:20:00Z"
            }
        };

        var feedService = new FakeTransactionFeedService(feedItems);
        var service = new TransactionIngestionService(dbContext, feedService);

        await service.ProcessSnapshotAsync();

        var transaction = dbContext.Transactions.Single(t => t.TransactionId == 1001);
        transaction.Amount.Should().Be(29.99m);

        dbContext.TransactionAudits.Any(a =>
            a.TransactionId == 1001 &&
            a.ChangeType == "Update" &&
            a.FieldName == "Amount").Should().BeTrue();
    }

    [Fact]
    public async Task ProcessSnapshotAsync_ShouldRevokeMissingTransaction()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var dbContext = CreateDbContext(connection);

        dbContext.Transactions.AddRange(
            new Transaction
            {
                TransactionId = 1001,
                CardLast4 = "1111",
                LocationCode = "STO-01",
                ProductName = "Wireless Mouse",
                Amount = 19.99m,
                TransactionTimeUtc = DateTime.UtcNow.AddHours(-1),
                Status = "Active",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new Transaction
            {
                TransactionId = 1002,
                CardLast4 = "0002",
                LocationCode = "STO-02",
                ProductName = "USB-C Cable",
                Amount = 25.00m,
                TransactionTimeUtc = DateTime.UtcNow.AddHours(-1),
                Status = "Active",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var feedItems = new List<TransactionFeedItemDto>
        {
            new()
            {
                TransactionId = 1001,
                CardNumber = "4111111111111111",
                LocationCode = "STO-01",
                ProductName = "Wireless Mouse",
                Amount = 19.99m,
                Timestamp = DateTime.UtcNow.AddHours(-1).ToString("O")
            }
        };

        var feedService = new FakeTransactionFeedService(feedItems);
        var service = new TransactionIngestionService(dbContext, feedService);

        await service.ProcessSnapshotAsync();

        var revoked = dbContext.Transactions.Single(t => t.TransactionId == 1002);
        revoked.Status.Should().Be("Revoked");

        dbContext.TransactionAudits.Any(a =>
            a.TransactionId == 1002 &&
            a.ChangeType == "Revoked").Should().BeTrue();
    }
}