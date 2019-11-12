// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Cli {
    using Microsoft.Azure.IIoT.Api.Jobs;
    using Microsoft.Azure.IIoT.Api.Jobs.Clients;
    using Microsoft.Azure.IIoT.Api.Jobs.Models;
    using Microsoft.Azure.IIoT.Agent.Framework.Serializer;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Http.Default;

    /// <summary>
    /// Job service clid
    /// </summary>
    internal static class Program {
        private static IContainer _container;

        private static void Main(string[] args) {
            Console.WriteLine(@"      _       _        _____                 _             _____                      _      ");
            Console.WriteLine(@"     | |     | |      / ____|               (_)           / ____|                    | |     ");
            Console.WriteLine(@"     | | ___ | |__   | (___   ___ _ ____   ___  ___ ___  | |     ___  _ __  ___  ___ | | ___ ");
            Console.WriteLine(@" _   | |/ _ \| '_ \   \___ \ / _ \ '__\ \ / / |/ __/ _ \ | |    / _ \| '_ \/ __|/ _ \| |/ _ \");
            Console.WriteLine(@"| |__| | (_) | |_) |  ____) |  __/ |   \ V /| | (_|  __/ | |___| (_) | | | \__ \ (_) | |  __/");
            Console.WriteLine(@" \____/ \___/|_.__/  |_____/ \___|_|    \_/ |_|\___\___|  \_____\___/|_| |_|___/\___/|_|\___|");
            Console.WriteLine();
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args) {

            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                .AddEnvironmentVariables()
                .AddFromDotEnvFile()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .Build();

            var containerBuilder = new ContainerBuilder();
            var jobManagerServiceConfiguration = config.GetSection(nameof(JobsServiceConfig)).Get<JobsServiceConfig>();

            containerBuilder.RegisterInstance(jobManagerServiceConfiguration ?? new JobsServiceConfig());
            containerBuilder.AddConsoleLogger();
            containerBuilder.RegisterType<JobsServiceClient>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<DefaultJobSerializer>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterModule<HttpClientModule>();
            containerBuilder.RegisterInstance(config).As<IConfiguration>();

            var manualKnownJobTypesProvider =
                new KnownJobConfigProvider(new[] { typeof(MonitoredItemDeviceJobModel), typeof(PubSubJobModel) });
            containerBuilder.RegisterInstance(manualKnownJobTypesProvider).AsImplementedInterfaces();

            _container = containerBuilder.Build();

            await ShowMainMenuAsync();
        }

        private static async Task ShowMainMenuAsync() {
            while (true) {
                Console.WriteLine("Main menu");
                Console.WriteLine("=========");
                Console.WriteLine();
                Console.WriteLine("   1 - List jobs");
                Console.WriteLine("   2 - Delete job");
                Console.WriteLine("   3 - Cancel job");
                Console.WriteLine("   4 - Restart job");
                Console.WriteLine("   5 - Export job documents to filesystem");
                Console.WriteLine();
                Console.WriteLine("   9 - Exit");
                Console.WriteLine();
                Console.Write("Enter your selection: ");
                var inputString = Console.ReadLine();

                if (!int.TryParse(inputString, out var input)) {
                    Console.WriteLine("Invalid input.");
                    continue;
                }

                Console.WriteLine();

                switch (input) {
                    case 1:
                        await ListJobsAsync();
                        break;
                    case 2:
                        await DeleteJobAsync();
                        break;
                    case 3:
                        await CancelJobAsync();
                        break;
                    case 4:
                        await RestartJobAsync();
                        break;
                    case 5:
                        await ExportJobDocumentsAsync();
                        break;
                    case 9:
                        return;
                    default:
                        Console.WriteLine("Invalid input.");
                        break;
                }
            }
        }

        private static async Task ExportJobDocumentsAsync() {
            var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Jobs");

            Console.Write($"Enter path to save job documents (leave empty for default '{defaultPath}'): ");
            var path = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(path)) {
                path = defaultPath;
            }

            var jobManagerServiceClient = _container.Resolve<IJobsServiceApi>();

            var jobs = await jobManagerServiceClient.ListAllJobsAsync();

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            foreach (var job in jobs) {
                var json = JsonConvert.SerializeObject(job, Formatting.Indented);
                File.WriteAllText(Path.Combine(path, $"{job.Id}.json"), json);
            }

            Console.WriteLine($"Saved jobs to {path}.");
            Console.WriteLine();
        }

        private static async Task DeleteJobAsync() {
            var jobManagerServiceClient = _container.Resolve<IJobsServiceApi>();
            var jobs = await jobManagerServiceClient.ListAllJobsAsync();

            Console.WriteLine();
            Console.WriteLine("");
            var counter = 1;
            var jobDic = new Dictionary<int, JobInfoApiModel>();
            foreach (var job in jobs) {
                if (job.LifetimeData.Status == JobStatus.Deleted) {
                    continue;
                }

                jobDic[counter] = job;
                Console.WriteLine($"{counter++} - {job.Id}\t{job.LifetimeData.Status}");
            }

            int selection;

            do {
                Console.WriteLine();
                Console.Write("Select the job you want to delete: ");
            } while (!int.TryParse(Console.ReadLine(), out selection) || selection < 1 || selection > jobDic.Count);

            await jobManagerServiceClient.DeleteJobAsync(jobDic[selection].Id);
        }

        private static async Task CancelJobAsync() {
            var jobManagerServiceClient = _container.Resolve<IJobsServiceApi>();
            var jobs = await jobManagerServiceClient.ListAllJobsAsync();

            Console.WriteLine();
            Console.WriteLine("");
            var counter = 1;
            var jobDic = new Dictionary<int, JobInfoApiModel>();
            foreach (var job in jobs) {
                if (job.LifetimeData.Status == JobStatus.Deleted) {
                    continue;
                }

                jobDic[counter] = job;
                Console.WriteLine($"{counter++} - {job.Id}\t{job.LifetimeData.Status}");
            }

            int selection;

            do {
                Console.WriteLine();
                Console.Write("Select the job you want to cancel: ");
            } while (!int.TryParse(Console.ReadLine(), out selection) || selection < 1 || selection > jobDic.Count);

            await jobManagerServiceClient.CancelJobAsync(jobDic[selection].Id);
        }

        private static async Task RestartJobAsync() {
            var jobManagerServiceClient = _container.Resolve<IJobsServiceApi>();
            var jobs = await jobManagerServiceClient.ListAllJobsAsync();

            Console.WriteLine();
            Console.WriteLine("");
            var counter = 1;
            var jobDic = new Dictionary<int, JobInfoApiModel>();
            foreach (var j in jobs) {
                if (j.LifetimeData.Status == JobStatus.Deleted) {
                    continue;
                }

                jobDic[counter] = j;
                Console.WriteLine($"{counter++} - {j.Id}\t{j.LifetimeData.Status}");
            }

            int selection;

            do {
                Console.WriteLine();
                Console.Write("Select the job you want to restart: ");
            } while (!int.TryParse(Console.ReadLine(), out selection) || selection < 1 || selection > jobDic.Count);

            var job = jobDic[selection];

            if (job.LifetimeData.Status == JobStatus.Deleted) {
                Console.WriteLine("Cannot restart deleted jobs.");
                return;
            }
            await jobManagerServiceClient.RestartJobAsync(jobDic[selection].Id);
        }

        private static async Task ListJobsAsync() {
            var jobManagerServiceClient = _container.Resolve<IJobsServiceApi>();
            var jobs = await jobManagerServiceClient.ListAllJobsAsync();

            Console.WriteLine("Jobs:");

            foreach (var job in jobs) {
                Console.WriteLine($"{job.Id}\t{job.LifetimeData.Status}");

                foreach (var p in job.LifetimeData.ProcessingStatus) {
                    Console.WriteLine($"   {p.Value}\t{p.Value.ProcessMode}\t{p.Value.LastKnownHeartbeat}");
                }

                Console.WriteLine();
            }

            Console.WriteLine();
        }
    }
}