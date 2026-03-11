namespace TransactionsIngest.App.Models;

public class TransactionAudit
{
    public int Id { get; set; }

    public int TransactionId { get; set; }

    public string ChangeType { get; set; } = string.Empty; // Insert, Update, Revoked, Finalized

    public string? FieldName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime ChangedAtUtc { get; set; }
}