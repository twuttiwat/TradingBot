namespace TradingBot;

// Data: Configuration records
public record Config(
    string Platform, // "Tradier" or "Schwab"
    string Token,
    string AccountId,
    string Symbol, // e.g., "AAPL"
    double RiskPerTradePercent, // e.g., 0.01
    double StopLossPercent, // e.g., 0.02
    double MaxPositionPercent, // e.g., 0.10
    StrategyConfig Strategy,
    string ConstraintSet // e.g., "Default"
);

public record StrategyConfig(string Name, int LookbackDays, int SmaPeriod = 5);

// Data: Normalized models
public record QuoteData(string Symbol, double Last);
public record DailyData(string Date, double High, double Low, double Close, double Volume);
public record BalanceData(double TotalCash);
public record Order(TrendSignal Signal, int Quantity, double EntryPrice, double StopLossPrice);
public record Result(bool Success, string Message);

// Calculation: Signal type
public enum TrendSignal { Buy, Sell, Hold }

// Data: API response models
public record TradierQuoteResponse(QuoteWrapper Quotes);
public record QuoteWrapper(QuoteData Quote);
public record TradierHistoryResponse(HistoryData History);
public record HistoryData(List<DailyData> Day);
public record TradierBalanceResponse(BalanceData Balances);
public record SchwabQuoteResponse(List<QuoteData> Quotes);
public record SchwabHistoryResponse(List<SchwabCandle> Candles);
public record SchwabCandle(DateTime Datetime, double High, double Low, double Close, double Volume);
public record SchwabBalanceResponse(SchwabAccount Account);
public record SchwabAccount(double CashBalance);