// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using CommandLine;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

/// <summary>
/// Command line parameters
/// </summary>
internal sealed class Parameters : IDisposable
{
    [Option('c', nameof(EdgeHubConnectionString), Required = false,
        HelpText = "The edge hub connection string that OPC Publisher is using " +
            "to bootstrap the rest api." +
            "\nDefaults to value of environment variable 'EdgeHubConnectionString'.")]
    public string? EdgeHubConnectionString { get; set; } =
        Environment.GetEnvironmentVariable(nameof(EdgeHubConnectionString));

    [Option('s', nameof(StorageConnectionString), Required = false,
        HelpText = "The storage connection string to use to upload capture bundles." +
        "\nDefaults to value of environment variable 'StorageConnectionString'.")]
    public string? StorageConnectionString { get; set; } =
        Environment.GetEnvironmentVariable(nameof(StorageConnectionString));

    [Option('m', nameof(PublisherModuleId), Required = false,
        HelpText = "The module id of the opc publisher." +
            "\nDefaults to value of environment variable 'PublisherModuleId'.")]
    public string PublisherModuleId { get; set; } =
        Environment.GetEnvironmentVariable(nameof(PublisherModuleId)) ?? "publisher";

    [Option('d', nameof(PublisherDeviceId), Required = false,
        HelpText = "The device id of the opc publisher." +
            "\nDefaults to value of environment variable 'PublisherModuleId'.")]
    public string? PublisherDeviceId { get; set; } =
        Environment.GetEnvironmentVariable(nameof(PublisherDeviceId));

    [Option('r', nameof(PublisherRestApiEndpoint), Required = false,
        HelpText = "The Rest api endpoint of the opc publisher." +
        "\nDefaults to value of environment variable 'PublisherRestApiEndpoint'.")]
    public string? PublisherRestApiEndpoint { get; set; } =
        Environment.GetEnvironmentVariable(nameof(PublisherRestApiEndpoint));

    [Option('e', nameof(OpcServerEndpointUrl), Required = false,
        HelpText = "The endpoint of the opc publisher." +
        "\nDefaults to value of environment variable 'OpcServerEndpointUrl'.")]
    public string? OpcServerEndpointUrl { get; set; } =
        Environment.GetEnvironmentVariable(nameof(OpcServerEndpointUrl));

    [Option('t', nameof(CaptureDuration), Required = false,
        HelpText = "The capture duration until data is uploaded and capture is restarted." +
        "\nDefaults to value of environment variable 'CaptureDuration'.")]
    public TimeSpan? CaptureDuration { get; set; } = TimeSpan.TryParse(
        Environment.GetEnvironmentVariable(nameof(CaptureDuration)), out var t) ? t : null;

    public void Dispose()
    {
        HttpClient.Dispose();
        _certificate?.Dispose();
        _apiKey = null;
    }

    internal HttpClient HttpClient { get; private set; } = new HttpClient();

