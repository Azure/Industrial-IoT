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
    using Microsoft.Azure.IIoT.Utils;
    using Autofac;
    using AutofacSerilogIntegration;
    using System;
    using System.Threading.Tasks;
    using Serilog;

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
                    args = CliOptions.ParseAsCommandLine(Console.ReadLine());
                }
                try {
                    if (args.Length < 1) {
                        throw new ArgumentException("Need a command!");
                    }
                    var command = args[0].ToLowerInvariant();
                    var options = new CliOptions(args);
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
        CliOptions options) {

            var manager = context.Resolve<IResourceGroupFactory>();
            var name = options.GetValueOrDefault("-n", "--name", StringEx.CreateUnique(9, "test"));
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
            CliOptions options) {

            var manager = context.Resolve<IResourceGroupFactory>();
            var name = options.GetValueOrDefault("-n", "--name", StringEx.CreateUnique(9, "test"));
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
        /// Configure Dependency injection
        /// </summary>
        /// <returns></returns>
        private static IContainer ConfigureContainer() {

            var builder = new ContainerBuilder();

            // Register logger
            builder.RegisterLogger(LogEx.ConsoleOut());

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
