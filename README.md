TradingBot
==========

C# Program to trade on the market using following logic
1. Detect Signal
2. Trade only when Constraint is met

There are 3 strategies and 2 constraints.

Strategies
----------
1. Trend Following
2. Mean Reversion
3. Breakout

Constraints
-----------
1. Default (Trade only when Market is open)
2. Always Trade (Trade always)

How to run
==========
1. Install dotnet 9.0
2. cd in TradingBot\TradingBot
3. dotnet run --platform Mock --token 123 --strategy TrendFollowing --constraint AlwaysTrade

Note
====
1. I do not have developer account to test actual Api.
Requesting it will takes long time. So I did create Mock service just for testing.
It always detect the signal for all strategies.

2. I would like to show how every component works together.
For example, platform, strategies and constraints are all seperated by interface.
which means that we can switch between different platform, strategies and constraints.

3. Thank you if you read through this.