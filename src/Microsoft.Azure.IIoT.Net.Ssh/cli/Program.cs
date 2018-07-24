// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Ssh.Cli {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Net.Ssh;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
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
            RunAsync(args).Wait();
        }

        /// <summary>
        /// Run client
        /// </summary>
        /// <param name="args">command-line arguments</param>
        public static async Task RunAsync(string[] args) {
            var sshFactory = new SshShellFactory(new SimpleLogger());
            ISecureShell shell = null;
            var run = true;
            do {
                if (run) {
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
                            run = false;
                            break;
                        case "logout":
                            shell = Logout(shell);
                            break;
                        case "login":
                            if (shell != null) {
                                throw new ArgumentException("Already logged in.");
                            }
                            shell = await LoginAsync(sshFactory, options);
                            break;
                        case "terminal":
                            await RunTerminalAsync(shell, options);
                            break;
                        case "exec":
                            await ExecuteCommandAsync(shell, options);
                            break;
                        case "download":
                            await DownloadAsync(shell, options);
                            break;
                        case "upload":
                            await UploadAsync(shell, options);
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
                    if (!run) {
                        PrintHelp();
                        return;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("==================");
                    Console.WriteLine(e);
                    Console.WriteLine("==================");
                }
            }
            while (run);
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="shellFactory"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<ISecureShell> LoginAsync(IShellFactory shellFactory,
            Dictionary<string, string> options) {

            var user = GetOption<string>(options, "-u", "--user", null);
            var pw = GetOption<string>(options, "-p", "--password", null);

            while (string.IsNullOrEmpty(user)) {
                Console.WriteLine("User:");
                user = Console.ReadLine();
            }

            NetworkCredential creds = null;
            if (string.IsNullOrEmpty(pw)) {
                Console.WriteLine("Password:");
                creds = new NetworkCredential(user, ConsoleEx.ReadPassword());
            }
            else {
                creds = new NetworkCredential(user, pw);
            }

            var cts = new CancellationTokenSource(
                GetOption(options, "-t", "--timeout", -1));
            return await shellFactory.OpenSecureShellAsync(
                GetOption<string>(options, "-h", "--host"),
                GetOption(options, "-p", "--port", 22),
                creds, cts.Token);
        }

        /// <summary>
        /// Logout
        /// </summary>
        /// <param name="shell"></param>
        /// <returns></returns>
        private static ISecureShell Logout(ISecureShell shell) {
            if (shell == null) {
                Console.WriteLine("Not logged in.");
            }
            else {
            shell.Dispose();
                }
            return null;
        }


        /// <summary>
        /// Shell into simulation
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task RunTerminalAsync(ISecureShell shell,
            Dictionary<string, string> options) {
            if (shell == null) {
                throw new ArgumentException("Must login first");
            }
            var cts = new CancellationTokenSource(
                GetOption(options, "-t", "--timeout", -1));
            await shell.BindAsync(cts.Token);
        }

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task UploadAsync(ISecureShell shell,
            Dictionary<string, string> options) {
            var to = GetOption<string>(options, "-t", "--to");
            var file = GetOption<string>(options, "-f", "--file");
            var from = GetOption<string>(options, "-p", "--path");
            var buffer = await File.ReadAllBytesAsync(Path.Combine(from, file));
            await shell.UploadAsync(buffer, file, to,
                GetOption(options, "-h", "--home", true),
                GetOption<string>(options, "-m", "--mode", null));
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task DownloadAsync(ISecureShell shell,
            Dictionary<string, string> options) {
            var file = GetOption<string>(options, "-f", "--file");
            var from = GetOption<string>(options, "-p", "--path");
            var str = await shell.DownloadAsync(file, from,
                GetOption(options, "-h", "--home", true));
            PrintResult(options, str);
        }

        /// <summary>
        /// Run command
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ExecuteCommandAsync(ISecureShell shell,
            Dictionary<string, string> options) {
            var str = await shell.ExecuteCommandAsync(
                GetOption<string>(options, "-c", "--command"));
            PrintResult(options, str);
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
        /// Print help
        /// </summary>
        private static void PrintHelp() {
            Console.WriteLine(
                @"
Ssh cli - Allows to use secure ssh shell api
usage:      sshcli command [options]

Commands and Options

     login       Login and start shell
        with ...
        -h, --host      Host name.
        -p, --port      Port (default 22)
        -u, --user      User name (default to prompt)
        -p, --password  Password (default to prompt)
        -t, --timeout   Timeout of connection attempt (optional)

     logout      Stop and exit shell.

     download    Download file to string
        with ...
        -f, --file      File name to download
        -p, --path      File path to read from
        -h, --home      Whether relative to home dir (default 
                        true)
        -F, --format    Json format for result

     upload      Upload file
        with ...
        -f, --file      File name
        -p, --path      File path to upload from
        -t, --to        File path to write to
        -h, --home      Whether relative to home dir (default 
                        true)

     exec        Execute command
        with ...
        -c, --command   Command to execute.
        -F, --format    Json format for result

     terminal    Open a terminal over ssh
        with ...
        -t, --timeout   Timeout of entire session (optional)

     delete      Delete simulation
        with ...
        -i, --id        Id of simulation to delete (mandatory)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }
    }
}
