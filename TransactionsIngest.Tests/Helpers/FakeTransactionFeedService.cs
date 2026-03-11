using TransactionsIngest.App.Dtos;
using TransactionsIngest.App.Services;

namespace TransactionsIngest.Tests.Helpers;

public class FakeTransactionFeedService : ITransactionFeedService
{
    private readonly List<TransactionFeedItemDto> _items;

    public FakeTransactionFeedService(List<TransactionFeedItemDto> items)
    {
        _items = items;
    }

    public Task<List<TransactionFeedItemDto>> GetTransactionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items);
    }
}