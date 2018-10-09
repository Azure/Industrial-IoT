// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Ssh.Cli {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Net.Ssh;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json;
    using System;
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
                        case "folderup":
                            await UploadFolderAsync(shell, options);
                            break;
                        case "folderdown":
                            await DownloadFolderAsync(shell, options);
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
            CliOptions options) {

            var user = options.GetValueOrDefault<string>("-u", "--user", null);
            var pw = options.GetValueOrDefault<string>("-p", "--password", null);

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
                options.GetValueOrDefault("-t", "--timeout", -1));
            return await shellFactory.OpenSecureShellAsync(
                options.GetValue<string>("-h", "--host"),
                options.GetValueOrDefault("-p", "--port", 22),
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
            CliOptions options) {
            if (shell == null) {
                throw new ArgumentException("Must login first");
            }
            var cts = new CancellationTokenSource(
                options.GetValueOrDefault("-t", "--timeout", -1));
            await shell.BindAsync(cts.Token);
        }

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task UploadAsync(ISecureShell shell,
            CliOptions options) {
            var to = options.GetValue<string>("-t", "--to");
            var file = options.GetValue<string>("-f", "--file");
            var from = options.GetValue<string>("-p", "--path");
            await UploadFileAsync(shell, options, to, from, file);
        }

        /// <summary>
        /// Upload folder
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task UploadFolderAsync(ISecureShell shell,
            CliOptions options) {
            var to = options.GetValue<string>("-t", "--to");
            var from = options.GetValue<string>("-p", "--path");

            await UploadFolderAsync(shell, options, to, from);
        }

        /// <summary>
        /// Upload folder
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        private static async Task UploadFolderAsync(ISecureShell shell,
            CliOptions options, string to, string from) {
            foreach (var file in Directory.EnumerateFiles(from)) {
                await UploadFileAsync(shell, options, to, from, file);
            }
            if (!options.GetValueOrDefault("-r", "--recursive", false)) {
                return;
            }
            foreach (var dir in Directory.EnumerateDirectories(from)) {
                await UploadFolderAsync(shell, options,
                    to + "/" + dir, Path.Combine(from, dir));
                // TODO: Path seperator is platform specific
            }
        }

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task UploadFileAsync(ISecureShell shell,
            CliOptions options, string to, string from, string file) {
            var buffer = await File.ReadAllBytesAsync(Path.Combine(from, file));
            await shell.UploadAsync(buffer, file, to,
                options.GetValueOrDefault("-h", "--home", true),
                options.GetValueOrDefault<string>("-m", "--mode", null));
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task DownloadAsync(ISecureShell shell,
            CliOptions options) {
            var file = options.GetValue<string>("-f", "--file");
            var from = options.GetValue<string>("-p", "--path");
            var str = await shell.DownloadAsync(file, from,
                options.GetValueOrDefault("-h", "--home", true));
            PrintResult(options, str);
        }

        /// <summary>
        /// Download folder
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task DownloadFolderAsync(ISecureShell shell,
            CliOptions options) {
            var from = options.GetValue<string>("-f", "--from");
            var path = options.GetValue<string>("-p", "--path");
            await shell.DownloadFolderAsync(path, from,
                options.GetValueOrDefault("-h", "--home", true));
        }

        /// <summary>
        /// Run command
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ExecuteCommandAsync(ISecureShell shell,
            CliOptions options) {
            var str = await shell.ExecuteCommandAsync(
                options.GetValue<string>("-c", "--command"));
            PrintResult(options, str);
        }

        /// <summary>
        /// Print result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <param name="status"></param>
        private static void PrintResult<T>(CliOptions options,
            T status) {
            Console.WriteLine("==================");
            Console.WriteLine(JsonConvert.SerializeObject(status,
                options.GetValueOrDefault("-F", "--format", Formatting.Indented)));
            Console.WriteLine("==================");
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

     folderdown  Download folder to local path
        with ...
        -f, --from      Folder to download
        -p, --path      File path to read from
        -h, --home      Whether relative to home dir (default
                        true)

     folderup    Upload folder
        with ...
        -p, --path      File path to upload from
        -t, --to        File path to write to
        -r, --recursive Include subfolders
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
