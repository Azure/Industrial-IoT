// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Azure.Identity;
using Azure.ResourceManager;
using CommandLine;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.ComponentModel;
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

    [Option('i', nameof(Install), Required = false,
        HelpText = "Whether to install the module." +
        "\nDefault if nothing else is provided.")]
    public bool Install { get; set; }

    [Option(nameof(TenantId), Required = false,
        HelpText = "The tenant to use to filter subscriptions down." +
        "\nDefault uses all tenants accessible.")]
    public string? TenantId { get; set; }

    [Option(nameof(SubscriptionId), Required = false,
        HelpText = "The subscription to use to install to." +
        "\nDefault uses all subscriptions accessible.")]
    public string? SubscriptionId { get; set; }

    [Option('u', nameof(Uninstall), Required = false,
        HelpText = "Whether to uninstall the module and remove all cloud resources." +
        "\nNot valid with install option.")]
    public bool Uninstall { get; set; }

    /// <summary>
    /// Http Client
    /// </summary>
    internal HttpClient HttpClient { get; private set; } = new HttpClient();

    /// <summary>
    /// Logger
    /// </summary>
    internal ILoggerFactory Logger { get; private set; } = new LoggerFactory();

    /// <inheritdoc/>
    public void Dispose()
    {
        HttpClient.Dispose();
        _certificate?.Dispose();
        _apiKey = null;
    }

    /// <summary>
    /// Parse parameters
    /// </summary>
    /// <param name="args"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async ValueTask<CmdLine> CreateAsync(string[] args,
        CancellationToken ct = default)
    {
        CmdLine? cmdLine = null;
        ParserResult<CmdLine> result = Parser.Default.ParseArguments<CmdLine>(args)
            .WithParsed(parsedParams => cmdLine = parsedParams)
            .WithNotParsed(errors =>
            {
                errors.ToList().ForEach(Console.WriteLine);
                Environment.Exit(1);
            });
        Debug.Assert(cmdLine != null);

        cmdLine.Logger = LoggerFactory.Create(builder => builder
            .AddSimpleConsole(options => options.SingleLine = true));

        if (cmdLine.Install)
        {
            await cmdLine.InstallAsync(ct).ConfigureAwait(false);
        }
        else if (cmdLine.Uninstall)
        {
            await cmdLine.UninstallAsync(ct).ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(cmdLine.EdgeHubConnectionString) ||
            Environment.GetEnvironmentVariable("IOTEDGE_WORKLOADURI") != null)
        {
            await cmdLine.ConnectAsModuleAsync().ConfigureAwait(false);
        }
#if NO_IOTEDGE
        else if (_apiKey != null && _certificate != null))
        {
            // Use the provided API key and certificate
            // Support to run without iot edge and hub
        }
