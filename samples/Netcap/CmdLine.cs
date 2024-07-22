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
    [Verb("run", isDefault: true, HelpText = "Run netcap to capture diagnostics.")]
    public sealed class RunOptions
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

        [Option('a', nameof(PublisherRestApiKey), Required = false,
            HelpText = "The api key of the opc publisher." +
                "\nDefaults to value of environment variable 'PublisherRestApiKey'.")]
        public string? PublisherRestApiKey { get; set; } =
            Environment.GetEnvironmentVariable(nameof(PublisherRestApiKey));

        [Option('p', nameof(PublisherRestCertificate), Required = false,
            HelpText = "The tls certificate of the opc publisher." +
                "\nDefaults to value of environment variable 'PublisherRestCertificate'.")]
        public string? PublisherRestCertificate { get; set; }

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

        public RunOptions()
        {
            PublisherRestCertificate =
                Environment.GetEnvironmentVariable(nameof(PublisherRestCertificate));
        }

        /// <summary>
        /// Get configuration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public void ConfigureFromTwin(Twin twin)
        {
            // Set any missing info from the netcap twin
            OpcServerEndpointUrl = twin.GetProperty(
                nameof(OpcServerEndpointUrl), OpcServerEndpointUrl);
            PublisherRestApiEndpoint = twin.GetProperty(
                nameof(PublisherRestApiEndpoint), PublisherRestApiEndpoint);
            PublisherRestApiKey = twin.GetProperty(
                nameof(PublisherRestApiKey), PublisherRestApiKey);
            PublisherRestCertificate = twin.GetProperty(
                nameof(PublisherRestCertificate), PublisherRestCertificate);
            var captureDuration = twin.GetProperty(nameof(CaptureDuration));
            if (!string.IsNullOrWhiteSpace(captureDuration) &&
                TimeSpan.TryParse(captureDuration, out var duration))
            {
                CaptureDuration = duration;
            }
        }
    }

    [Verb("install", HelpText = "Install netcap into a publisher.")]
    public sealed class InstallOptions
    {
        [Option(nameof(TenantId), Required = false,
            HelpText = "The tenant to use to filter subscriptions down." +
            "\nDefault uses all tenants accessible.")]
        public string? TenantId { get; set; }

        [Option(nameof(SubscriptionId), Required = false,
            HelpText = "The subscription to use to install to." +
            "\nDefault uses all subscriptions accessible.")]
        public string? SubscriptionId { get; set; }
    }

    [Verb("uninstall", HelpText = "Uninstall netcap from one or all publishers.")]
    public sealed class UninstallOptions
    {
        [Option(nameof(TenantId), Required = false,
            HelpText = "The tenant to use to filter subscriptions down." +
            "\nDefault uses all tenants accessible.")]
        public string? TenantId { get; set; }

        [Option(nameof(SubscriptionId), Required = false,
            HelpText = "The subscription to use to install to." +
            "\nDefault uses all subscriptions accessible.")]
        public string? SubscriptionId { get; set; }
    }

    /// <summary>
    /// Http Client
    /// </summary>
    internal HttpClient HttpClient { get; private set; } = new HttpClient();

    /// <summary>
    /// Logger
    /// </summary>
    internal ILoggerFactory Logger { get; private set; } = null!;

    /// <summary>
    /// Whether to run
    /// </summary>
    internal RunOptions? Run => _run;

    /// <summary>
    /// Create
    /// </summary>
    public CmdLine()
    {
        _logger = UpdateLogger();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        HttpClient.Dispose();
        _certificate?.Dispose();
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
        var cmdLine = new CmdLine();
        await cmdLine.ParseAsync(args, ct).ConfigureAwait(false);
        return cmdLine;
    }

    /// <summary>
    /// Parse parameters
    /// </summary>
    /// <param name="args"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async ValueTask ParseAsync(string[] args, CancellationToken ct = default)
    {
        Parser.Default.ParseArguments<RunOptions, InstallOptions, UninstallOptions>(args)
            .WithParsed<RunOptions>(parsedParams => _run = parsedParams)
            .WithParsed<InstallOptions>(parsedParams => _install = parsedParams)
            .WithParsed<UninstallOptions>(parsedParams => _uninstall = parsedParams)
            .WithNotParsed(errors =>
            {
                errors.ToList().ForEach(Console.WriteLine);
                Environment.Exit(1);
            });

        _logger = UpdateLogger();
        var iothubConnectionString =
            Environment.GetEnvironmentVariable("IoTHubOwnerConnectionString") ??
            Environment.GetEnvironmentVariable("_HUB_CS");

        if (_install != null)
        {
            await InstallAsync(ct).ConfigureAwait(false);
        }
        else if (_uninstall != null)
        {
            await UninstallAsync(ct).ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(_run?.EdgeHubConnectionString) ||
            Environment.GetEnvironmentVariable("IOTEDGE_WORKLOADURI") != null)
        {
            await ConnectAsModuleAsync(ct).ConfigureAwait(false);
        }
        else if (!string.IsNullOrEmpty(iothubConnectionString))
        {
            // NOTE: This is for local testing against IoT Hub
            await ConnectAsIoTHubOwnerAsync(
                iothubConnectionString, ct).ConfigureAwait(false);
        }
        else if (string.IsNullOrWhiteSpace(_run?.PublisherRestApiKey) &&
            string.IsNullOrWhiteSpace(_run?.PublisherRestCertificate) &&
            string.IsNullOrWhiteSpace(_run?.PublisherRestApiEndpoint))
        {
            await InstallAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Install
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task InstallAsync(CancellationToken ct = default)
    {
        if (_install == null)
        {
            _install = new InstallOptions();
        }
        // Login to azure
        var armClient = new ArmClient(new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                TenantId = _install.TenantId
            }));

        _logger.LogInformation("Installing netcap module...");

        var gateway = new Gateway(armClient, _logger);
        try
        {
            // Get publishers
            var found = await gateway.SelectPublisherAsync(_install.SubscriptionId,
                false, ct).ConfigureAwait(false);
            if (!found)
            {
                return;
            }

            // Create storage account or update if it already exists in the rg
            await gateway.Storage.CreateOrUpdateAsync(ct).ConfigureAwait(false);
            // Create container registry or update and build netcap module
            await gateway.Netcap.CreateOrUpdateAsync(ct).ConfigureAwait(false);

            // Deploy the module using manifest to device with the chosen publisher
            await gateway.DeployNetcapModuleAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to install netcap module with error: {Error}",
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Uninstall
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task UninstallAsync(CancellationToken ct = default)
    {
        if (_uninstall == null)
        {
            _uninstall = new UninstallOptions();
        }
        // Login to azure
        var armClient = new ArmClient(new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                TenantId = _uninstall.TenantId
            }));

        _logger.LogInformation("Uninstalling netcap module...");

        var gateway = new Gateway(armClient, _logger);
        try
        {
            // Select netcap modules
            var found = await gateway.SelectPublisherAsync(_uninstall.SubscriptionId, true,
                ct).ConfigureAwait(false);
            if (!found)
            {
                return;
            }

            // Add guard here

            // Delete storage account or update if it already exists in the rg
            // await gateway.Storage.DeleteAsync(ct).ConfigureAwait(false);
            // Delete container registry
            // await gateway.NetcapException.DeleteAsync(ct).ConfigureAwait(false);

            // Deploy the module using manifest to device with the chosen publisher
            await gateway.RemoveNetcapModuleAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to uninstall netcap module with error: {Error}",
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Connect module to edge hub
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async ValueTask ConnectAsModuleAsync(CancellationToken ct = default)
    {
        if (_run == null)
        {
            _run = new RunOptions();
        }
        if (string.IsNullOrWhiteSpace(_run.EdgeHubConnectionString))
        {
            var moduleClient =
                await ModuleClient.CreateFromEnvironmentAsync().ConfigureAwait(false);
            await using (var _ = moduleClient.ConfigureAwait(false))
            {
                await ConnectAsModuleAsync(moduleClient, ct).ConfigureAwait(false);
            }
        }
        else
        {
            var moduleClient =
                ModuleClient.CreateFromConnectionString(_run.EdgeHubConnectionString);
            await using (var _ = moduleClient.ConfigureAwait(false))
            {
                await ConnectAsModuleAsync(moduleClient, ct).ConfigureAwait(false);
            }
        }

        async Task ConnectAsModuleAsync(ModuleClient moduleClient, CancellationToken ct)
        {
            // Call the "GetApiKey" and "GetServerCertificate" methods on the publisher module
            await moduleClient.OpenAsync(ct).ConfigureAwait(false);
            try
            {
                await moduleClient.UpdateReportedPropertiesAsync(new TwinCollection
                {
                    ["__type__"] = "OpcNetcap",
                    ["__version__"] = GetType().Assembly.GetVersion()
                }, ct).ConfigureAwait(false);

                var twin = await moduleClient.GetTwinAsync(ct).ConfigureAwait(false);

                var deviceId = twin.DeviceId ?? Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
                var moduleId = twin.ModuleId ?? Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
                _run.PublisherModuleId = twin.GetProperty(nameof(_run.PublisherModuleId), _run.PublisherModuleId);
                _run.PublisherDeviceId = deviceId; // Override as we must be in the same device
                Debug.Assert(_run.PublisherModuleId != null);
                Debug.Assert(_run.PublisherDeviceId != null);

                _run.ConfigureFromTwin(twin);

                _logger.LogInformation(
                    "Connecting to OPC Publisher Module {PublisherModuleId} on {PublisherDeviceId}...",
                    _run.PublisherModuleId, _run.PublisherDeviceId);

                if (_run.PublisherRestApiKey == null || _run.PublisherRestCertificate == null)
                {
                    if (_run.PublisherRestApiKey == null)
                    {
                        var apiKeyResponse = await moduleClient.InvokeMethodAsync(
                            _run.PublisherDeviceId, _run.PublisherModuleId,
                            new MethodRequest("GetApiKey"), ct).ConfigureAwait(false);
                        _run.PublisherRestApiKey =
                            JsonSerializer.Deserialize<string>(apiKeyResponse.Result);
                    }
                    if (_run.PublisherRestCertificate == null)
                    {
                        var certResponse = await moduleClient.InvokeMethodAsync(
                            _run.PublisherDeviceId, _run.PublisherModuleId,
                            new MethodRequest("GetServerCertificate"), ct).ConfigureAwait(false);
                        _run.PublisherRestCertificate =
                            JsonSerializer.Deserialize<string>(certResponse.Result);
                    }
                }
                await CreateHttpClientWithAuthAsync().ConfigureAwait(false);
            }
            finally
            {
                await moduleClient.CloseAsync(ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Connect with iot hub connections tring
    /// </summary>
    /// <param name="iothubConnectionString"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async ValueTask ConnectAsIoTHubOwnerAsync(
        string iothubConnectionString, CancellationToken ct = default)
    {
        string deviceId;
        var ncModuleId = "netcap";
        if (_run == null)
        {
            _run = new RunOptions();
        }
        if (!string.IsNullOrWhiteSpace(_run.EdgeHubConnectionString))
        {
            // Get device and module id from edge hub connection string provided
            var ehc = IotHubConnectionStringBuilder.Create(_run.EdgeHubConnectionString);
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
        try
        {
            await rm.AddDeviceAsync(new Microsoft.Azure.Devices.Device(deviceId), ct)
                .ConfigureAwait(false);
        }
        catch (DeviceAlreadyExistsException) { }
        try
        {
            await rm.AddModuleAsync(new Microsoft.Azure.Devices.Module(
                deviceId, ncModuleId), ct).ConfigureAwait(false);
        }
        catch (ModuleAlreadyExistsException) { }

        var twin = await rm.GetTwinAsync(deviceId, ncModuleId, ct).ConfigureAwait(false);
        twin = await rm.UpdateTwinAsync(deviceId, ncModuleId, new Twin
        {
            Properties = new TwinProperties
            {
                Reported = new TwinCollection
                {
                    ["__type__"] = "OpcNetcap",
                    ["__version__"] = GetType().Assembly.GetVersion()
                }
            }
        }, twin.ETag, ct).ConfigureAwait(false);

        // Get publisher id from twin if not configured
        _run.PublisherModuleId = twin.GetProperty(nameof(_run.PublisherModuleId), _run.PublisherModuleId);
        _run.PublisherDeviceId ??= deviceId;
        Debug.Assert(_run.PublisherModuleId != null);
        Debug.Assert(_run.PublisherDeviceId != null);

        _run.ConfigureFromTwin(twin);

        _logger.LogInformation("Connecting to OPC Publisher Module {PublisherModuleId} " +
            "on {PublisherDeviceId} via IoTHub...", _run.PublisherModuleId, _run.PublisherDeviceId);
        if (_run.PublisherRestApiKey == null || _run.PublisherRestCertificate == null)
        {
            using var serviceClient = Microsoft.Azure.Devices.ServiceClient
                .CreateFromConnectionString(iothubConnectionString);
            if (_run.PublisherRestApiKey == null)
            {
                var apiKeyResponse = await serviceClient.InvokeDeviceMethodAsync(
                    _run.PublisherDeviceId, _run.PublisherModuleId,
                    new Microsoft.Azure.Devices.CloudToDeviceMethod(
                        "GetApiKey"), ct).ConfigureAwait(false);
                _run.PublisherRestApiKey =
                    JsonSerializer.Deserialize<string>(apiKeyResponse.GetPayloadAsJson());
            }
            if (_run.PublisherRestCertificate == null)
            {
                var certResponse = await serviceClient.InvokeDeviceMethodAsync(
                    _run.PublisherDeviceId, _run.PublisherModuleId,
                    new Microsoft.Azure.Devices.CloudToDeviceMethod(
                        "GetServerCertificate"), ct).ConfigureAwait(false);
                _run.PublisherRestCertificate =
                    JsonSerializer.Deserialize<string>(certResponse.GetPayloadAsJson());
            }
        }
        await CreateHttpClientWithAuthAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Create client
    /// </summary>
    /// <returns></returns>
    private async ValueTask CreateHttpClientWithAuthAsync()
    {
        if (_run?.PublisherRestApiKey != null)
        {
            // Load the certificate of the publisher if not exist
            if (!string.IsNullOrWhiteSpace(_run?.PublisherRestCertificate)
                && _certificate == null)
            {
                try
                {
                    _certificate = X509Certificate2.CreateFromPem(
                        _run.PublisherRestCertificate.Trim());
                }
                catch
                {
                    var cert = Convert.FromBase64String(
                        _run.PublisherRestCertificate.Trim());
                    _certificate = new X509Certificate2(
                        cert!, _run.PublisherRestApiKey);
                }
            }

            HttpClient.Dispose();
#pragma warning disable CA2000 // Dispose objects before losing scope
            HttpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
                {
                    if (_certificate?.Thumbprint != cert?.Thumbprint)
                    {
                        _logger.LogWarning(
                            "Certificate thumbprint mismatch: {Expected} != {Actual}",
                            _certificate?.Thumbprint, cert?.Thumbprint);
                        return false;
                    }
                    return true;
                }
            });
#pragma warning restore CA2000 // Dispose objects before losing scope
            HttpClient.BaseAddress =
                await GetOpcPublisherRestEndpoint().ConfigureAwait(false);
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("ApiKey", _run?.PublisherRestApiKey);
        }

        /// <summary>
        /// Publisher Endpoint
        /// </summary>
        async ValueTask<Uri> GetOpcPublisherRestEndpoint()
        {
            if (_run?.PublisherRestApiEndpoint != null &&
                Uri.TryCreate(_run.PublisherRestApiEndpoint,
                UriKind.Absolute, out var u))
            {
                return u;
            }
            var host = _run?.PublisherModuleId;
            if (host != null)
            {
                // Poor man ping
                try
                {
                    var result = await Dns.GetHostEntryAsync(host).ConfigureAwait(false);
                    if (result.AddressList.Length == 0)
                        host = null;
                }
                catch { host = null; }
            }
            if (host == null)
            {
                host = "localhost";
            }
            var isLocal = host == null;
            var uri = new UriBuilder
            {
                Scheme = "https",
                Port = !isLocal ? 8081 : 443,
                Host = host
            };
            if (_run?.PublisherRestApiKey == null)
            {
                uri.Scheme = "http";
                uri.Port = !isLocal ? 8080 : 80;
            }
            return uri.Uri;
        }
    }

    /// <summary>
    /// Update logger
    /// </summary>
    private ILogger UpdateLogger()
    {
        Logger = LoggerFactory.Create(builder => builder
            .AddSimpleConsole(options => options.SingleLine = true));
        return Logger.CreateLogger("Netcap");
    }

    private ILogger _logger;
    private X509Certificate2? _certificate;
    private InstallOptions? _install;
    private UninstallOptions? _uninstall;
    private RunOptions? _run;
    internal static readonly JsonSerializerOptions Indented
        = new() { WriteIndented = true };
}
