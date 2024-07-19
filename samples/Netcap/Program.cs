// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Netcap;
using System.Net.Http.Json;
using System.Text.Json;

using var cts = new CancellationTokenSource();

Console.WriteLine($@"
   ____  _____   _____   _   _      _
  / __ \|  __ \ / ____| | \ | |    | |
 | |  | | |__) | |      |  \| | ___| |_ ___ __ _ _ __
 | |  | |  ___/| |      | . ` |/ _ \ __/ __/ _` | '_ \
 | |__| | |    | |____  | |\  |  __/ || (_| (_| | |_) |
  \____/|_|     \_____| |_| \_|\___|\__\___\__,_| .__/
                                                | |
                                                |_| {typeof(Extensions).Assembly.GetVersion()}
");

using var cmdLine = await CmdLine.CreateAsync(args, cts.Token).ConfigureAwait(false);
if (cmdLine.Install || cmdLine.Uninstall)
{
    return;
}

if (!Extensions.IsRunningInContainer())
{
    _ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); });
    Console.WriteLine("Press any key to exit");
    Console.WriteLine();
}

var logger = cmdLine.Logger.CreateLogger("Netcap");
try
{
    // Connect to publisher
    var publisher = new Publisher(cmdLine.Logger.CreateLogger("Publisher"), cmdLine.HttpClient,
        cmdLine.OpcServerEndpointUrl);

    Storage? uploader = null;
    if (!string.IsNullOrEmpty(cmdLine.StorageConnectionString))
    {
        logger.LogInformation("Uploading to storage...");
        // TODO: move to seperate task
        uploader = new Storage(cmdLine.PublisherDeviceId ?? "unknown", cmdLine.PublisherModuleId,
            cmdLine.StorageConnectionString, cmdLine.Logger.CreateLogger("Upload"));
    }

    for (var i = 0; !cts.IsCancellationRequested; i++)
    {
        // Get endpoint urls and addresses to monitor if not set
        if (!await publisher.TryUpdateEndpointsAsync(cts.Token).ConfigureAwait(false))
        {
            logger.LogInformation("waiting .....");
            await Task.Delay(TimeSpan.FromMinutes(1), cts.Token).ConfigureAwait(false);
        }

        // Capture traffic for duration
        using var timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        if (uploader != null || cmdLine.CaptureDuration != null)
        {
            var duration = cmdLine.CaptureDuration ?? TimeSpan.FromMinutes(10);
            logger.LogInformation("Capturing for {Duration}", duration);
            timeoutToken.CancelAfter(duration);
        }
        var folder = Path.Combine(Path.GetTempPath(), "capture" + i);

        var bundle = new Bundle(cmdLine.Logger.CreateLogger("Capture"), folder);
        using (bundle.CaptureNetworkTraces(publisher, i))
        {
            while (!timeoutToken.IsCancellationRequested)
            {
                // Watch session diagnostics while we capture
                try
                {
                    logger.LogInformation("Monitoring diagnostics at {Url}...",
                        cmdLine.HttpClient.BaseAddress);
                    await foreach (var diagnostic in
                        cmdLine.HttpClient.GetFromJsonAsAsyncEnumerable<JsonElement>(
                        "v2/diagnostics/connections/watch",
                            cancellationToken: timeoutToken.Token).ConfigureAwait(false))
                    {
                        await bundle.AddSessionKeysFromDiagnosticsAsync(
                            diagnostic, publisher.Endpoints).ConfigureAwait(false);
                    }
                    logger.LogInformation("Restart monitoring diagnostics...");
                }
                catch (OperationCanceledException) { } // Done
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error monitoring diagnostics - restarting...");
                }
            }
        }

        // TODO: move to seperate task
        if (uploader != null)
        {
            await uploader.UploadAsync(bundle, cts.Token).ConfigureAwait(false);
        }
    }
}
catch (OperationCanceledException) { }
catch (Exception ex)
{
    logger.LogError(ex, "Failed to run.");
}
