namespace TradingBot.Constraints;

public class AlwaysTradeConstraint : IConstraint
{

    public Result CanTrade()
    {
        return new Result(true, "Trade allowed.");
    }

}