// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Cli {
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A slightly better console for queries
    /// </summary>
    public class Program {

        /// <summary>
        /// Console entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            var command = (args.Length > 0) ? args[0].ToLowerInvariant() : null;
            var options = new CliOptions(args, 2);
            try {
                switch (command) {
                    case "dump":
                        DumpCollectionAsync(options).Wait();
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

        /// <summary>
        /// Dump all documents using sql
        /// </summary>
        public static async Task DumpCollectionAsync(CliOptions options) {
            var collection = await GetDocsAsync(options);
            var queryable = collection.OpenSqlClient();
            var serializer = new NewtonSoftJsonSerializer();
            var feed = queryable.Query<dynamic>("select * from root");
            while (feed.HasMore()) {
                var result = await feed.ReadAsync();
                foreach (var item in result) {
                    Console.WriteLine(serializer.SerializePretty(item));
                }
            }
        }


        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintHelp() {
            Console.WriteLine(
                @"
aziiotstoragecli - Test command line interface
usage:      aziiotstoragecli command [options]

Commands and Options

     dump        Dump all documents in a collection as raw json.
     gremlin     Run a gremlin console against gremlin query interface.
     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Get database
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<IDatabase> GetDatabaseAsync(CliOptions options) {
            var logger = ConsoleLogger.Create();
            var config = new ConfigurationBuilder()
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .Build();
            var configuration = new CosmosDbConfig(config);
            var server = new CosmosDbServiceClient(configuration, logger);
            return await server.OpenAsync(
                options.GetValueOrDefault("-d", "--db", "default"), null);
        }

        /// <summary>
        /// Get collection interface
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<IDocuments> GetDocsAsync(CliOptions options) {
            var database = await GetDatabaseAsync(options);
            var coll = await database.OpenContainerAsync(
                options.GetValueOrDefault("-c", "--collection", "default"));
            return coll.AsDocuments();
        }

        /// <summary>
        /// Print header for console.
        /// </summary>
        private static void WriteHeader() {
            Console.Clear();
            Console.WriteLine("Gremlin console (exit|run|!|history|?)");
        }
    }
}
