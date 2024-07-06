// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Netcap;
using System.Net.Http.Json;
using System.Text.Json;

using var parameters = await Parameters.Parse(args).ConfigureAwait(false);

Console.WriteLine("Press key to exit");
Console.WriteLine();

using var cts = new CancellationTokenSource();
_ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); });
var factory = LoggerFactory.Create(builder => builder
    .AddSimpleConsole(options => options.SingleLine = true));
var logger = factory.CreateLogger("Netcap");

// Connect to publisher
var publisher = new Publisher(factory.CreateLogger("Publisher"), parameters.HttpClient,
    parameters.OpcServerEndpointUrl);

Uploader? uploader = null;
if (!string.IsNullOrEmpty(parameters.StorageConnectionString))
{
    // TODO: move to seperate task
    uploader = new Uploader(
        parameters.PublisherDeviceId ?? "unknown", parameters.PublisherModuleId,
        parameters.StorageConnectionString, factory.CreateLogger("Upload"));
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
    if (uploader != null || parameters.CaptureDuration != null)
    {
        var duration = parameters.CaptureDuration ?? TimeSpan.FromMinutes(10);
        logger.LogInformation("Capturing for {Duration}", duration);
        timeoutToken.CancelAfter(duration);
    }
    var folder = Path.Combine(Path.GetTempPath(), "capture" + i);

    var capture = new CaptureBundle(publisher, factory.CreateLogger("Capture"), folder);
    using (capture.CreatePcap(i))
    {
        while (!timeoutToken.IsCancellationRequested)
        {
            // Watch session diagnostics while we capture
            try
            {
                await foreach (var diagnostic in parameters.HttpClient
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
