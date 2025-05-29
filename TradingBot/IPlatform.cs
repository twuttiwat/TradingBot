namespace TradingBot;

public interface IPlatform
{
    Task<QuoteData?> FetchQuote(string symbol, string token);
    Task<List<DailyData>?> FetchDailyHistory(string symbol, int lookbackDays, string token);
    Task<double?> FetchBalance(string accountId, string token);
    Task<Result> PlaceOrder(string accountId, string symbol, Order order, string token);
}

public interface IConstraint
{
    Result CanTrade();
}