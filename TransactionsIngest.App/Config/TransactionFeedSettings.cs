namespace TransactionsIngest.App.Config;

public class TransactionFeedSettings
{
    public bool UseMockFeed { get; set; }

    public string MockJsonPath { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;
}