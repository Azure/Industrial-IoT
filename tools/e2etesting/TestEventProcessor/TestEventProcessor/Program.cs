// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor
{
    using Mono.Options;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BusinessLogic;
    using Microsoft.Extensions.Logging;
    using Serilog.Extensions.Logging;

    class Program
    {
        static async Task Main(string[] args)
        {
            string ioTHubEventHubEndpointConnectionString = null;
            string storageConnectionString = null;
            string blobContainerName = "checkpoint";
            string eventHubConsumerGroup = "$Default";
            bool showHelp = false;
            uint expectedValueChangesPerTimestamp = 0;
            uint expectedIntervalOfValueChanges = 0;
            uint expectedMaximalTotalDuration = 0;

            var options = new OptionSet
            {
                {"c|connectionString=", "The connection string of the IoT Hub Device/Module that receives telemetry", s => ioTHubEventHubEndpointConnectionString = s },
                {"sc|storageConnectionString=", "The connection string of the storage account to store checkpoints.", s => storageConnectionString = s },
                {"ee|expectedEvents=", "The amount of value changes per SourceTimestamp that is expected", (uint i) => expectedValueChangesPerTimestamp = i},
                {"ei|expectedInterval=", "The time in milliseconds between value changes that is expected", (uint i) => expectedIntervalOfValueChanges = i},
                {"ed|expectedDuration=", "The maximal time in milliseconds between OPC UA value change and enqueue in event hub partition that is expected", (uint i) => expectedMaximalTotalDuration = i},
                {"h|help",  "show this message and exit", b => showHelp = b != null }
            };

            options.Parse(args);

            if (showHelp)
            {
                ShowHelp(options);
                return;
            }

            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}][{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

            var configuration = new ValidatorConfiguration() {
                BlobContainerName = blobContainerName,
                EventHubConsumerGroup = eventHubConsumerGroup,
                ExpectedIntervalOfValueChanges = expectedIntervalOfValueChanges,
                ExpectedMaximalDuration = expectedMaximalTotalDuration,
                ExpectedValueChangesPerTimestamp = expectedValueChangesPerTimestamp,
                IoTHubEventHubEndpointConnectionString = ioTHubEventHubEndpointConnectionString,
                StorageConnectionString = storageConnectionString,
            };


            var loggerFactory = new SerilogLoggerFactory(Log.Logger);
            var melLogger = loggerFactory.CreateLogger<TelemetryValidator>();

            var validator = new TelemetryValidator(melLogger);
            await validator.StartAsync(configuration);

            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, cancelArgs) =>
            {
                if (cancelArgs.SpecialKey == ConsoleSpecialKey.ControlC)
                {
                    Log.Information("Cancellation requested by hitting ctrl+c");
                    validator.StopAsync().Wait();
                }
            };

            Log.Information("TestEventProcessor stopped");
        }

        /// <summary>
        /// Print the command line options
        /// </summary>
        /// <param name="optionSet">configured Options</param>
        private static void ShowHelp(OptionSet optionSet)
        {
            if (optionSet == null)
            {
                throw new ArgumentNullException(nameof(optionSet));
            }

            Console.WriteLine("Usage: TestEventProcessor");
            Console.WriteLine();
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
        }
    }
}
