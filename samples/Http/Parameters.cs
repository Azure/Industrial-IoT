// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using CommandLine;
using Microsoft.Azure.Devices;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

/// <summary>
/// Command line parameters
/// </summary>
internal sealed class Parameters : IDisposable
{
    [Option(
        'c',
        "IoTHubOwnerConnectionString",
        Required = false,
        HelpText = "The IoT hub connection string. This is available under the 'Shared access policies' in the Azure portal." +
        "\nDefaults to value of environment variable 'IoTHubOwnerConnectionString'.")]
    public string? IoTHubOwnerConnectionString { get; set; } = Environment.GetEnvironmentVariable("IoTHubOwnerConnectionString");

    [Option(
        'e',
        "EdgeHubConnectionString",
        Required = false,
        HelpText = "The edge hub connection string that OPC Publisher is using mainly for the module and device id values." +
        "\nDefaults to value of environment variable 'EdgeHubConnectionString'.")]
    public string? EdgeHubConnectionString { get; set; } = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

    const string HttpEndpoint = "http://localhost:9071";
    const string HttpsEndpoint = "https://localhost:9072";

    /// <summary>
    /// Publisher Endpoint
    /// </summary>
    public string OpcPublisher => ApiKey == null ? HttpEndpoint : HttpsEndpoint;

    /// <summary>
    /// Plc endpoint
    /// </summary>
    public string OpcPlc { get; } = "opc.tcp://opcplc:50000";

    /// <summary>
    /// Certificate
    /// </summary>
    public X509Certificate2? Certificate { get; private set; }

    /// <summary>
    /// Api key
    /// </summary>
    public string? ApiKey { get; private set; }

    internal static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>
    /// Create client
    /// </summary>
    /// <returns></returns>
    public HttpClient CreateHttpClientWithAuth()
    {
        if (ApiKey == null)
        {
            return new HttpClient();
        }
        var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, cert, _, _)
                => cert != null && Certificate?.Thumbprint == cert?.Thumbprint
        });
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("ApiKey", ApiKey);
        return httpClient;
    }

    public void Dispose()
    {
        Certificate?.Dispose();
        ApiKey = null;
    }

    /// <summary>
    /// Parse parameters
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task<Parameters> Parse(string[] args)
    {
        Parameters? parameters = null;
        ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
            .WithParsed(parsedParams => parameters = parsedParams)
            .WithNotParsed(errors => Environment.Exit(1));
        Debug.Assert(parameters != null);

        var deviceId = string.Empty;
        var moduleId = string.Empty;
        try
        {
            var ehc = IotHubConnectionStringBuilder.Create(parameters.EdgeHubConnectionString);
            deviceId = ehc.DeviceId;
            moduleId = ehc.ModuleId;

            // Create a ServiceClient to communicate with service-facing endpoint on your hub.
            using var registryManager = RegistryManager.CreateFromConnectionString(
                parameters.IoTHubOwnerConnectionString);
            var twin = await registryManager.GetTwinAsync(deviceId, moduleId).ConfigureAwait(false);
            parameters.ApiKey = (string)twin.Properties.Reported["__apikey__"];
            var cert = (byte[])twin.Properties.Reported["__certificate__"];
            parameters.Certificate = new X509Certificate2(cert, parameters.ApiKey);
            return parameters;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occurred trying to access api key and certificate.");

            // In production this should be thrown and not continued.
            Console.WriteLine(ex.Message);
        }
        Console.WriteLine("Specify connection strings to use the secure endpoint.");
        Console.WriteLine("The unsecure endpoint will be used for demonstration purposes.");
        Console.WriteLine("This is a security risk and should not be used in production!!!");
        return parameters;
    }
}
