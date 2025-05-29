using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using TradingBot;
using TradingBot.Platforms;
using TradingBot.Constraints;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Define command-line options
        var platformOption = new Option<string>(
            name: "--platform",
            description: "Trading platform (Tradier, Schwab, Mock)")
        {
            IsRequired = true
        };
        platformOption.AddValidator(result =>
        {
            var value = result.GetValueOrDefault<string>();
            if (!new[] { "Tradier", "Schwab", "Mock" }.Contains(value))
                result.ErrorMessage = "Platform must be Tradier, Schwab, or Mock.";
        });

        var tokenOption = new Option<string>(
            name: "--token",
            description: "API access token (or set {platform}_TOKEN env variable)")
        {
            IsRequired = true
        };

        var strategyOption = new Option<string>(
            name: "--strategy",
            description: "Trading strategy (TrendFollowing, MeanReversion, Breakout)")
        {
            IsRequired = true
        };
        strategyOption.AddValidator(result =>
        {
            var value = result.GetValueOrDefault<string>();
            if (!new[] { "TrendFollowing", "MeanReversion", "Breakout" }.Contains(value))
                result.ErrorMessage = "Strategy must be TrendFollowing, MeanReversion, or Breakout.";
        });

        var constraintOption = new Option<string>(
            name: "--constraint",
            description: "Constraint set (Default)")
        {
            IsRequired = true
        };
        constraintOption.AddValidator(result =>
        {
            var value = result.GetValueOrDefault<string>();
            if (!new[] { "Default", "AlwaysTrade" }.Contains(value))
                result.ErrorMessage = "Constraint must be Default or AlwaysTrade.";
        });

        // Define root command
        var rootCommand = new RootCommand("Trading system with simplified command-line configuration")
        {
            platformOption,
            tokenOption,
            strategyOption,
            constraintOption
        };

        // Handle command
        rootCommand.SetHandler((Func<InvocationContext,Task>)(async (context) =>
        {
            var platform = context.ParseResult.GetValueForOption(platformOption)!;
            var token = context.ParseResult.GetValueForOption(tokenOption) ?? Environment.GetEnvironmentVariable($"{platform.ToUpper()}_TOKEN");
            var strategy = context.ParseResult.GetValueForOption(strategyOption)!;
            var constraintSet = context.ParseResult.GetValueForOption(constraintOption)!;

            if (string.IsNullOrEmpty(token))
            {
                Console.Error.WriteLine($"Error: Token not provided and {platform.ToUpper()}_TOKEN not set.");
                context.ExitCode = 1;
                return;
            }

            // Hardcode account ID based on platform
            var accountId = platform switch
            {
                "Tradier" => "tradier-account",
                "Schwab" => "schwab-account",
                "Mock" => "mock-account",
                _ => throw new NotSupportedException($"Platform {platform} not supported.")
            };

            var config = new Config(
                Platform: platform,
                Token: token,
                AccountId: accountId,
                Symbol: "AAPL", // Hardcoded
                RiskPerTradePercent: 0.01, // 1%
                StopLossPercent: 0.02, // 2%
                MaxPositionPercent: 0.10, // 10%
                Strategy: new StrategyConfig(strategy, LookbackDays: 5, SmaPeriod: 5),
                ConstraintSet: constraintSet
            );

            IPlatform platformInstance = config.Platform switch
            {
                "Tradier" => new TradierPlatform(),
                "Schwab" => new SchwabPlatform(),
                "Mock" => new MockPlatform(),
                _ => throw new NotSupportedException($"Platform {config.Platform} not supported.")
            };

            IConstraint constraint = config.ConstraintSet switch
            {
                "Default" => new DefaultConstraint(),
                "AlwaysTrade" => new AlwaysTradeConstraint(),
                _ => throw new NotSupportedException($"Constraint set {config.ConstraintSet} not supported.")
            };

            await TradingProgram.Run(config, platformInstance, constraint);
        }));

        // Parse and execute
        return await rootCommand.InvokeAsync(args);
    }
}