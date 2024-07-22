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
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

/// <summary>
/// Netcap exception
/// </summary>
public class NetcapException : Exception
{
    public NetcapException(string message) : base(message)
    {
    }

    public NetcapException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Netcap main
/// </summary>
internal sealed class Main : IDisposable
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
        [Option('t', nameof(TenantId), Required = false,
            HelpText = "The tenant to use to filter subscriptions down." +
            "\nDefault uses all tenants accessible.")]
        public string? TenantId { get; set; }

        [Option('s', nameof(SubscriptionId), Required = false,
            HelpText = "The subscription to use to install to." +
            "\nDefault uses all subscriptions accessible.")]
        public string? SubscriptionId { get; set; }

        [Option('o', nameof(OutputPath), Required = false,
            HelpText = "The output path to capture to.")]
        public string? OutputPath { get; set; }
    }

    [Verb("uninstall", HelpText = "Uninstall netcap from one or all publishers.")]
    public sealed class UninstallOptions
    {
        [Option('t', nameof(TenantId), Required = false,
            HelpText = "The tenant to use to filter subscriptions down." +
            "\nDefault uses all tenants accessible.")]
        public string? TenantId { get; set; }

        [Option('s', nameof(SubscriptionId), Required = false,
            HelpText = "The subscription to use to install to." +
            "\nDefault uses all subscriptions accessible.")]
        public string? SubscriptionId { get; set; }
    }

    /// <summary>
    /// Create
    /// </summary>
    public Main()
    {
        _httpClient = new HttpClient();
        _loggerFactory = new LoggerFactory();
        _logger = UpdateLogger();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _loggerFactory.Dispose();
        _httpClient.Dispose();
        _certificate?.Dispose();
    }

    /// <summary>
    /// Parse parameters
    /// </summary>
    /// <param name="args"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async ValueTask<Main> RunAsync(string[] args,
        CancellationToken ct = default)
    {
        var cmd = new Main();
        await cmd.ParseAsync(args, ct).ConfigureAwait(false);
        return cmd;
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

            await RunAsync(ct).ConfigureAwait(false);
        }
        else if (!string.IsNullOrEmpty(iothubConnectionString))
        {
            // NOTE: This is for local testing against IoT Hub
            await ConnectAsIoTHubOwnerAsync(
                iothubConnectionString, ct).ConfigureAwait(false);

            await RunAsync(ct).ConfigureAwait(false);
        }
        else if (string.IsNullOrWhiteSpace(_run?.PublisherRestApiKey) &&
            string.IsNullOrWhiteSpace(_run?.PublisherRestCertificate) &&
            string.IsNullOrWhiteSpace(_run?.PublisherRestApiEndpoint))
        {
            await InstallAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Run netcap
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task RunAsync(CancellationToken ct = default)
    {
        if (_run == null)
        {
            _run = new RunOptions();
        }
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (!Extensions.IsRunningInContainer())
        {
            while (Console.KeyAvailable) { Console.ReadKey(); }
            _ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); }, ct);
            Console.WriteLine("Press any key to exit");
            Console.WriteLine();
        }

        try
        {
            // Connect to publisher
            var publisher = new Publisher(_loggerFactory.CreateLogger("Publisher"), _httpClient,
                _run.OpcServerEndpointUrl);

            Storage? uploader = null;
            if (!string.IsNullOrEmpty(_run.StorageConnectionString))
            {
                _logger.LogInformation("Uploading to storage of publisher module {DeviceId}/{ModuleId}...",
                    _run.PublisherDeviceId, _run.PublisherModuleId);
                // TODO: move to seperate task
                uploader = new Storage(_run.PublisherDeviceId ?? "unknown", _run.PublisherModuleId,
                    _run.StorageConnectionString, _loggerFactory.CreateLogger("Upload"));
            }

            for (var i = 0; !cts.IsCancellationRequested; i++)
            {
                // Get endpoint urls and addresses to monitor if not set
                if (!await publisher.TryUpdateEndpointsAsync(cts.Token).ConfigureAwait(false))
                {
                    _logger.LogInformation("waiting .....");
                    await Task.Delay(TimeSpan.FromMinutes(1), cts.Token).ConfigureAwait(false);
                    continue;
                }

                // Capture traffic for duration
                using var timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                if (uploader != null || _run.CaptureDuration != null)
                {
                    var duration = _run.CaptureDuration ?? TimeSpan.FromMinutes(10);
                    _logger.LogInformation("Capturing for {Duration}", duration);
                    timeoutToken.CancelAfter(duration);
                }
                var folder = Path.Combine(Path.GetTempPath(), "capture" + i);

                var bundle = new Bundle(_loggerFactory.CreateLogger("Capture"), folder);
                using (bundle.CaptureNetworkTraces(publisher, i))
                {
                    while (!timeoutToken.IsCancellationRequested)
                    {
                        // Watch session diagnostics while we capture
                        try
                        {
                            _logger.LogInformation("Monitoring diagnostics at {Url}...", _httpClient.BaseAddress);
                            await foreach (var diagnostic in _httpClient.GetFromJsonAsAsyncEnumerable<JsonElement>(
                                "v2/diagnostics/connections/watch",
                                    cancellationToken: timeoutToken.Token).ConfigureAwait(false))
                            {
                                await bundle.AddSessionKeysFromDiagnosticsAsync(
                                    diagnostic, publisher.Endpoints).ConfigureAwait(false);
                            }
                            _logger.LogInformation("Restart monitoring diagnostics...");
                        }
                        catch (OperationCanceledException) { } // Done
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error monitoring diagnostics - restarting...");
                        }
                    }
                }

                // TODO: move to seperate task
                if (uploader != null)
                {
                    await uploader.UploadAsync(bundle, cts.Token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run.");
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

            if (!string.IsNullOrWhiteSpace(_install.OutputPath))
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (!Extensions.IsRunningInContainer())
                {
                    while (Console.KeyAvailable) { Console.ReadKey(); }
                    _ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); }, ct);
                    Console.WriteLine("Press any key to exit");
                    Console.WriteLine();
                }
                try
                {
                    // Get the logs from the module, when cancelled undeploy
                    var downloader = gateway.GetStorage();
                    await downloader.DownloadAsync(_install.OutputPath,
                        cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await gateway.RemoveNetcapModuleAsync(ct).ConfigureAwait(false);
                }
            }
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

            _httpClient.Dispose();
#pragma warning disable CA2000 // Dispose objects before losing scope
            _httpClient = new HttpClient(new HttpClientHandler
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
            _httpClient.BaseAddress =
                await GetOpcPublisherRestEndpoint().ConfigureAwait(false);
            _httpClient.DefaultRequestHeaders.Authorization =
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
        _loggerFactory.Dispose();
        _loggerFactory = LoggerFactory.Create(builder => builder
            .AddSimpleConsole(options => options.SingleLine = true));
        return _loggerFactory.CreateLogger("Netcap");
    }


    private HttpClient _httpClient;
    private ILoggerFactory _loggerFactory = null!;
    private ILogger _logger;
    private X509Certificate2? _certificate;
    private InstallOptions? _install;
    private UninstallOptions? _uninstall;
    private RunOptions? _run;
    internal static readonly JsonSerializerOptions Indented
        = new() { WriteIndented = true };
}
