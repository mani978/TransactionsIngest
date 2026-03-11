using Microsoft.EntityFrameworkCore;
using TransactionsIngest.App.Data;
using TransactionsIngest.App.Dtos;
using TransactionsIngest.App.Models;


namespace TransactionsIngest.App.Services;

public class TransactionIngestionService : ITransactionIngestionService
{
    private readonly AppDbContext _dbContext;
    private readonly ITransactionFeedService _feedService;

    public TransactionIngestionService(
        AppDbContext dbContext,
        ITransactionFeedService feedService)
    {
        _dbContext = dbContext;
        _feedService = feedService;
    }

    public async Task ProcessSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _feedService.GetTransactionsAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var windowStartUtc = nowUtc.AddHours(-24);

        using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var currentSnapshotIds = new HashSet<int>(
            snapshot.Select(x => x.TransactionId));

        int inserted = 0;
        int updated = 0;
        int revoked = 0;
        int finalized = 0;
        int noChange = 0;

        foreach (var item in snapshot)
        {
            var existing = await _dbContext.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == item.TransactionId, cancellationToken);

            if (existing == null)
            {
                var newTransaction = new Transaction
                {
                    TransactionId = item.TransactionId,
                    CardLast4 = GetLast4(item.CardNumber),
                    LocationCode = item.LocationCode.Trim(),
                    ProductName = item.ProductName.Trim(),
                    Amount = item.Amount,
                    TransactionTimeUtc = ParseUtcTimestamp(item.Timestamp),
                    Status = "Active",
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc,
                    LastSeenInSnapshotAtUtc = nowUtc
                };

                _dbContext.Transactions.Add(newTransaction);

                _dbContext.TransactionAudits.Add(new TransactionAudit
                {
                    TransactionId = item.TransactionId,
                    ChangeType = "Insert",
                    FieldName = null,
                    OldValue = null,
                    NewValue = "Inserted new transaction",
                    ChangedAtUtc = nowUtc
                });

                inserted++;
                continue;
            }

            if (existing.Status == "Finalized")
            {
                noChange++;
                continue;
            }

            bool hasChanges = false;

            hasChanges |= CompareAndTrack(existing, "CardLast4", existing.CardLast4, GetLast4(item.CardNumber), nowUtc);
            hasChanges |= CompareAndTrack(existing, "LocationCode", existing.LocationCode, item.LocationCode.Trim(), nowUtc);
            hasChanges |= CompareAndTrack(existing, "ProductName", existing.ProductName, item.ProductName.Trim(), nowUtc);
            hasChanges |= CompareAndTrack(existing, "Amount", existing.Amount.ToString("0.00"), item.Amount.ToString("0.00"), nowUtc);
            hasChanges |= CompareAndTrack(existing, "TransactionTimeUtc", new DateTimeOffset(DateTime.SpecifyKind(existing.TransactionTimeUtc, DateTimeKind.Utc)).ToUnixTimeSeconds().ToString(), DateTimeOffset.Parse(item.Timestamp).ToUnixTimeSeconds().ToString(), nowUtc);

            if (hasChanges)
            {
                existing.CardLast4 = GetLast4(item.CardNumber);
                existing.LocationCode = item.LocationCode.Trim();
                existing.ProductName = item.ProductName.Trim();
                existing.Amount = item.Amount;
                existing.TransactionTimeUtc = ParseUtcTimestamp(item.Timestamp);
                existing.UpdatedAtUtc = nowUtc;

                updated++;
            }
            else
            {
                if (existing.TransactionTimeUtc >= windowStartUtc)
                {


                    noChange++;
                }
            }

            existing.LastSeenInSnapshotAtUtc = nowUtc;
        }

        var existingRecentTransactions = await _dbContext.Transactions
            .Where(t => t.TransactionTimeUtc >= windowStartUtc &&
                        t.Status != "Revoked" &&
                        t.Status != "Finalized")
            .ToListAsync(cancellationToken);

        foreach (var existing in existingRecentTransactions)
        {
            if (!currentSnapshotIds.Contains(existing.TransactionId))
            {
                existing.Status = "Revoked";
                existing.UpdatedAtUtc = nowUtc;

                _dbContext.TransactionAudits.Add(new TransactionAudit
                {
                    TransactionId = existing.TransactionId,
                    ChangeType = "Revoked",
                    FieldName = "Status",
                    OldValue = "Active",
                    NewValue = "Revoked",
                    ChangedAtUtc = nowUtc
                });

                revoked++;
            }
        }

        var oldTransactions = await _dbContext.Transactions
            .Where(t => t.TransactionTimeUtc < windowStartUtc &&
                        t.Status != "Finalized")
            .ToListAsync(cancellationToken);

        foreach (var oldTransaction in oldTransactions)
        {
            var previousStatus = oldTransaction.Status;

            oldTransaction.Status = "Finalized";
            oldTransaction.UpdatedAtUtc = nowUtc;

            _dbContext.TransactionAudits.Add(new TransactionAudit
            {
                TransactionId = oldTransaction.TransactionId,
                ChangeType = "Finalized",
                FieldName = "Status",
                OldValue = previousStatus,
                NewValue = "Finalized",
                ChangedAtUtc = nowUtc
            });

            finalized++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        Console.WriteLine("Ingestion run completed.");
        Console.WriteLine($"Inserted: {inserted}");
        Console.WriteLine($"Updated: {updated}");
        Console.WriteLine($"Revoked: {revoked}");
        Console.WriteLine($"Finalized: {finalized}");
        Console.WriteLine($"No Change: {noChange}");
    }






    private bool CompareAndTrack(Transaction existing, string fieldName, string oldValue, string newValue, DateTime changedAtUtc)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return false;
        }
        // Used for debugging
        // Console.WriteLine($"Update detected for {existing.TransactionId} | Field: {fieldName} | Old: {oldValue} | New: {newValue}");

        _dbContext.TransactionAudits.Add(new TransactionAudit
        {
            TransactionId = existing.TransactionId,
            ChangeType = "Update",
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAtUtc = changedAtUtc
        });

        return true;
    }

    private static DateTime ParseUtcTimestamp(string timestamp)
    {
        return DateTimeOffset.Parse(timestamp).UtcDateTime;
    }

    private static string GetLast4(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
        {
            return string.Empty;
        }

        return cardNumber[^4..];
    }
}