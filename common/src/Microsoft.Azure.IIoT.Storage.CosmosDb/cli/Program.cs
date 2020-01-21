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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Diagnostics;

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
                    case "gremlin":
                        RunGremlinConsoleAsync(options).Wait();
                        break;
                    case "gremlin-file":
                        RunGremlinFileAsync(options).Wait();
                        break;
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
            var feed = queryable.Query<dynamic>("select * from root");
            while (feed.HasMore()) {
                var result = await feed.ReadAsync();
                foreach (var item in result) {
                    Console.WriteLine(JsonConvertEx.SerializeObjectPretty(item));
                }
            }
        }

        /// <summary>
        /// Run gremlin console
        /// </summary>
        public static async Task RunGremlinConsoleAsync(CliOptions options) {
            LinkedList<List<string>> history = null;

            var graph = await GetGraphAsync(options);
            try {
                var text = File.ReadAllText("_history.tmp");
                history = JArray.Parse(text).ToObject<LinkedList<List<string>>>();
            }
            catch {
                history = new LinkedList<List<string>>();
            }
            var batch = new List<string>();

            using (var gremlinClient = graph.OpenGremlinClient()) {
                WriteHeader();
                while (true) {
                    Console.Write(":> ");
                    if (batch.Any()) {
                        Console.Write("   ");
                    }
                    var command = Console.ReadLine();
                    if (string.IsNullOrEmpty(command)) {
                        continue;
                    }
                    if (command == "run" || command == ";") {
                        if (!batch.Any()) {
                            Console.WriteLine("Nothing to do!");
                            continue;
                        }
                        // Execute
                        var gremlin = string.Concat(batch.Select(s => s.Trim()));
                        var sw = new Stopwatch();
                        sw.Start();
                        var feed = gremlinClient.Submit<dynamic>(gremlin);
                        try {
                            var results = await feed.AllAsync();
                            var array = results.ToArray();
                            for (var i = 0; i < array.Length; i++) {
                                Console.WriteLine($"[{i + 1}]\t{array[i]}");
                                if (0 == ((i + 1) % 50)) {
                                    Console.ReadKey();
                                }
                            }
                            Console.WriteLine($"       ... {array.Length} item(s) returned ({sw.Elapsed}).");
                        }
                        catch (Exception e) {
                            Console.WriteLine(e);
                            Console.WriteLine($"Query submitted : \"{gremlin}\" ({sw.Elapsed}).");
                        }
                        sw.Stop();
                        history.AddFirst(batch);
                        batch = new List<string>();
                        continue;
                    }
                    if (command == "exit") {
                        File.WriteAllText("_history.tmp", JArray.FromObject(history).ToString());
                        return;
                    }
                    else if (command == "cls") {
                        WriteHeader();
                        batch = new List<string>();
                        continue;
                    }
                    else if (command == "history" || command == "h") {
                        var historyArray = history.ToArray();
                        for (var i = 0; i < historyArray.Length; i++) {
                            var gremlin = string.Concat(historyArray[i].Select(s => s.Trim()));
                            Console.WriteLine($"[{i}]\t\"{gremlin}\"");
                        }
                        while (true) {
                            Console.Write("Select index: ");
                            var selection = Console.ReadLine();
                            if (selection == "exit") {
                                break;
                            }
                            if (int.TryParse(selection, out var index) &&
                                index < historyArray.Length) {
                                if (batch.Any()) {
                                    history.AddFirst(batch);
                                }
                                batch = historyArray[index];
                                WriteHeader();
                                break;
                            }
                            Console.WriteLine("Try again!");
                        }
                    }

                    else if (command == "again" || command == "a") {
                        if (history.Any()) {
                            batch = history.First.Value;
                            WriteHeader();
                        }
                    }

                    else if (command == "help" || command == "?") {
                        Console.WriteLine(":-)");
                        continue;
                    }

                    else if (command[0] == '!' || command[0] == '=') {
                        // replace
                        var rest = command.Substring(1);
                        if (batch.Count > 0) {
                            var i = batch.Count - 1;
                            if (!string.IsNullOrEmpty(rest)) {
                                if (!int.TryParse(rest, out i)) {
                                    i = -1;
                                }
                            }
                            if (i >= 0 && i < batch.Count) {
                                string selection = null;
                                if (command[0] == '=') {
                                    Console.Write($"{i}= ");
                                    selection = Console.ReadLine();
                                }
                                batch.RemoveAt(i);
                                if (command[0] == '=') {
                                    batch.Insert(i, selection);
                                }
                            }
                        }
                    }
                    else if (command[0] == '>') {
                        // insert
                        var rest = command.Substring(1);
                        if (string.IsNullOrEmpty(rest) ||
                            !int.TryParse(rest, out var i)) {
                            i = batch.Count;
                        }
                        Console.Write($"{i}> ");
                        var selection = Console.ReadLine();
                        if (i < batch.Count) {
                            batch.Insert(i, selection);
                        }
                        else {
                            batch.Add(selection);
                        }
                    }
                    else {
                        batch.Add(command);
                        continue;
                    }

                    foreach (var line in batch) {
                        Console.WriteLine($":>     {line}");
                    }
                }
            }
        }

        /// <summary>
        /// Run gremlin from file input line by line
        /// </summary>
        public static async Task RunGremlinFileAsync(CliOptions options) {
            var graph = await GetGraphAsync(options);
            using (var gremlinClient = graph.OpenGremlinClient()) {
                var lines = await File.ReadAllLinesAsync(
                    options.GetValueOrDefault("-f", "--file", "gremlin.txt"));
                foreach (var gremlin in lines) {
                    Console.WriteLine($"\"{gremlin}\" ...");
                    var feed = gremlinClient.Submit<dynamic>(gremlin);
                    try {
                        var results = await feed.AllAsync();
                        var array = results.ToArray();
                        for (var i = 0; i < array.Length; i++) {
                            Console.WriteLine($"[{i + 1}]\t{array[i]}");
                        }
                        Console.WriteLine($"       ... {array.Length} item(s) returned.");
                    }
                    catch (Exception e) {
                        Console.WriteLine(e);
                    }
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
        /// Get collection interface
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<IGraph> GetGraphAsync(CliOptions options) {
            var database = await GetDatabaseAsync(options);
            var coll = await database.OpenContainerAsync(
                options.GetValueOrDefault("-c", "--collection", "default"));
            return coll.AsGraph();
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
