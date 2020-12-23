// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Standalone {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Standalone.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Agent;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Triggering;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Testing;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Serilog;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class Program {
        private static void Main(string[] args) {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args) {
            var log = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger = log;

            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile($"appsettings.{environmentName}.json", true)
                .AddEnvironmentVariables()
                .AddFromDotEnvFile()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .Build();

            var opcServerEndpointUrl = config.GetValue<string>("OPCUAServerEndpointUrl");

            if (opcServerEndpointUrl.Equals("sampleserver", StringComparison.OrdinalIgnoreCase)) {
                var tss = new TestServerFixture();
                opcServerEndpointUrl = $"opc.tcp://{Environment.MachineName}:{tss.Port}/UA/SampleServer";
            }

            var messageSinkConnection = config.GetValue<string>("MessageSinkConnection");

            var pubSubJobConfig = SampleData.GetDataSetWriterDeviceJobModel(opcServerEndpointUrl, messageSinkConnection);

            var jobJson = JsonConvert.SerializeObject(pubSubJobConfig);
            File.WriteAllText(@"D:\Temp\job.json", jobJson);

            var cb = new ContainerBuilder();

            cb.RegisterInstance(config).As<IConfiguration>();

            //switch (pubSubJobConfig.MessageSinkConfiguration.Type) {
            //    case MessageSinkConfigurationTypes.ConnectionString:
            //        throw new NotImplementedException();
            //    // TODO currently in rework
            //    //cb.RegisterType<IoTHubMessageSink>().As<IMessageSink>().InstancePerDependency();
            //    //break;
            //    case MessageSinkConfigurationTypes.Directory:
            //        cb.RegisterType<FileSystemMessageSink>().AsImplementedInterfaces().InstancePerDependency();
            //        break;
            //    default: throw new NotImplementedException($"Unknown message sink type: {pubSubJobConfig.MessageSinkConfiguration.Type}");
            //}

            cb.RegisterInstance(pubSubJobConfig.Job);
            cb.RegisterInstance(pubSubJobConfig);
         //  TODO  cb.RegisterInstance(pubSubJobConfig.MessageTriggerConfig.OpcConfig);
            cb.RegisterInstance(pubSubJobConfig.Job.Configuration ?? new Models.PublisherJobConfigModel());
            cb.RegisterInstance(new ModuleConfig().Clone(pubSubJobConfig.ConnectionString));

            cb.RegisterType<DefaultSessionManager>().SingleInstance().AsImplementedInterfaces();
            cb.RegisterType<DefaultSubscriptionManager>().SingleInstance().AsImplementedInterfaces();
            //cb.RegisterType<PubSubJsonMessageEncoder>().SingleInstance().Named<IMessageEncoder>(EncodingConfiguration.ContentTypes.PubSubJson);
            cb.RegisterType<DataFlowProcessingEngine>().As<IProcessingEngine>().InstancePerDependency();
            cb.RegisterType<WorkerSupervisor>().AsImplementedInterfaces().SingleInstance();

            cb.RegisterType<PubSubMessageTrigger>().As<IMessageTrigger>().InstancePerDependency();
            cb.RegisterType<JsonEncoder>().AsImplementedInterfaces().SingleInstance();

            var container = cb.Build();

            var messageEncoder = container.ResolveNamed<IMessageEncoder>(pubSubJobConfig.Job.ToEncodingConfig().ContentType);

            using (var engineScope = container.BeginLifetimeScope()) {
                var engine = engineScope.Resolve<IProcessingEngine>(new NamedParameter("messageEncoder", messageEncoder));

                var cts = new CancellationTokenSource();
                await engine.RunAsync(cts.Token, ProcessMode.Active);
            }
        }
    }
}