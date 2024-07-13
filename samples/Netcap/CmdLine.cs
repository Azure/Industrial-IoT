// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using CommandLine;
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
internal sealed class CmdLine : IDisposable
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
            "\nDefaults to value of environment variable 'PublisherDeviceId'.")]
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

    /// <summary>
    /// Http Client
    /// </summary>
    internal HttpClient HttpClient { get; private set; } = new HttpClient();

    /// <summary>
    /// Module client
    /// </summary>
    internal ModuleClient? ModuleClient { get; private set; }

    /// <summary>
    /// Deploy netcap module and run remotely
    /// </summary>
    internal bool DeployToEdge { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        HttpClient.Dispose();
        ModuleClient?.Dispose();
        _certificate?.Dispose();
        _apiKey = null;
    }

    /// <summary>
    /// Parse parameters
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async ValueTask<CmdLine> Parse(string[] args)
    {
        CmdLine? parameters = null;
        ParserResult<CmdLine> result = Parser.Default.ParseArguments<CmdLine>(args)
            .WithParsed(parsedParams => parameters = parsedParams)
            .WithNotParsed(errors => Environment.Exit(1));
        Debug.Assert(parameters != null);

        if (!string.IsNullOrWhiteSpace(parameters.EdgeHubConnectionString) ||
            Environment.GetEnvironmentVariable("IOTEDGE_WORKLOADURI") != null)
        {
            await parameters.ConnectAsModuleAsync().ConfigureAwait(false);
        }
// TODO:else if (_apiKey != null && _certificate != null))
// TODO:{
// TODO:    // Use the provided API key and certificate
// TODO:    // Support to run without iot edge and hub
// TODO:}
        else
        {
            var iothubConnectionString =
                Environment.GetEnvironmentVariable("IoTHubOwnerConnectionString") ??
                Environment.GetEnvironmentVariable("_HUB_CS");
            if (string.IsNullOrEmpty(iothubConnectionString))
            {
                // Deploy netcap module
                parameters.DeployToEdge = true;
            }
            else
            {
                // NOTE: For testing locally only
                await parameters.ConnectAsIoTHubOwnerAsync(
                    iothubConnectionString).ConfigureAwait(false);
            }
        }
        return parameters;
    }

    /// <summary>
    /// Connect module to edge hub
    /// </summary>
    /// <returns></returns>
    private async ValueTask ConnectAsModuleAsync()
    {
        // Call the "GetApiKey" and "GetServerCertificate" methods on the publisher module
        ModuleClient = string.IsNullOrWhiteSpace(EdgeHubConnectionString) ?
            (await ModuleClient.CreateFromEnvironmentAsync().ConfigureAwait(false)) :
            ModuleClient.CreateFromConnectionString(EdgeHubConnectionString);

        Console.WriteLine("Connecting to OPC Publisher Module " +
            $"{PublisherModuleId} on {PublisherDeviceId}...");
        await ModuleClient.OpenAsync().ConfigureAwait(false);
        var twin = await ModuleClient.GetTwinAsync().ConfigureAwait(false);

        var deviceId = twin.DeviceId;
        PublisherModuleId = twin.GetProperty(nameof(PublisherModuleId),
            PublisherModuleId);
        PublisherDeviceId = deviceId; // Override as we must be in the same device
        Debug.Assert(PublisherModuleId != null);
        Debug.Assert(PublisherDeviceId != null);

        var apiKeyResponse = await ModuleClient.InvokeMethodAsync(
            PublisherDeviceId, PublisherModuleId, 
            new MethodRequest("GetApiKey")).ConfigureAwait(false);
        _apiKey =
            JsonSerializer.Deserialize<string>(apiKeyResponse.Result);
        var certResponse = await ModuleClient.InvokeMethodAsync(
            PublisherDeviceId, PublisherModuleId, 
            new MethodRequest("GetServerCertificate")).ConfigureAwait(false);
        _certificate = X509Certificate2.CreateFromPem(
            JsonSerializer.Deserialize<string>(certResponse.Result));

        ConfigureFromTwin(twin);
        await CreateHttpClientWithAuthAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Connect with iot hub connections tring
    /// </summary>
    /// <param name="iothubConnectionString"></param>
    /// <returns></returns>
    private async ValueTask ConnectAsIoTHubOwnerAsync(
        string iothubConnectionString)
    {
        string deviceId;
        var ncModuleId = "netcap";
        if (!string.IsNullOrWhiteSpace(EdgeHubConnectionString))
        {
            // Get device and module id from edge hub connection string provided
            var ehc = IotHubConnectionStringBuilder.Create(EdgeHubConnectionString);
            deviceId = ehc.DeviceId;
            ncModuleId = ehc.ModuleId ?? ncModuleId;
        }
        else
        {
            // Default device to host name just like we do it in our publisher CLI
            deviceId = Dns.GetHostName().ToLowerInvariant();
        }
        var rm = Microsoft.Azure.Devices.RegistryManager
            .CreateFromConnectionString(iothubConnectionString);
        // Create module if not exist
        try { await rm.AddDeviceAsync(
            new Microsoft.Azure.Devices.Device(deviceId)).ConfigureAwait(false); }
        catch (DeviceAlreadyExistsException) { }
        Microsoft.Azure.Devices.Module module;
        try
        {
            module = await rm.AddModuleAsync(new Microsoft.Azure.Devices.Module(
                deviceId, ncModuleId)).ConfigureAwait(false);
        }
        catch (ModuleAlreadyExistsException)
        {
            module = await rm.GetModuleAsync(
                deviceId, ncModuleId).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(EdgeHubConnectionString))
        {
            // Create edge hub connection string
            var iotHubCs = IotHubConnectionStringBuilder.Create(iothubConnectionString);
            EdgeHubConnectionString = IotHubConnectionStringBuilder.Create(
                iotHubCs.HostName, new ModuleAuthenticationWithRegistrySymmetricKey(
                    deviceId, ncModuleId,
                    module.Authentication.SymmetricKey.PrimaryKey)).ToString();
        }

        var twin = await rm.GetTwinAsync(deviceId, ncModuleId).ConfigureAwait(false);
        PublisherModuleId = twin.GetProperty(nameof(PublisherModuleId), PublisherModuleId);
        PublisherDeviceId ??= deviceId;
        Debug.Assert(PublisherModuleId != null);
        Debug.Assert(PublisherDeviceId != null);

        Console.WriteLine("Connecting to OPC Publisher Module " +
            $"{PublisherModuleId} on {PublisherDeviceId} via IoTHub...");
        var serviceClient = Microsoft.Azure.Devices.ServiceClient
            .CreateFromConnectionString(iothubConnectionString);
        var apiKeyResponse = await serviceClient.InvokeDeviceMethodAsync(
            PublisherDeviceId, PublisherModuleId, 
            new Microsoft.Azure.Devices.CloudToDeviceMethod(
                "GetApiKey")).ConfigureAwait(false);
        _apiKey =
            JsonSerializer.Deserialize<string>(apiKeyResponse.GetPayloadAsJson());
        var certResponse = await serviceClient.InvokeDeviceMethodAsync(
            PublisherDeviceId, PublisherModuleId, 
            new Microsoft.Azure.Devices.CloudToDeviceMethod(
                "GetServerCertificate")).ConfigureAwait(false);
        _certificate = X509Certificate2.CreateFromPem(
            JsonSerializer.Deserialize<string>(certResponse.GetPayloadAsJson()));
        
        ConfigureFromTwin(twin);

        ModuleClient = ModuleClient.CreateFromConnectionString(EdgeHubConnectionString);
        await ModuleClient.OpenAsync().ConfigureAwait(false);
        await CreateHttpClientWithAuthAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Get configuration from twin
    /// </summary>
    /// <param name="twin"></param>
    /// <returns></returns>
    private void ConfigureFromTwin(Twin twin)
    {
        // Set any missing info from the netcap twin
        OpcServerEndpointUrl = twin.GetProperty(
            nameof(OpcServerEndpointUrl), OpcServerEndpointUrl);
        PublisherRestApiEndpoint = twin.GetProperty(
            nameof(PublisherRestApiEndpoint), PublisherRestApiEndpoint);
        var captureDuration = twin.GetProperty(nameof(CaptureDuration));
        if (!string.IsNullOrWhiteSpace(captureDuration) &&
            TimeSpan.TryParse(captureDuration, out var duration))
        {
            CaptureDuration = duration;
        }
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
            HttpClient.BaseAddress = 
                await GetOpcPublisherRestEndpoint().ConfigureAwait(false);
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
    internal static readonly JsonSerializerOptions Indented 
        = new() { WriteIndented = true };
}
