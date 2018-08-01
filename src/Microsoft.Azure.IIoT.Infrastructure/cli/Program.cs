// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Cli {
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Infrastructure.Compute;
    using Microsoft.Azure.IIoT.Infrastructure.Compute.Services;
    using Microsoft.Azure.IIoT.Infrastructure.Hub;
    using Microsoft.Azure.IIoT.Infrastructure.Hub.Services;
    using Microsoft.Azure.IIoT.Infrastructure.Runtime;
    using Microsoft.Azure.IIoT.Infrastructure.Services;
    using Microsoft.Azure.IIoT.Infrastructure;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Newtonsoft.Json;
    using Autofac;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Api command line interface
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            if (args == null) {
                throw new ArgumentNullException(nameof(args));
            }

            // Set up dependency injection for the module host
            var container = ConfigureContainer();

            using (var scope = container.BeginLifetimeScope()) {
                RunAsync(args, scope).Wait();
            }
        }

        /// <summary>
        /// Run client
        /// </summary>
        /// <param name="args">command-line arguments</param>
        public static async Task RunAsync(string[] args, IComponentContext context) {
            var interactive = true;
            do {
                if (interactive) {
                    Console.Write("> ");
                    args = Console.ReadLine().ParseAsCommandLine();
                }
                try {
                    if (args.Length < 1) {
                        throw new ArgumentException("Need a command!");
                    }
                    var command = args[0].ToLowerInvariant();
                    var options = CollectOptions(1, args);
                    switch (command) {
                        case "exit":
                            return;
                        case "iothub":
                            await TestIoTHubCreateDeleteCreate(context, options);
                            break;
                        case "vm":
                            await TestVmCreateDeleteCreate(context, options);
                            break;
                        case "-?":
                        case "-h":
                        case "--help":
                        case "help":
                            PrintHelp();
                            break;
                        default:
                            throw new ArgumentException($"Unknown command {command}.");
                    }
                }
                catch (ArgumentException e) {
                    Console.WriteLine(e.Message);
                    PrintHelp();
                }
                catch (Exception e) {
                    Console.WriteLine("==================");
                    Console.WriteLine(e);
                    Console.WriteLine("==================");
                }
            }
            while (interactive);
        }


        /// <summary>
        /// Create and delete resource grouop
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
            private static async Task TestIoTHubCreateDeleteCreate(IComponentContext context,
            Dictionary<string, string> options) {

            var manager = context.Resolve<IResourceGroupFactory>();
            var name = GetOption(options, "-n", "--name", StringEx.CreateUnique(9, "test"));
            Console.WriteLine("Creating resource group....");
            using (var resourceGroup = await manager.CreateAsync(true)) {
                Console.WriteLine("Resource group created.");

                var hubs = context.Resolve<IIoTHubFactory>();
                Console.WriteLine("Creating iothub...");
                var hub = await hubs.CreateAsync(resourceGroup, name);
                Console.WriteLine("iothub created.");

                Console.WriteLine("Deleting iothub...");
                await hub.DeleteAsync();
                Console.WriteLine("iothub deleted.");

                Console.WriteLine("Recreating iothub...");
                hub = await hubs.CreateAsync(resourceGroup, name);
                Console.WriteLine("iothub created.");
            }
            Console.WriteLine("Resource group deleted.");
        }

        /// <summary>
        /// Create and delete resource grouop
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task TestVmCreateDeleteCreate(IComponentContext context,
            Dictionary<string, string> options) {

            var manager = context.Resolve<IResourceGroupFactory>();
            var name = GetOption(options, "-n", "--name", StringEx.CreateUnique(9, "test"));
            Console.WriteLine("Creating resource group....");
            using (var resourceGroup = await manager.CreateAsync(true)) {
                Console.WriteLine("Resource group created.");
                var vms = context.Resolve<IVirtualMachineFactory>();
                Console.WriteLine("Creating virtual machine...");
                var vm = await vms.CreateAsync(resourceGroup, name);
                Console.WriteLine("Virtual machine created.");

                Console.WriteLine("Deleting virtual machine...");
                await vm.DeleteAsync();
                Console.WriteLine("Virtual machine deleted.");

                Console.WriteLine("Recreating virtual machine...");
                vm = await vms.CreateAsync(resourceGroup, name);
                Console.WriteLine("Virtual machine created.");
            }
            Console.WriteLine("Resource group deleted.");
        }


        /// <summary>
        /// Print result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <param name="status"></param>
        private static void PrintResult<T>(Dictionary<string, string> options,
            T status) {
            Console.WriteLine("==================");
            Console.WriteLine(JsonConvert.SerializeObject(status,
                GetOption(options, "-F", "--format", Formatting.Indented)));
            Console.WriteLine("==================");
        }

        /// <summary>
        /// Get option value
        /// </summary>
        /// <param name="options"></param>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static T GetOption<T>(Dictionary<string, string> options,
            string key1, string key2, T defaultValue) {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                return defaultValue;
            }
            return value.As<T>();
        }

        /// <summary>
        /// Get mandatory option value
        /// </summary>
        /// <param name="options"></param>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        private static T GetOption<T>(Dictionary<string, string> options,
            string key1, string key2) {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                throw new ArgumentException($"Missing {key1}/{key2} option.");
            }
            return value.As<T>();
        }

        /// <summary>
        /// Get mandatory option value
        /// </summary>
        /// <param name="options"></param>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        private static T? GetOption<T>(Dictionary<string, string> options,
            string key1, string key2, T? defaultValue) where T : struct {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                return defaultValue;
            }
            if (typeof(T).IsEnum) {
                return Enum.Parse<T>(value, true);
            }
            return value.As<T>();
        }

        /// <summary>
        /// Helper to collect options
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Dictionary<string, string> CollectOptions(int offset,
            string[] args) {
            var options = new Dictionary<string, string>();
            for (var i = offset; i < args.Length;) {
                var key = args[i];
                if (key[0] != '-') {
                    throw new ArgumentException($"{key} is not an option.");
                }
                i++;
                if (i == args.Length) {
                    options.Add(key, "true");
                    break;
                }
                var val = args[i];
                if (val[0] == '-') {
                    // An option, so previous one is a boolean option
                    options.Add(key, "true");
                    continue;
                }
                options.Add(key, val);
                i++;
            }
            return options;
        }


        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        /// <returns></returns>
        private static IContainer ConfigureContainer() {

            var builder = new ContainerBuilder();

            // Register logger
            builder.RegisterType<SimpleLogger>()
                .AsImplementedInterfaces().SingleInstance();

            // Register infrastructure code
            builder.RegisterType<ResourceGroupFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AzureSubscription>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ConsoleSelector>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<VisualStudioCredentials>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<IoTHubFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VirtualMachineFactory>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintHelp() {
            Console.WriteLine(
                @"
Management cli - Tests management api
usage:      Management test [options]

Tests

     iothub      Test iothub create and delete
     vm          Test vm create and delete

     help, -h, -? --help
                 Prints out this help.
"
                );
        }
    }
}
