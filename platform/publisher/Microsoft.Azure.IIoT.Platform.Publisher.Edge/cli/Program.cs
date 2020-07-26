// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Platform.Publisher.Edge.Cli {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher module host process
    /// </summary>
    public class Program {

        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main(string[] args) {

            var inputRate = 30000; // x values total per second
            var notifications = 1; // over x messages
            var outputRate = 100; // x network messages per second
            var mode = MessagingMode.Samples;
            var encoding = MessageEncoding.Json;
            var batchInterval = TimeSpan.FromMilliseconds(500);
            var batchSize = 1000;

            Console.WriteLine("Publisher test command line interface.");
            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new ArgumentException("Help");
                        case "-i":
                        case "--input-rate":
                            i++;
                            if (i < args.Length) {
                                if (!int.TryParse(args[i], out inputRate)) {
                                    i--;
                                }
                            }
                            break;
                        case "-o":
                        case "--output-rate":
                            i++;
                            if (i < args.Length) {
                                if (!int.TryParse(args[i], out outputRate)) {
                                    i--;
                                }
                            }
                            break;
                        case "-b":
                        case "--batch-size":
                            i++;
                            if (i < args.Length) {
                                if (!int.TryParse(args[i], out batchSize)) {
                                    i--;
                                }
                            }
                            break;
                        case "-t":
                        case "--batch-timeout":
                            i++;
                            if (i < args.Length) {
                                if (!TimeSpan.TryParse(args[i], out batchInterval)) {
                                    i--;
                                }
                            }
                            break;
                        case "-e":
                        case "--encoding":
                            i++;
                            if (i < args.Length) {
                                if (!Enum.TryParse(args[i], out encoding)) {
                                    i--;
                                }
                            }
                            break;
                        case "-m":
                        case "--mode":
                            i++;
                            if (i < args.Length) {
                                if (!Enum.TryParse(args[i], out mode)) {
                                    i--;
                                }
                            }
                            break;
                        default:
                            throw new ArgumentException($"Unknown argument {args[i]}.");
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Usage:       Microsoft.Azure.IIoT.Platform.Publisher.Edge.Cli [options]

Options:

    --batch-timeout
     -t      Batch timeout
    --batch-size
     -b      Batch size
    --input-rate
     -i      Input rate per second
    --output-rate
     -o      Output rate per second
    --encoding
     -e      Encoding (Json/Uadp)
    --mode
     -m      Messaging mode (PubSub/Samples)

    --help
     -?
     -h      Prints out this help.
"
                    );
                return;
            }

            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                logger.Fatal(e.ExceptionObject as Exception, "Exception");
                Console.WriteLine(e);
            };
            try {
                RunBenchmarkAsync(inputRate, notifications, batchSize, batchInterval, mode, encoding, outputRate, logger).Wait();
            }
            catch (Exception e) {
                logger.Error(e, "Exception");
            }
        }

        /// <summary>
        /// Run benchmark
        /// </summary>
        /// <returns></returns>
        private static async Task RunBenchmarkAsync(int rate, int notifications, int batch, TimeSpan interval,
            MessagingMode mode, MessageEncoding encoding, int delay, ILogger logger) {
            using (var io = new Mock(rate, notifications, batch, interval, mode, encoding, delay, logger))
            using (var process = new DataFlowProcessingEngine(io, CreateEncoder(mode, encoding),
                io, io, logger, io)) {
                await process.RunAsync(Agent.Framework.Models.ProcessMode.Active, CancellationToken.None);
            }
        }

        /// <summary>
        /// Encoder creation
        /// </summary>
        /// <returns></returns>
        private static IMessageEncoder CreateEncoder(MessagingMode mode, MessageEncoding encoding) {
            return mode == MessagingMode.PubSub ? new NetworkMessageEncoder() :
                (IMessageEncoder)new MonitoredItemMessageEncoder();
        }

        /// <summary>
        /// Mock input and output
        /// </summary>
        public class Mock : IMessageTrigger, IMessageSink, IEngineConfiguration, IIdentity {

            public string Id { get; } = "Test";

            public int NumberOfConnectionRetries { get; }
            public long ValueChangesCount => _valueChangesCount;
            public long DataChangesCount => _dataChangesCount;


            public int? BatchSize => _batchSize;
            public TimeSpan? BatchTriggerInterval => _interval;

            public int? MaxMessageSize { get; }
            public TimeSpan? DiagnosticsInterval { get; }

            public long SentMessagesCount => _sentMessagesCount;
            public string DeviceId { get; } = "Device";
            public string ModuleId { get; } = "Module";
            public string SiteId { get; } = "Site";
            public string Gateway { get; } = "Gateway";

            public Mock(int inputRate, int notifications, int batchSize, TimeSpan interval,
                MessagingMode mode, MessageEncoding encoding, int outputRate, ILogger logger) {
                _inputRate = inputRate == 0 ? 1 : inputRate;
                _notifications = notifications == 0 || notifications > _inputRate ? 1 : notifications;
                _batchSize = batchSize;
                _interval = interval;
                _mode = mode;
                _encoding = encoding;
                _outputRate = outputRate == 0 ? 1 : outputRate;
                _logger = logger;
                DiagnosticsInterval = TimeSpan.FromSeconds(10);
            }

            public event EventHandler<DataSetMessageModel> OnMessage;

            public void Dispose() {
            }

            public Task RunAsync(CancellationToken ct) {
                return Task.Run(async () => {
                    _logger.Information("Starting producer");
                    var sw = new Stopwatch();
                    var cls = Guid.NewGuid();
                    var ctx = new ServiceMessageContext();
                    var w = new DataSetWriterModel {
                        DataSetWriterId = "WriterId",
                        DataSet = new PublishedDataSetModel {
                            Name = null,
                            DataSetMetaData = new DataSetMetaDataModel {
                                ConfigurationVersion = new ConfigurationVersionModel {
                                    MajorVersion = 1,
                                    MinorVersion = 0
                                },
                                DataSetClassId = cls,
                                Name = "Writer"
                            },
                            ExtensionFields = new Dictionary<string, string> {
                                ["EndpointId"] = "Endpoint",
                                ["PublisherId"] = "Publisher",
                                ["DataSetWriterId"] = "WriterId"
                            }
                        },
                        DataSetFieldContentMask =
                                    OpcUa.Publisher.Models.DataSetFieldContentMask.StatusCode |
                                    OpcUa.Publisher.Models.DataSetFieldContentMask.SourceTimestamp |
                                    OpcUa.Publisher.Models.DataSetFieldContentMask.ServerTimestamp |
                                    OpcUa.Publisher.Models.DataSetFieldContentMask.NodeId |
                                    OpcUa.Publisher.Models.DataSetFieldContentMask.DisplayName |
                                    OpcUa.Publisher.Models.DataSetFieldContentMask.ApplicationUri |
                                    OpcUa.Publisher.Models.DataSetFieldContentMask.EndpointUrl |
                                    OpcUa.Publisher.Models.DataSetFieldContentMask.ExtensionFields,
                        MessageSettings = new DataSetWriterMessageSettingsModel() {
                            DataSetMessageContentMask =
                                        DataSetContentMask.Timestamp |
                                        DataSetContentMask.MetaDataVersion |
                                        DataSetContentMask.Status |
                                        DataSetContentMask.DataSetWriterId |
                                        DataSetContentMask.MajorVersion |
                                        DataSetContentMask.MinorVersion |
                                        DataSetContentMask.SequenceNumber
                        },
                    };
                    var g = new WriterGroupModel {
                        MessageType = _encoding,
                        Name = "WriterGroupName",
                        DataSetWriters = new List<DataSetWriterModel> { w},
                        MessageSettings = new WriterGroupMessageSettingsModel() {
                            NetworkMessageContentMask =
                                NetworkMessageContentMask.PublisherId |
                                NetworkMessageContentMask.WriterGroupId |
                                NetworkMessageContentMask.NetworkMessageNumber |
                                NetworkMessageContentMask.SequenceNumber |
                                NetworkMessageContentMask.PayloadHeader |
                                NetworkMessageContentMask.Timestamp |
                                NetworkMessageContentMask.DataSetClassId |
                                NetworkMessageContentMask.NetworkMessageHeader |
                                NetworkMessageContentMask.DataSetMessageHeader
                        },
                        WriterGroupId = "WriterGroup",
                    };

                    // Produce
                    for (var i = 0u; !ct.IsCancellationRequested; ) {
                        sw.Restart();

                        var messages = new List<DataSetMessageModel>();
                        var itemsPerNotification = _inputRate / _notifications;
                        var lastNotification = _inputRate % _notifications;
                        var notifications = lastNotification == 0 ? _notifications : _notifications + 1;
                        if (notifications == _notifications) {
                            lastNotification = itemsPerNotification;
                        }
                        var x = 0;
                        for (var j = 0; j < notifications; j++) {
                            var values = j == notifications - 1 ? lastNotification : itemsPerNotification;
                            var message = new DataSetMessageModel {
                                ApplicationUri = "Application",
                                EndpointUrl = "Endpoint",
                                PublisherId = "Publisher",
                                SequenceNumber = ++i,
                                ServiceMessageContext = ctx,
                                SubscriptionId = "Subscription",
                                Writer = w,
                                WriterGroup = g,
                                Notifications = LinqEx.Repeat(() => new MonitoredItemNotificationModel {
                                    DisplayName = "Value" + ++x,
                                    ClientHandle = (uint)x,
                                    Id = x.ToString(),
                                    NodeId = "i=" + x,
                                    SequenceNumber = i,
                                    Value = new DataValue(new Variant(i), StatusCodes.Good,
                                        DateTime.UtcNow - TimeSpan.FromSeconds(1), DateTime.UtcNow),
                                    PublishTime = DateTime.UtcNow,
                                }, values)
                            };
                            Interlocked.Increment(ref _dataChangesCount);
                            Interlocked.Add(ref _valueChangesCount, values);
                            OnMessage?.Invoke(this, message);
                        }
                        var elapsed = sw.ElapsedMilliseconds;
                        if (elapsed < 1000) {
                            await Task.Delay(1000 - (int)elapsed, ct);
                        }
                        else {
                            Console.WriteLine("Drifting");
                        }
                    }
                    _logger.Information("Ending producer");
                });
            }

            public async Task SendAsync(IEnumerable<NetworkMessageModel> messages) {
                var numberOfMessages = messages.Count();
                if (_outputRate < 1000) {
                    await Task.Delay(1000 / _outputRate / numberOfMessages); // Sending rate
                }
                Interlocked.Add(ref _sentMessagesCount, numberOfMessages);
            }


            private readonly int _inputRate;
            private readonly int _notifications;
            private readonly int _outputRate;
            private readonly ILogger _logger;
            private long _valueChangesCount;
            private long _dataChangesCount;
            private long _sentMessagesCount;

            private readonly int _batchSize;
            private readonly TimeSpan _interval;
            private readonly MessagingMode _mode;
            private readonly MessageEncoding _encoding;
        }
    }
}
