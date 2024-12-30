using System.Data.Common;
using Microsoft.AspNetCore.SignalR.Client;
//using Serilog;

namespace Kurisu.Test.WebApi_A.AutoReload
{
    /// <summary>
    /// signalR configuration provider
    /// </summary>
    public class SignalRConfigurationProvider : ConfigurationProvider
    {
        private readonly SignalROptions _signalROptions;
        private readonly HubConnection _connection;
        private static IDictionary<string, string> _data = new Dictionary<string, string>();

        public SignalRConfigurationProvider(IServiceProvider serviceProvider, SignalROptions signalROptions)
        {
            _signalROptions = signalROptions;
            _connection = new HubConnectionBuilder()
                .WithUrl(_signalROptions.Url, (options) =>
                {
                    //options.AccessTokenProvider = () =>
                    //{
                    //    return null;
                    //};
                    //options.
                })
                .ConfigureLogging(builder =>
                {
                    //builder.AddSerilog();
                    builder.SetMinimumLevel(LogLevel.Debug);
                })
                .WithAutomaticReconnect(new RandomRetryPolicy())
                .Build();

            _connection.Reconnected += msg =>
            {
                return Task.CompletedTask;
            };

            _connection.Reconnecting += msg =>
            {
                return Task.CompletedTask;
            };

            _connection.Closed += msg =>
            {
                return Task.CompletedTask;
            };

            _connection.On<IDictionary<string, string>>("Reload", data =>
            {
                _data = data;
                Load();
            });

            _connection.StartAsync().Wait();
        }

        public override void Load()
        {
            Data = _data;
        }
    }
}



public class RandomRetryPolicy : IRetryPolicy
{
    private readonly Random _random = new Random();

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        // If we've been reconnecting for less than 60 seconds so far,
        // wait between 0 and 10 seconds before the next reconnect attempt.
        if (retryContext.ElapsedTime < TimeSpan.FromSeconds(60))
        {
            return TimeSpan.FromSeconds(_random.NextDouble() * 10);
        }
        else
        {
            // If we've been reconnecting for more than 60 seconds so far, stop reconnecting.
            return null;
        }
    }
}
