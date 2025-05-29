using System;
using System.Linq;
using System.Threading.Tasks;

namespace TradingBot;

public static class TradingProgram
{
    // Action: Main entry point
    public static async Task Run(Config config, IPlatform platform, IConstraint constraint)
    {
        var result = constraint.CanTrade() switch
        {
            { Success: false, Message: var msg } => msg,
            _ => await ExecuteTradePipeline(config, platform, constraint)
        };
        Console.WriteLine(result);
    }

    // Action: Trade pipeline
    private static async Task<string> ExecuteTradePipeline(Config config, IPlatform platform, IConstraint constraint)
    {
        var balance = await platform.FetchBalance(config.AccountId, config.Token);
        if (!balance.HasValue)
            return "Failed to fetch account balance.";

        var history = await platform.FetchDailyHistory(config.Symbol, config.Strategy.LookbackDays, config.Token);
        if (history == null || history.Count == 0)
            return "Failed to fetch daily history data.";

        var quote = await platform.FetchQuote(config.Symbol, config.Token);
        if (quote == null)
            return "Failed to fetch real-time quote.";

        var signal = DetectSignal(config.Strategy, quote, history);
        var order = CalculateOrder(quote, balance.Value, signal, config);
        if (order.Signal == TrendSignal.Hold)
            return "No clear trend: Hold.";

        var result = await platform.PlaceOrder(config.AccountId, config.Symbol, order, config.Token);
        if (result.Success)
            Constraints.DefaultConstraint.RecordTrade();

        return result.Message;
    }

    // Calculation: Detect signal based on strategy
    private static TrendSignal DetectSignal(StrategyConfig strategy, QuoteData quote, List<DailyData> history)
    {
        return strategy.Name switch
        {
            "TrendFollowing" => DetectTrendFollowing(quote, history, strategy.LookbackDays),
            "MeanReversion" => DetectMeanReversion(quote, history, strategy.SmaPeriod),
            "Breakout" => DetectBreakout(quote, history, strategy.LookbackDays),
            _ => throw new NotSupportedException($"Strategy {strategy.Name} not supported.")
        };
    }

    // Calculation: Trend-following strategy
    private static TrendSignal DetectTrendFollowing(QuoteData quote, List<DailyData> history, int lookbackDays)
    {
        var recentHigh = history.TakeLast(lookbackDays).Max(d => d.High);
        var recentLow = history.TakeLast(lookbackDays).Min(d => d.Low);
        return quote.Last > recentHigh ? TrendSignal.Buy :
               quote.Last < recentLow ? TrendSignal.Sell : TrendSignal.Hold;
    }

    // Calculation: Mean-reversion strategy
    private static TrendSignal DetectMeanReversion(QuoteData quote, List<DailyData> history, int smaPeriod)
    {
        var sma = history.TakeLast(smaPeriod).Average(d => d.Close);
        return quote.Last < sma ? TrendSignal.Buy :
               quote.Last > sma ? TrendSignal.Sell : TrendSignal.Hold;
    }

    // Calculation: Breakout strategy
    private static TrendSignal DetectBreakout(QuoteData quote, List<DailyData> history, int lookbackDays)
    {
        var recentHigh = history.TakeLast(lookbackDays).Max(d => d.High);
        var recentLow = history.TakeLast(lookbackDays).Min(d => d.Low);
        var avgVolume = history.TakeLast(lookbackDays).Average(d => d.Volume);
        var latestDay = history.Last();
        bool highVolume = latestDay.Volume > avgVolume * 1.5;
        return highVolume && quote.Last > recentHigh ? TrendSignal.Buy :
               highVolume && quote.Last < recentLow ? TrendSignal.Sell : TrendSignal.Hold;
    }

    // Calculation: Compute order details
    private static Order CalculateOrder(QuoteData quote, double balance, TrendSignal signal, Config config)
    {
        if (signal == TrendSignal.Hold)
            return new Order(signal, 0, 0, 0);

        double entryPrice = quote.Last;
        double stopLossPrice = signal == TrendSignal.Buy
            ? entryPrice * (1 - config.StopLossPercent)
            : entryPrice * (1 + config.StopLossPercent);
        double riskAmount = balance * config.RiskPerTradePercent;
        int quantity = (int)(riskAmount / Math.Abs(entryPrice - stopLossPrice));

        // Constraint: Max position size
        double maxPositionValue = balance * config.MaxPositionPercent;
        if (quantity * entryPrice > maxPositionValue)
            quantity = (int)(maxPositionValue / entryPrice);

        return new Order(signal, quantity, entryPrice, stopLossPrice);
    }
}