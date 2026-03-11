using System.Text.Json;
using Microsoft.Extensions.Options;
using TransactionsIngest.App.Config;
using TransactionsIngest.App.Dtos;

namespace TransactionsIngest.App.Services;

public class MockTransactionFeedService : ITransactionFeedService
{
    private readonly TransactionFeedSettings _settings;

    public MockTransactionFeedService(IOptions<TransactionFeedSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<List<TransactionFeedItemDto>> GetTransactionsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.MockJsonPath))
        {
            throw new InvalidOperationException("MockJsonPath is missing in configuration.");
        }

        var fullPath = Path.Combine(AppContext.BaseDirectory, _settings.MockJsonPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Mock JSON file not found at path: {fullPath}");
        }

        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var items = JsonSerializer.Deserialize<List<TransactionFeedItemDto>>(json, options);

        return items ?? new List<TransactionFeedItemDto>();
    }
}