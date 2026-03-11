namespace TransactionsIngest.App.Dtos;

public class TransactionFeedItemDto
{
    public int TransactionId { get; set; }

    public string CardNumber { get; set; } = string.Empty;

    public string LocationCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Timestamp { get; set; } = string.Empty;
}