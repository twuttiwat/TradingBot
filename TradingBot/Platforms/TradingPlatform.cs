using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TradingBot.Platforms;

public class TradierPlatform : IPlatform
{
    private static readonly HttpClient Client = new();

    public async Task<QuoteData?> FetchQuote(string symbol, string token)
    {
        var url = $"https://api.tradier.com/v1/markets/quotes?symbols={symbol}";
        var response = await SendGetRequest(url, token);
        return response != null
            ? JsonSerializer.Deserialize<TradierQuoteResponse>(response)?.Quotes?.Quote
            : null;
    }

    public async Task<List<DailyData>?> FetchDailyHistory(string symbol, int lookbackDays, string token)
    {
        var url = $"https://api.tradier.com/v1/markets/history?symbol={symbol}&interval=daily" +
                  $"&start={DateTime.Now.AddDays(-lookbackDays):yyyy-MM-dd}&end={DateTime.Now:yyyy-MM-dd}";
        var response = await SendGetRequest(url, token);
        return response != null
            ? JsonSerializer.Deserialize<TradierHistoryResponse>(response)?.History?.Day
            : null;
    }

    public async Task<double?> FetchBalance(string accountId, string token)
    {
        var url = $"https://api.tradier.com/v1/accounts/{accountId}/balances";
        var response = await SendGetRequest(url, token);
        return response != null
            ? JsonSerializer.Deserialize<TradierBalanceResponse>(response)?.Balances?.TotalCash
            : null;
    }

    public async Task<Result> PlaceOrder(string accountId, string symbol, Order order, string token)
    {
        if (order.Quantity <= 0)
            return new Result(false, "Position size too small. No trade placed.");

        var url = $"https://api.tradier.com/v1/accounts/{accountId}/orders";
        var orderData = new
        {
            @class = "equity",
            symbol,
            side = order.Signal == TrendSignal.Buy ? "buy" : "sell",
            quantity = order.Quantity,
            type = "market",
            duration = "day",
            stop = order.StopLossPrice
        };

        Client.DefaultRequestHeaders.Clear();
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        Client.DefaultRequestHeaders.Add("Accept", "application/json");

        var content = new StringContent(JsonSerializer.Serialize(orderData), System.Text.Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(url, content);
        return response.IsSuccessStatusCode
            ? new Result(true, $"Order placed: {order.Signal} {order.Quantity} shares of {symbol} at ${order.EntryPrice:F2} with stop-loss at ${order.StopLossPrice:F2}")
            : new Result(false, $"Order failed: {response.StatusCode}");
    }

    private static async Task<string?> SendGetRequest(string url, string token)
    {
        Client.DefaultRequestHeaders.Clear();
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        Client.DefaultRequestHeaders.Add("Accept", "application/json");
        var response = await Client.GetAsync(url);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
    }
}