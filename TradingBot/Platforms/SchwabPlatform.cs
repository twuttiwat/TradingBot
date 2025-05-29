using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TradingBot.Platforms;

public class SchwabPlatform : IPlatform
{
    private static readonly HttpClient Client = new();

    public async Task<QuoteData?> FetchQuote(string symbol, string token)
    {
        var url = $"https://api.schwabapi.com/marketdata/v1/quotes?symbols={symbol}";
        var response = await SendGetRequest(url, token);
        return response != null
            ? JsonSerializer.Deserialize<SchwabQuoteResponse>(response)?.Quotes.FirstOrDefault()
            : null;
    }

    public async Task<List<DailyData>?> FetchDailyHistory(string symbol, int lookbackDays, string token)
    {
        var url = $"https://api.schwabapi.com/marketdata/v1/{symbol}/pricehistory?periodType=day&period={lookbackDays}&frequencyType=daily";
        var response = await SendGetRequest(url, token);
        return response != null
            ? JsonSerializer.Deserialize<SchwabHistoryResponse>(response)?.Candles
                .Select(c => new DailyData(c.Datetime.ToString("yyyy-MM-dd"), c.High, c.Low, c.Close, c.Volume)).ToList()
            : null;
    }

    public async Task<double?> FetchBalance(string accountId, string token)
    {
        var url = $"https://api.schwabapi.com/trader/v1/accounts/{accountId}";
        var response = await SendGetRequest(url, token);
        return response != null
            ? JsonSerializer.Deserialize<SchwabBalanceResponse>(response)?.Account?.CashBalance
            : null;
    }

    public async Task<Result> PlaceOrder(string accountId, string symbol, Order order, string token)
    {
        if (order.Quantity <= 0)
            return new Result(false, "Position size too small. No trade placed.");

        var url = $"https://api.schwabapi.com/trader/v1/accounts/{accountId}/orders";
        var orderData = new
        {
            orderType = "MARKET",
            session = "NORMAL",
            duration = "DAY",
            orderLegCollection = new[]
            {
                new
                {
                    instruction = order.Signal == TrendSignal.Buy ? "BUY" : "SELL",
                    quantity = order.Quantity,
                    instrument = new { symbol, assetType = "EQUITY" }
                }
            },
            orderStrategyType = "TRIGGER",
            childOrderStrategies = new[]
            {
                new
                {
                    orderType = "STOP",
                    session = "NORMAL",
                    duration = "DAY",
                    stopPrice = order.StopLossPrice,
                    orderLegCollection = new[]
                    {
                        new
                        {
                            instruction = order.Signal == TrendSignal.Buy ? "SELL" : "BUY",
                            quantity = order.Quantity,
                            instrument = new { symbol, assetType = "EQUITY" }
                        }
                    }
                }
            }
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