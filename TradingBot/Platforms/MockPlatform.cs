using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradingBot.Platforms;

public class MockPlatform : IPlatform
{
    private readonly double balance;
    private readonly List<DailyData> history;
    private readonly QuoteData quote;

    public MockPlatform(
        double balance = 10000.0,
        List<DailyData>? history = null,
        QuoteData? quote = null)
    {
        this.balance = balance;
        this.history = history ?? new List<DailyData>
        {
            new("2025-05-24", 100, 90, 95, 1000),
            new("2025-05-25", 105, 95, 100, 1200),
            new("2025-05-26", 110, 100, 105, 1100),
            new("2025-05-27", 108, 98, 103, 1300),
            new("2025-05-28", 112, 102, 110, 2000) // High volume
        };
        this.quote = quote ?? new QuoteData("AAPL", 115.0);
    }

    public Task<double?> FetchBalance(string accountId, string token)
    {
        return Task.FromResult<double?>(balance);
    }

    public Task<List<DailyData>?> FetchDailyHistory(string symbol, int lookbackDays, string token)
    {
        return Task.FromResult<List<DailyData>?>(history.TakeLast(lookbackDays).ToList());
    }

    public Task<QuoteData?> FetchQuote(string symbol, string token)
    {
        return Task.FromResult<QuoteData?>(quote with { Symbol = symbol });
    }

    public Task<Result> PlaceOrder(string accountId, string symbol, Order order, string token)
    {
        if (order.Quantity <= 0)
            return Task.FromResult(new Result(false, "Position size too small. No trade placed."));

        return Task.FromResult(new Result(true,
            $"Order placed: {order.Signal} {order.Quantity} shares of {symbol} at ${order.EntryPrice:F2} with stop-loss at ${order.StopLossPrice:F2}"));
    }
}