    /// <summary>
    /// Parse parameters
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async ValueTask<Parameters> Parse(string[] args)
    {
        Parameters? parameters = null;
        ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
            .WithParsed(parsedParams => parameters = parsedParams)
            .WithNotParsed(errors => Environment.Exit(1));
        Debug.Assert(parameters != null);

        Twin twin;
        var iothubConnectionString =
            Environment.GetEnvironmentVariable("IoTHubOwnerConnectionString") ??
            Environment.GetEnvironmentVariable("_HUB_CS");
        // NOTE: For testing locally only
        if (!string.IsNullOrEmpty(iothubConnectionString))
        {
            string deviceId;
            var ncModuleId = "netcap";
            if (!string.IsNullOrWhiteSpace(parameters.EdgeHubConnectionString))
            {
                // Get device and module id from edge hub connection string provided
                var ehc = Microsoft.Azure.Devices.Client.IotHubConnectionStringBuilder.Create(
                    parameters.EdgeHubConnectionString);
                deviceId = ehc.DeviceId;
                ncModuleId = ehc.ModuleId ?? ncModuleId;
            }
            else
            {
                // Default device to host name just like we do it in our publisher CLI
                deviceId = Dns.GetHostName().ToLowerInvariant();
            }

            // Create a registry client to communicate with service-facing endpoint on your hub.
            using var rm = RegistryManager.CreateFromConnectionString(
                iothubConnectionString);
            // Create module if not exist
            try { await rm.AddDeviceAsync(new Device(deviceId)).ConfigureAwait(false); }
            catch (DeviceAlreadyExistsException) { }
            try { await rm.AddModuleAsync(new Module(deviceId, ncModuleId)).ConfigureAwait(false); }
            catch (ModuleAlreadyExistsException) { }

            twin = await rm.GetTwinAsync(deviceId, ncModuleId).ConfigureAwait(false);
            parameters.PublisherModuleId = twin.GetProperty(nameof(PublisherModuleId),
                parameters.PublisherModuleId);
            parameters.PublisherDeviceId ??= deviceId;
            Debug.Assert(parameters.PublisherModuleId != null);
            Debug.Assert(parameters.PublisherDeviceId != null);

            Console.WriteLine("Connecting to OPC Publisher Module " +
                $"{parameters.PublisherModuleId} on {parameters.PublisherDeviceId} via IoTHub...");
            using var serviceClient = ServiceClient.CreateFromConnectionString(iothubConnectionString);
            var apiKeyResponse = await serviceClient.InvokeDeviceMethodAsync(parameters.PublisherDeviceId,
                parameters.PublisherModuleId, new CloudToDeviceMethod("GetApiKey")).ConfigureAwait(false);
            parameters._apiKey =
                JsonSerializer.Deserialize<string>(apiKeyResponse.GetPayloadAsJson());
            var certResponse = await serviceClient.InvokeDeviceMethodAsync(parameters.PublisherDeviceId,
                parameters.PublisherModuleId, new CloudToDeviceMethod("GetServerCertificate")).ConfigureAwait(false);
            parameters._certificate = X509Certificate2.CreateFromPem(
                JsonSerializer.Deserialize<string>(certResponse.GetPayloadAsJson()));
        }
        // TODO: else if (_apiKey != null && _certificate != null))
        // TODO: {
        // TODO:     // Use the provided API key and certificate
        // TODO: }
        else
        {
            // Call the "GetApiKey" and "GetServerCertificate" methods on the publisher module
            var moduleClient = string.IsNullOrWhiteSpace(parameters.EdgeHubConnectionString) ?
                (await ModuleClient.CreateFromEnvironmentAsync().ConfigureAwait(false)) :
                ModuleClient.CreateFromConnectionString(parameters.EdgeHubConnectionString);
            twin = await moduleClient.GetTwinAsync().ConfigureAwait(false);

            var deviceId = twin.DeviceId;
            parameters.PublisherModuleId = twin.GetProperty(nameof(PublisherModuleId),
                parameters.PublisherModuleId);
            parameters.PublisherDeviceId = deviceId; // Override as we must be in the same device
            Debug.Assert(parameters.PublisherModuleId != null);
            Debug.Assert(parameters.PublisherDeviceId != null);

            Console.WriteLine("Connecting to OPC Publisher Module " +
                $"{parameters.PublisherModuleId} on {parameters.PublisherDeviceId}...");
            var apiKeyResponse = await moduleClient.InvokeMethodAsync(parameters.PublisherDeviceId,
                parameters.PublisherModuleId, new MethodRequest("GetApiKey")).ConfigureAwait(false);
            parameters._apiKey =
                JsonSerializer.Deserialize<string>(apiKeyResponse.Result);
            var certResponse = await moduleClient.InvokeMethodAsync(parameters.PublisherDeviceId,
                parameters.PublisherModuleId, new MethodRequest("GetServerCertificate")).ConfigureAwait(false);
            parameters._certificate = X509Certificate2.CreateFromPem(
                JsonSerializer.Deserialize<string>(certResponse.Result));
        }

        // Set any missing info from the twin
        parameters.OpcServerEndpointUrl = twin.GetProperty(
            nameof(OpcServerEndpointUrl), parameters.OpcServerEndpointUrl);
        parameters.PublisherRestApiEndpoint = twin.GetProperty(
            nameof(PublisherRestApiEndpoint), parameters.PublisherRestApiEndpoint);
        var captureDuration = twin.GetProperty(nameof(CaptureDuration));
        if (!string.IsNullOrWhiteSpace(captureDuration) &&
            TimeSpan.TryParse(captureDuration, out var duration))
        {
            parameters.CaptureDuration = duration;
        }
        await parameters.CreateHttpClientWithAuthAsync().ConfigureAwait(false);
        return parameters;
    }

    /// <summary>
    /// Create client
    /// </summary>
    /// <returns></returns>
    private async ValueTask CreateHttpClientWithAuthAsync()
    {
        if (_apiKey != null)
        {
            HttpClient.Dispose();
            HttpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, cert, _, _)
                    => cert != null && _certificate?.Thumbprint == cert?.Thumbprint
            });
            HttpClient.BaseAddress = await GetOpcPublisherRestEndpoint().ConfigureAwait(false);
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("ApiKey", _apiKey);
        }

        /// <summary>
        /// Publisher Endpoint
        /// </summary>
        async ValueTask<Uri> GetOpcPublisherRestEndpoint()
        {
            if (PublisherRestApiEndpoint != null &&
                Uri.TryCreate(PublisherRestApiEndpoint,
                UriKind.Absolute, out var u))
            {
                return u;
            }
            var host = PublisherModuleId;
            if (host != null)
            {
                // Poor man ping
                try
                {
                    var result = await Dns.GetHostAddressesAsync(
                        PublisherModuleId).ConfigureAwait(false);
                    if (result.Length == 0) { host = null; }
                }
                catch { host = null; }
            }
            if (host == null)
            {
                host = "localhost";
            }
            var uri = new UriBuilder
            {
                Scheme = "https",
                Port = 9072,
                Host = host
            };
            if (_apiKey == null)
            {
                uri.Scheme = "http";
                uri.Port = 9071;
            }
            return uri.Uri;
        }
    }

    private X509Certificate2? _certificate;
    private string? _apiKey;
    internal static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };
}
