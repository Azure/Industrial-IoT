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
var factory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = factory.CreateLogger("Netcap");

// Connect to publisher
var publisher = new Publisher(factory.CreateLogger("Publisher"), parameters.HttpClient,
    parameters.OpcServerEndpointUrl);

for (var i = 0; !cts.IsCancellationRequested; i++)
{
    // Get and endpoint urls and addresses to monitor if not set
    if (!await publisher.TryUpdateEndpointsAsync(cts.Token).ConfigureAwait(false))
    {
        Console.WriteLine("waiting .....");
        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token).ConfigureAwait(false);
    }

    // Capture traffic for duration
    using var timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
    if (!string.IsNullOrEmpty(parameters.StorageConnectionString) ||
        parameters.CaptureDuration != null)
    {
        timeoutToken.CancelAfter(parameters.CaptureDuration ?? TimeSpan.FromMinutes(5));
    }
    var folder = Path.Combine(Path.GetTempPath(), "capture" + i);
    using (var capture = Pcap.Capture(publisher.Addresses, folder, factory.CreateLogger("Pcap_" + i)))
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
            }
            catch (OperationCanceledException) { } // Done
        }
    }

    if (!string.IsNullOrEmpty(parameters.StorageConnectionString))
    {
        // TODO: move to seperate task
        using var uploader = new Uploader(folder, publisher.PnJson,
            parameters.PublisherDeviceId ?? "unknown", parameters.PublisherModuleId,
            parameters.StorageConnectionString, factory.CreateLogger("Upload_" + i));
        await uploader.UploadAsync(cts.Token).ConfigureAwait(false);
    }
}
