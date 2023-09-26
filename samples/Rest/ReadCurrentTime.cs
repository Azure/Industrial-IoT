﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Text.Json;
using System.Net.Http.Json;

Console.WriteLine("Press key to exit");
Console.WriteLine();
using var cts = new CancellationTokenSource();
_ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); });

// Connect to publisher
using var httpClient = SamplesHelper.Shared.CreateClient();

while (true)
{
    // Read current time from server - see /read api in api.md
    using var response = await httpClient.PostAsJsonAsync(SamplesHelper.Shared.OpcPublisher + "/v2/read", new
    {
        connection = new
        {
            endpoint = new
            {
                url = SamplesHelper.Shared.OpcPlc,
                securityMode = "SignAndEncrypt"
            }
        },
        request = new
        {
            nodeId = "i=2258"
        }
    }).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();

    var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
    Console.WriteLine("Current time on server:" + responseJson.GetProperty("value").GetString());
}
