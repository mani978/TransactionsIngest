using TransactionsIngest.App.Dtos;

namespace TransactionsIngest.App.Services;

public interface ITransactionFeedService
{
    Task<List<TransactionFeedItemDto>> GetTransactionsAsync(CancellationToken cancellationToken = default);
}