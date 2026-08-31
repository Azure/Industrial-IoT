// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
#if NET8_0_OR_GREATER
    using System.IO.Pipes;
    using System.Net.Sockets;
#endif
    using Azure.IIoT.OpcUa.Core.IoTEdge.Services;
    using IoTHubby.Edge.Workload;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// IoT Edge workload API implementation.
    /// </summary>
    public sealed class IoTEdgeWorkloadApi : IIoTEdgeWorkloadApi, IDisposable
    {
        /// <inheritdoc/>
        public bool IsAvailable => _client != null;

        /// <summary>
        /// Create workload api from environment.
        /// </summary>
        public IoTEdgeWorkloadApi()
            : this(Environment.GetEnvironmentVariable("IOTEDGE_WORKLOADURI"),
                Environment.GetEnvironmentVariable("IOTEDGE_MODULEGENERATIONID"),
                Environment.GetEnvironmentVariable("IOTEDGE_MODULEID"),
                Environment.GetEnvironmentVariable("IOTEDGE_APIVERSION"))
        {
        }

        /// <summary>
        /// Create workload api.
        /// </summary>
        /// <param name="workloadUri"></param>
        /// <param name="generationId"></param>
        /// <param name="moduleId"></param>
        /// <param name="apiVersion"></param>
        /// <param name="handler"></param>
        internal IoTEdgeWorkloadApi(string? workloadUri, string? generationId,
            string? moduleId, string? apiVersion, HttpMessageHandler? handler = null)
        {
            if (workloadUri == null || generationId == null || moduleId == null)
            {
                return;
            }

            apiVersion ??= "2019-01-30";
            var uri = WorkloadApiHttpClient.CreateWorkloadUri(workloadUri);
            _client = new WorkloadApiHttpClient(uri, apiVersion,
                moduleId, generationId, handler);
            _workloadClient = new WorkloadApiClient(uri, apiVersion, handler);
            _moduleId = moduleId;
            _generationId = generationId;
        }

        /// <inheritdoc/>
        public async ValueTask<ReadOnlyMemory<byte>> EncryptAsync(
            string initializationVector, ReadOnlyMemory<byte> plaintext,
            CancellationToken ct = default)
        {
            var (client, moduleId, generationId) = GetWorkloadClient();
            return await client.EncryptAsync(moduleId, generationId,
                initializationVector, plaintext.ToArray(), ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask<ReadOnlyMemory<byte>> DecryptAsync(
            string initializationVector, ReadOnlyMemory<byte> ciphertext,
            CancellationToken ct = default)
        {
            var (client, moduleId, generationId) = GetWorkloadClient();
            return await client.DecryptAsync(moduleId, generationId,
                initializationVector, ciphertext.ToArray(), ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask<ReadOnlyMemory<byte>> SignAsync(
            ReadOnlyMemory<byte> data, string? keyId = null, string? algo = null,
            CancellationToken ct = default)
        {
            var client = GetClient();
            var dataBase64 = Convert.ToBase64String(data.Span);
            return await client.SignAsync(keyId ?? "primary", algo ?? "HMACSHA256",
                dataBase64, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask<X509Certificate2Collection> CreateServerCertificateAsync(
            string commonName, DateTime expiration, CancellationToken ct = default)
        {
            var client = GetClient();
            var certificate = await client.CreateServerCertificateAsync(commonName,
                expiration, ct).ConfigureAwait(false);
#pragma warning disable CA2000 // Dispose objects before losing scope
            return new X509Certificate2Collection(
                X509Certificate2.CreateFromPem(certificate.Certificate,
                    certificate.PrivateKey?.Bytes));
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        /// <inheritdoc/>
        public async ValueTask<X509Certificate2Collection> GetTrustBundleAsync(
            CancellationToken ct = default)
        {
            var pem = await GetClient().GetTrustBundleAsync(ct).ConfigureAwait(false);
            return ImportPem(pem);
        }

        /// <inheritdoc/>
        public async ValueTask<X509Certificate2Collection> GetManifestTrustBundleAsync(
            CancellationToken ct = default)
        {
            var pem = await GetClient().GetManifestTrustBundleAsync(ct).ConfigureAwait(false);
            return ImportPem(pem);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _client?.Dispose();
            _workloadClient?.Dispose();
        }

        private WorkloadApiHttpClient GetClient()
        {
            if (_client == null)
            {
                throw new NotSupportedException("Not running in IoT Edge.");
            }
            return _client;
        }

        private (WorkloadApiClient Client, string ModuleId, string GenerationId)
            GetWorkloadClient()
        {
            if (_workloadClient == null || _moduleId == null || _generationId == null)
            {
                throw new NotSupportedException("Not running in IoT Edge.");
            }
            return (_workloadClient, _moduleId, _generationId);
        }

        private static X509Certificate2Collection ImportPem(string pem)
        {
            var collection = new X509Certificate2Collection();
            if (!string.IsNullOrEmpty(pem))
            {
                collection.ImportFromPem(pem);
            }
            return collection;
        }

        private readonly WorkloadApiHttpClient? _client;
        private readonly WorkloadApiClient? _workloadClient;
        private readonly string? _moduleId;
        private readonly string? _generationId;
    }

    /// <summary>
    /// Minimal workload daemon HTTP client.
    /// </summary>
    internal sealed class WorkloadApiHttpClient : IDisposable
    {
        /// <summary>
        /// Create workload client.
        /// </summary>
        public WorkloadApiHttpClient(string workloadUriText, string apiVersion,
            string moduleId, string generationId, HttpMessageHandler? handler = null)
            : this(CreateWorkloadUri(workloadUriText), apiVersion,
                moduleId, generationId, handler)
        {
        }

        /// <summary>
        /// Create workload client.
        /// </summary>
        public WorkloadApiHttpClient(Uri workloadUri, string apiVersion,
            string moduleId, string generationId, HttpMessageHandler? handler = null)
        {
            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentException("The workload API version is required.",
                    nameof(apiVersion));
            }
            if (string.IsNullOrEmpty(moduleId))
            {
                throw new ArgumentException("The module id is required.", nameof(moduleId));
            }
            if (string.IsNullOrEmpty(generationId))
            {
                throw new ArgumentException("The generation id is required.", nameof(generationId));
            }

            _apiVersion = apiVersion;
            _moduleId = moduleId;
            _generationId = generationId;
            _requestBaseUri = CreateRequestBaseUri(workloadUri);
            _client = new HttpClient(handler ?? CreateTransportHandler(workloadUri),
                disposeHandler: true);
        }

        /// <summary>
        /// Sign using the workload daemon's legacy payload shape.
        /// </summary>
        public async Task<byte[]> SignAsync(string keyId, string algo,
            string dataBase64, CancellationToken ct)
        {
            var request = new WorkloadSignRequest(keyId, algo,
                Encoding.UTF8.GetBytes(dataBase64));
            var response = await PostModuleAsync("sign", request,
                IoTEdgeWorkloadJsonContext.Default.WorkloadSignRequest,
                IoTEdgeWorkloadJsonContext.Default.WorkloadSignResponse,
                ct).ConfigureAwait(false);
            return response.Digest ?? [];
        }

        /// <summary>
        /// Create server certificate.
        /// </summary>
        public async Task<WorkloadCertificateResponse> CreateServerCertificateAsync(
            string commonName, DateTime expiration, CancellationToken ct)
        {
            var request = new WorkloadServerCertificateRequest(commonName, expiration);
            return await PostModuleAsync("certificate/server", request,
                IoTEdgeWorkloadJsonContext.Default.WorkloadServerCertificateRequest,
                IoTEdgeWorkloadJsonContext.Default.WorkloadCertificateResponse,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get trust bundle.
        /// </summary>
        public async Task<string> GetTrustBundleAsync(CancellationToken ct)
        {
            var response = await GetAsync("trust-bundle",
                IoTEdgeWorkloadJsonContext.Default.WorkloadTrustBundleResponse,
                ct).ConfigureAwait(false);
            return response.Certificate ?? string.Empty;
        }

        /// <summary>
        /// Get manifest trust bundle.
        /// </summary>
        public async Task<string> GetManifestTrustBundleAsync(CancellationToken ct)
        {
            var response = await GetAsync("manifest-trust-bundle",
                IoTEdgeWorkloadJsonContext.Default.WorkloadTrustBundleResponse,
                ct).ConfigureAwait(false);
            return response.Certificate ?? string.Empty;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _client.Dispose();
        }

        internal Uri CreateRequestUriForTest(string relativePath)
        {
            return CreateRequestUri(relativePath);
        }

        private async Task<TResponse> PostModuleAsync<TRequest, TResponse>(
            string operation, TRequest requestBody,
            System.Text.Json.Serialization.Metadata.JsonTypeInfo<TRequest> requestInfo,
            System.Text.Json.Serialization.Metadata.JsonTypeInfo<TResponse> responseInfo,
            CancellationToken ct)
        {
            var path = $"modules/{Uri.EscapeDataString(_moduleId)}/genid/" +
                $"{Uri.EscapeDataString(_generationId)}/{operation}";
            var requestJson = JsonSerializer.Serialize(requestBody, requestInfo);
            using var request = new HttpRequestMessage(HttpMethod.Post,
                CreateRequestUri(path))
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var stream = await ReadAsStreamAsync(response.Content, ct).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync(stream, responseInfo, ct)
                    .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"The workload {operation} response was empty.");
        }

        private async Task<TResponse> GetAsync<TResponse>(string operation,
            System.Text.Json.Serialization.Metadata.JsonTypeInfo<TResponse> responseInfo,
            CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                CreateRequestUri(operation));
            using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var stream = await ReadAsStreamAsync(response.Content, ct).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync(stream, responseInfo, ct)
                    .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"The workload {operation} response was empty.");
        }

        private Uri CreateRequestUri(string relativePath)
        {
            return new(_requestBaseUri,
                $"{relativePath}?api-version={Uri.EscapeDataString(_apiVersion)}");
        }

        internal static Uri CreateWorkloadUri(string workloadUriText)
        {
            if (workloadUriText.Length > 0 && workloadUriText[0] == '/')
            {
                return new Uri("unix://" + workloadUriText);
            }
            return Uri.TryCreate(workloadUriText, UriKind.Absolute, out var workloadUri) ?
                workloadUri :
                throw new InvalidOperationException("IOTEDGE_WORKLOADURI is not a valid URI.");
        }

        private static Uri CreateRequestBaseUri(Uri workloadUri)
        {
            if (IsTcpUri(workloadUri))
            {
                var pathUri = new Uri(workloadUri.GetLeftPart(UriPartial.Path));
                return pathUri.AbsoluteUri[^1] == '/' ?
                    pathUri : new Uri(pathUri.AbsoluteUri + "/");
            }
            return new Uri("http://localhost/");
        }

        private static bool IsTcpUri(Uri uri)
        {
            return uri.IsAbsoluteUri &&
                (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsNamedPipeUri(Uri uri)
        {
            return uri.IsAbsoluteUri &&
                uri.Scheme.Equals("npipe", StringComparison.OrdinalIgnoreCase);
        }

#if NET8_0_OR_GREATER
        [ExcludeFromCodeCoverage(Justification =
            "Builds a real UDS/named-pipe HTTP transport; exercised only against a live edge runtime.")]
        private static HttpMessageHandler CreateTransportHandler(Uri workloadUri)
        {
            if (IsTcpUri(workloadUri))
            {
                return new HttpClientHandler();
            }
            if (!IsNamedPipeUri(workloadUri) && !IsUnixSocketUri(workloadUri))
            {
                throw new NotSupportedException(
                    $"The workload URI scheme '{workloadUri.Scheme}' is not supported.");
            }
            return new SocketsHttpHandler
            {
                ConnectCallback = (_, cancellationToken) =>
                    ConnectAsync(workloadUri, cancellationToken)
            };
        }

        [ExcludeFromCodeCoverage(Justification =
            "Opens a real UDS/named-pipe connection; exercised only against a live edge runtime.")]
        private static async ValueTask<System.IO.Stream> ConnectAsync(Uri workloadUri,
            CancellationToken ct)
        {
            if (IsNamedPipeUri(workloadUri))
            {
                return await ConnectNamedPipeAsync(workloadUri, ct).ConfigureAwait(false);
            }
            return await ConnectUnixSocketAsync(GetUnixSocketPath(workloadUri), ct)
                .ConfigureAwait(false);
        }

        private static async ValueTask<System.IO.Stream> ConnectUnixSocketAsync(
            string socketPath, CancellationToken ct)
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream,
                ProtocolType.Unspecified);
            try
            {
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), ct)
                    .ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private static async ValueTask<System.IO.Stream> ConnectNamedPipeAsync(
            Uri workloadUri, CancellationToken ct)
        {
            var (serverName, pipeName) = GetNamedPipeParts(workloadUri);
            var stream = new NamedPipeClientStream(serverName, pipeName,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await stream.ConnectAsync(ct).ConfigureAwait(false);
                return stream;
            }
            catch
            {
                await stream.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        private static string GetUnixSocketPath(Uri uri)
        {
            if (!uri.IsAbsoluteUri)
            {
                return uri.OriginalString;
            }
            var path = Uri.UnescapeDataString(uri.AbsolutePath);
            var host = Uri.UnescapeDataString(uri.Host);
            if (string.IsNullOrEmpty(path) || path == "/")
            {
                return host;
            }
            if (!string.IsNullOrEmpty(host) && host[0] == '/')
            {
                return host + path;
            }
            return path;
        }

        private static (string ServerName, string PipeName) GetNamedPipeParts(Uri uri)
        {
            var serverName = string.IsNullOrEmpty(uri.Host) ? "." : uri.Host;
            if (serverName.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                serverName = ".";
            }
            var pipeName = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
            const string pipePrefix = "pipe/";
            if (pipeName.StartsWith(pipePrefix, StringComparison.OrdinalIgnoreCase))
            {
                pipeName = pipeName[pipePrefix.Length..];
            }
            pipeName = pipeName.Replace('/', '\\');
            if (string.IsNullOrEmpty(pipeName))
            {
                throw new ArgumentException("The named pipe workload URI does not contain a pipe name.",
                    nameof(uri));
            }
            return (serverName, pipeName);
        }

        private static bool IsUnixSocketUri(Uri uri)
        {
            return uri.IsAbsoluteUri &&
                uri.Scheme.Equals("unix", StringComparison.OrdinalIgnoreCase) ||
                uri.OriginalString.Length > 0 && uri.OriginalString[0] == '/';
        }
#else
        private static HttpMessageHandler CreateTransportHandler(Uri workloadUri)
        {
            _ = workloadUri;
            throw new PlatformNotSupportedException(
                "The Edge Workload API requires .NET 8 or later.");
        }
#endif

        private static async Task<System.IO.Stream> ReadAsStreamAsync(
            HttpContent content, CancellationToken ct)
        {
#if NET8_0_OR_GREATER
            return await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
#else
            _ = ct;
            return await content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
        }

        private readonly string _apiVersion;
        private readonly string _moduleId;
        private readonly string _generationId;
        private readonly HttpClient _client;
        private readonly Uri _requestBaseUri;
    }

    internal sealed record WorkloadSignRequest(string KeyId, string Algo, byte[] Data);

    internal sealed record WorkloadSignResponse(byte[]? Digest);

    internal sealed record WorkloadServerCertificateRequest(string CommonName,
        DateTime Expiration);

    internal sealed record WorkloadPrivateKey(string Type, string? Ref, string? Bytes);

    internal sealed record WorkloadCertificateResponse(WorkloadPrivateKey? PrivateKey,
        string Certificate, DateTime Expiration);

    internal sealed record WorkloadTrustBundleResponse(string? Certificate);

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(WorkloadSignRequest))]
    [JsonSerializable(typeof(WorkloadSignResponse))]
    [JsonSerializable(typeof(WorkloadServerCertificateRequest))]
    [JsonSerializable(typeof(WorkloadPrivateKey))]
    [JsonSerializable(typeof(WorkloadCertificateResponse))]
    [JsonSerializable(typeof(WorkloadTrustBundleResponse))]
    internal sealed partial class IoTEdgeWorkloadJsonContext : JsonSerializerContext
    {
    }
}