#endif
        else
        {
            var iothubConnectionString =
                Environment.GetEnvironmentVariable("IoTHubOwnerConnectionString") ??
                Environment.GetEnvironmentVariable("_HUB_CS");
            if (string.IsNullOrEmpty(iothubConnectionString))
            {
                cmdLine.Install = true;
                await cmdLine.InstallAsync(ct).ConfigureAwait(false);
            }
            else
            {
                // NOTE: For testing locally only
                await cmdLine.ConnectAsIoTHubOwnerAsync(
                    iothubConnectionString).ConfigureAwait(false);
            }
        }
        return cmdLine;
    }

    /// <summary>
    /// Install
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task InstallAsync(CancellationToken ct = default)
    {
        var logger = Logger.CreateLogger("Netcap");

        // Login to azure
        var armClient = new ArmClient(new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                TenantId = TenantId
            }));

        logger.LogInformation("Installing netcap module...");

        // Get publisher
        var gateway = new Gateway(armClient, logger);
        try
        {
            await gateway.SelectPublisherAsync(SubscriptionId, ct).ConfigureAwait(false);

            // Create storage account or update if it already exists in the rg
            await gateway.Storage.CreateOrUpdateAsync(ct).ConfigureAwait(false);
            // Create container registry or update and build netcap module
            await gateway.Netcap.CreateOrUpdateAsync(ct).ConfigureAwait(false);

            // Deploy the module using manifest to device with the chosen publisher
            await gateway.DeployNetcapModuleAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to install netcap module with error: {Error}",
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Uninstall
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task UninstallAsync(CancellationToken ct = default)
    {
        var logger = Logger.CreateLogger("Netcap");

        // Login to azure
        var armClient = new ArmClient(new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                TenantId = TenantId
            }));

        logger.LogInformation("Uninstalling netcap module...");

        // Select publisher
        var gateway = new Gateway(armClient, logger);
        try
        {
            await gateway.SelectPublisherAsync(SubscriptionId, ct).ConfigureAwait(false);

            // Create storage account or update if it already exists in the rg
            await gateway.Storage.DeleteAsync(ct).ConfigureAwait(false);
            // Create container registry or update and build netcap module
            await gateway.Netcap.DeleteAsync(ct).ConfigureAwait(false);

            // Deploy the module using manifest to device with the chosen publisher
            await gateway.RemoveNetcapModuleAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to uninstall netcap module with error: {Error}",
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Connect module to edge hub
    /// </summary>
    /// <returns></returns>
    private async ValueTask ConnectAsModuleAsync()
    {
        Console.WriteLine("Connecting to OPC Publisher Module " +
            $"{PublisherModuleId} on {PublisherDeviceId}...");

        if (string.IsNullOrWhiteSpace(EdgeHubConnectionString))
        {
            var moduleClient =
                await ModuleClient.CreateFromEnvironmentAsync().ConfigureAwait(false);
            await using (var _ = moduleClient.ConfigureAwait(false))
            {
                await ConnectAsModuleAsync(moduleClient).ConfigureAwait(false);
            }
        }
        else {
            var moduleClient =
                ModuleClient.CreateFromConnectionString(EdgeHubConnectionString);
            await using (var _ = moduleClient.ConfigureAwait(false))
            {
                await ConnectAsModuleAsync(moduleClient).ConfigureAwait(false);
            }
        }

        async Task ConnectAsModuleAsync(ModuleClient moduleClient)
        {
            // Call the "GetApiKey" and "GetServerCertificate" methods on the publisher module
            await moduleClient.OpenAsync().ConfigureAwait(false);
            try
            {
                var twin = await moduleClient.GetTwinAsync().ConfigureAwait(false);

                var deviceId = twin.DeviceId;
                PublisherModuleId = twin.GetProperty(nameof(PublisherModuleId),
                    PublisherModuleId);
                PublisherDeviceId = deviceId; // Override as we must be in the same device
                Debug.Assert(PublisherModuleId != null);
                Debug.Assert(PublisherDeviceId != null);

                var apiKeyResponse = await moduleClient.InvokeMethodAsync(
                    PublisherDeviceId, PublisherModuleId,
                    new MethodRequest("GetApiKey")).ConfigureAwait(false);
                _apiKey =
                    JsonSerializer.Deserialize<string>(apiKeyResponse.Result);
                var certResponse = await moduleClient.InvokeMethodAsync(
                    PublisherDeviceId, PublisherModuleId,
                    new MethodRequest("GetServerCertificate")).ConfigureAwait(false);
                _certificate = X509Certificate2.CreateFromPem(
                    JsonSerializer.Deserialize<string>(certResponse.Result));

                ConfigureFromTwin(twin);
                await CreateHttpClientWithAuthAsync().ConfigureAwait(false);
            }
            finally
            {
                await moduleClient.CloseAsync().ConfigureAwait(false);
            }
        }
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
#pragma warning disable CA1308 // Normalize strings to uppercase
            deviceId = Dns.GetHostName().ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
        }
        using var rm = Microsoft.Azure.Devices.RegistryManager
            .CreateFromConnectionString(iothubConnectionString);
        // Create module if not exist
        try { await rm.AddDeviceAsync(
            new Microsoft.Azure.Devices.Device(deviceId)).ConfigureAwait(false); }
        catch (DeviceAlreadyExistsException) { }
        try { await rm.AddModuleAsync(new Microsoft.Azure.Devices.Module(
                deviceId, ncModuleId)).ConfigureAwait(false); }
        catch (ModuleAlreadyExistsException) { }

        var twin = await rm.GetTwinAsync(deviceId, ncModuleId).ConfigureAwait(false);
        PublisherModuleId = twin.GetProperty(nameof(PublisherModuleId), PublisherModuleId);
        PublisherDeviceId ??= deviceId;
        Debug.Assert(PublisherModuleId != null);
        Debug.Assert(PublisherDeviceId != null);

        Console.WriteLine("Connecting to OPC Publisher Module " +
            $"{PublisherModuleId} on {PublisherDeviceId} via IoTHub...");
        using var serviceClient = Microsoft.Azure.Devices.ServiceClient
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
#pragma warning disable CA2000 // Dispose objects before losing scope
            HttpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, cert, _, _)
                    => cert != null && _certificate?.Thumbprint == cert?.Thumbprint
            });
#pragma warning restore CA2000 // Dispose objects before losing scope
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
