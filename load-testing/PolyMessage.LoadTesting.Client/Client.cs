﻿using System;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PolyMessage.LoadTesting.Client
{
    public static class Client
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ClientOptions>(args)
                .WithParsed(options => Start(options));
        }

        private static void Start(ClientOptions options)
        {
            IServiceProvider serviceProvider = BuildServiceProvider(options.LogLevel);
            ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Client));
            ClientRunner runner = new ClientRunner(logger, serviceProvider);
            runner.Run(options);
        }

        private static IServiceProvider BuildServiceProvider(LogLevel logLevel)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(logLevel);
                loggingBuilder.AddDebug();
                loggingBuilder.AddConsole();
            });

            return services.BuildServiceProvider();
        }
    }
}
