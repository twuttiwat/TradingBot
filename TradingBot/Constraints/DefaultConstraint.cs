namespace TradingBot.Constraints;

public class DefaultConstraint : IConstraint
{
    private static DateTime? LastTradeTime;

    public Result CanTrade()
    {
        var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
        var marketOpen = new TimeSpan(9, 30, 0);
        var marketClose = new TimeSpan(16, 0, 0);

        if (now.DayOfWeek < DayOfWeek.Monday || now.DayOfWeek > DayOfWeek.Friday
                                             || now.TimeOfDay < marketOpen || now.TimeOfDay > marketClose)
            return new Result(false, "Market is closed. Trading not allowed.");

        if (LastTradeTime.HasValue && LastTradeTime.Value.Date == DateTime.Today)
            return new Result(false, "Trade limit reached for today.");

        return new Result(true, "Trade allowed.");
    }

    public static void RecordTrade() => LastTradeTime = DateTime.Now;
}