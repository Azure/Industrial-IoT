// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Netcap;
using System.Net.Http.Json;
using System.Text.Json;

using var cmdLine = await CmdLine.Parse(args).ConfigureAwait(false);

var factory = LoggerFactory.Create(builder => builder
    .AddSimpleConsole(options => options.SingleLine = true));

using var cts = new CancellationTokenSource();
_ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); });
var logger = factory.CreateLogger("Netcap");

if (cmdLine.DeployToEdge)
{
    // Run deployment
    var deployer = new Deployer(logger);
    await deployer.RunAsync(cts.Token).ConfigureAwait(false);
    return;
}

Console.WriteLine("Press key to exit");
Console.WriteLine();

// Connect to publisher
var publisher = new Publisher(factory.CreateLogger("Publisher"), cmdLine.HttpClient,
    cmdLine.OpcServerEndpointUrl);

Uploader? uploader = null;
if (!string.IsNullOrEmpty(cmdLine.StorageConnectionString))
{
    // TODO: move to seperate task
    uploader = new Uploader(
        cmdLine.PublisherDeviceId ?? "unknown", cmdLine.PublisherModuleId,
        cmdLine.ModuleClient, cmdLine.StorageConnectionString,
        factory.CreateLogger("Upload"));
}

for (var i = 0; !cts.IsCancellationRequested; i++)
{
    // Get and endpoint urls and addresses to monitor if not set
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

    var capture = new Bundle(publisher, factory.CreateLogger("Capture"), folder);
    using (capture.CreatePcap(i))
    {
        while (!timeoutToken.IsCancellationRequested)
        {
            // Watch session diagnostics while we capture
            try
            {
                await foreach (var diagnostic in cmdLine.HttpClient
                    .GetFromJsonAsAsyncEnumerable<JsonElement>(
                        "v2/diagnostics/connections/watch",
                        cancellationToken: timeoutToken.Token).ConfigureAwait(false))
                {
                    await capture.AddSessionKeysFromDiagnosticsAsync(
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
        await uploader.UploadAsync(capture, cts.Token).ConfigureAwait(false);
    }
}
