
using System;
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

/// <summary>
/// Helper to connect to publisher
/// </summary>
internal class SamplesHelper
{
    const string HttpEndpoint = "http://localhost:9071";
    const string HttpsEndpoint = "https://localhost:9072";

    /// <summary>
    /// Publisher Endpoint
    /// </summary>
    public string OpcPublisher { get; } = HttpEndpoint;

    /// <summary>
    /// Plc endpoint
    /// </summary>
    public string OpcPlc { get; } = "opc.tcp://opcplc:50000";

    /// <summary>
    /// Get shared instance
    /// </summary>
    public static SamplesHelper Shared { get; } = new();

    /// <summary>
    /// Create client
    /// </summary>
    /// <returns></returns>
    public HttpClient CreateClient() => new HttpClient();
}

