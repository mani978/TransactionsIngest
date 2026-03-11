namespace TransactionsIngest.App.Services;

public interface ITransactionIngestionService
{
    Task ProcessSnapshotAsync(CancellationToken cancellationToken = default);
}