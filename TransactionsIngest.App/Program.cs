using Microsoft.EntityFrameworkCore;
using TransactionsIngest.App.Config;
using TransactionsIngest.App.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionsIngest.App.Data;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.Configure<TransactionFeedSettings>(
        context.Configuration.GetSection("TransactionFeed"));

        services.AddScoped<ITransactionFeedService, MockTransactionFeedService>();
        services.AddScoped<ITransactionIngestionService, TransactionIngestionService>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var feedService = scope.ServiceProvider.GetRequiredService<ITransactionFeedService>();

dbContext.Database.Migrate();
// Console.WriteLine($"Transactions currently in DB: {dbContext.Transactions.Count()}");

// Console.WriteLine("Database is ready.");
// Console.WriteLine($"Base Directory: {AppContext.BaseDirectory}");
// Console.WriteLine($"DB Connection: {dbContext.Database.GetConnectionString()}");

var ingestionService = scope.ServiceProvider.GetRequiredService<ITransactionIngestionService>();
await ingestionService.ProcessSnapshotAsync();

// var transactions = await feedService.GetTransactionsAsync();
// Console.WriteLine($"Loaded {transactions.Count} transactions from mock feed.");

// foreach (var item in transactions)
// {
//     Console.WriteLine($"{item.TransactionId} | {item.ProductName} | {item.Amount} | {item.Timestamp:O}");
// }