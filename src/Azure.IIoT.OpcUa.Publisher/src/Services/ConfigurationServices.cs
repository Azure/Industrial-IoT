// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Furly.Exceptions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration services uses the address space and services of a connected server to
    /// configure the publisher. The configuration services allow interactive expansion of
    /// published nodes.
    /// </summary>
    public sealed class ConfigurationServices : IConfigurationServices,
        IAssetConfiguration<Stream>, IAssetConfiguration<byte[]>, IDisposable
    {
        /// <summary>
        /// Create configuration services
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="client"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="timeProvider"></param>
        public ConfigurationServices(IPublishedNodesServices configuration,
            IOpcUaClientManager<ConnectionModel> client, IOptions<PublisherOptions> options,
            ILogger<ConfigurationServices> logger, TimeProvider? timeProvider = null)
        {
            _configuration = configuration;
            _client = client;
            _options = options;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> ExpandAsync(
            PublishedNodesEntryModel entry, PublishedNodeExpansionModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.OpcNodes);
            ValidateNodes(entry.OpcNodes);

            var browser = new ConfigurationBrowser(entry, request, _options, null,
                _logger, _timeProvider);
            return _client.ExecuteAsync(entry.ToConnectionModel(), browser, request.Header, ct);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAsync(
            PublishedNodesEntryModel entry, PublishedNodeExpansionModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.OpcNodes);
            ValidateNodes(entry.OpcNodes);

            var browser = new ConfigurationBrowser(entry, request, _options, _configuration,
                _logger, _timeProvider);
            return _client.ExecuteAsync(entry.ToConnectionModel(), browser, request.Header, ct);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAssetAsync(
            PublishedNodeCreateAssetRequestModel<byte[]> request, CancellationToken ct)
        {
            var stream = new MemoryStream(request.Configuration);
            await using var _ = stream.ConfigureAwait(false);
            var requestWithStream = new PublishedNodeCreateAssetRequestModel<Stream>
            {
                Configuration = stream,
                Entry = request.Entry,
                Header = request.Header,
                WaitTime = request.WaitTime
            };
            return await CreateOrUpdateAssetAsync(requestWithStream,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> GetAllAssetsAsync(
            PublishedNodesEntryModel entry, RequestHeaderModel? header, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return CoreAsync(ct);

            async IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> CoreAsync(
                [EnumeratorCancellation] CancellationToken ct)
            {
                // Expand the wot node one level and expand each level
                var expansion = new PublishedNodeExpansionModel
                {
                    ExcludeRootIfInstanceNode = true,
                    MaxDepth = 1, // There is one asset level underneath the root connection node
                    Header = header
                };
                var browser = new ConfigurationBrowser(entry with
                {
                    // Named object in the address space of the server.
                    OpcNodes = [new() { Id = AssetsEx.Root }]
                }, expansion, _options, null, _logger, _timeProvider, true);

                // Browse and swap the data set writer id and data set name to make an asset entry.
                await foreach (var result in _client.ExecuteAsync(entry.ToConnectionModel(),
                    browser, header, ct).ConfigureAwait(false))
                {
                    yield return result with
                    {
                        Result = result.Result == null ? null : result.Result with
                        {
                            DataSetWriterGroup = entry.DataSetWriterGroup,
                            DataSetWriterId = result.Result.DataSetName,
                            DataSetName = result.Result.DataSetWriterId?.TrimStart('/') // Asset name
                        }
                    };
                }
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAssetAsync(
            PublishedNodeCreateAssetRequestModel<Stream> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Configuration);
            ArgumentNullException.ThrowIfNull(request.Entry);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetName); // Asset name

            using var trace = _activitySource.StartActivity("CreateOrUpdateAsset");

            var entry = request.Entry;
            var connection = entry.ToConnectionModel();
            ServiceResultModel? errorInfo;
            (entry, errorInfo) = await _client.ExecuteAsync(connection, async context =>
            {
                // 1) Create asset and get asset file object
                var (nodeId, errorInfo) = await context.Session.CreateAssetAsync(
                    request.Header.ToRequestHeader(_timeProvider),
                    request.Entry.DataSetName, context.Ct).ConfigureAwait(false); // Asset name
                if (errorInfo != null || nodeId is null || NodeId.IsNull(nodeId))
                {
                    // TOOD errorInfo?.StatusCode ==
                    //  Opc.Ua.StatusCodes.BadBrowseNameDuplicated
                    errorInfo ??= new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadNodeIdInvalid,
                        ErrorMessage = "Failed to create asset."
                    };
                    return (entry with { DataSetWriterId = null }, errorInfo);
                }
                var assetId = nodeId.AsString(context.Session.MessageContext,
                    NamespaceFormat.Expanded);

                entry = entry with
                {
                    DataSetWriterId = assetId,
                    OpcNodes =
                    [
                        new ()
                        {
                            Id = assetId,
                            DataSetFieldId = entry.DataSetName // Asset name
                        }
                    ]
                };

                // Find the created asset file
                (nodeId, errorInfo) = await context.Session.GetAssetFileAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodeId!,
                    context.Ct).ConfigureAwait(false);
                if (errorInfo != null || nodeId is null || NodeId.IsNull(nodeId))
                {
                    errorInfo ??= new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadNotFound,
                        ErrorMessage = "Asset did not have a file component."
                    };
                    return (entry, errorInfo);
                }

                // 2) upload asset file
                var bufferSize = await context.Session.GetBufferSizeAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodeId,
                    context.Ct).ConfigureAwait(false);
                var (fileHandle, openError) = await context.Session.OpenAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodeId, 0x2 | 0x4,
                    context.Ct).ConfigureAwait(false);
                if (openError != null || !fileHandle.HasValue || NodeId.IsNull(nodeId))
                {
                    openError ??= new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadUserAccessDenied,
                        ErrorMessage = "Asset file could not be opened for write."
                    };
                    return (entry, openError);
                }
                while (true)
                {
                    // Copy buffers to server
                    var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    try
                    {
                        var read = await request.Configuration.ReadAsync(
                            buffer.AsMemory(), context.Ct).ConfigureAwait(false);
                        if (read == 0)
                        {
                            break;
                        }
                        errorInfo = await context.Session.WriteAsync(
                            request.Header.ToRequestHeader(_timeProvider), nodeId,
                            fileHandle.Value, buffer.AsMemory()[..read],
                            context.Ct).ConfigureAwait(false);
                        if (errorInfo != null)
                        {
                            return (entry, errorInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        return (entry, ex.ToServiceResultModel());
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                errorInfo = await context.Session.CloseAndUpdateAsync(
                    request.Header.ToRequestHeader(_timeProvider), nodeId,
                    fileHandle.Value, context.Ct).ConfigureAwait(false);
                return (entry, errorInfo);
            }, request.Header, ct).ConfigureAwait(false);

            // From now on we need to revert by deleting the asset
            if (errorInfo != null && errorInfo.StatusCode != 0)
            {
                if (entry.DataSetWriterId != null)
                {
                    // Delete the asset for good house keeping sake
                    await DeleteAssetAsync(request.Header, entry, ct).ConfigureAwait(false);
                }
                return new ServiceResponse<PublishedNodesEntryModel>
                {
                    Result = entry,
                    ErrorInfo = errorInfo
                };
            }
            try
            {
                if (request.WaitTime.HasValue)
                {
                    //
                    // The asset is uploaded the nodes are being created, potentially
                    // the session is disconnected now and the server is restarting.
                    // We wait a bit here to ensure all of this has happened correctly.
                    //
                    await _timeProvider.Delay(request.WaitTime.Value, ct).ConfigureAwait(false);
                }

                // 3) Collect all created tags under the asset
                var browser = new ConfigurationBrowser(entry, new PublishedNodeExpansionModel
                {
                    CreateSingleWriter = true,
                    MaxDepth = 0,
                    Header = request.Header
                }, _options, _configuration, _logger, _timeProvider);

                var results = new List<ServiceResponse<PublishedNodesEntryModel>>();
                await foreach (var configurationResult in _client.ExecuteAsync(
                    entry.ToConnectionModel(), browser, request.Header,
                    ct).ConfigureAwait(false))
                {
                    results.Add(configurationResult);
                }
                // We only expect a single writer here, else it is a failure.
                if (results.Count != 1 ||
                    (results[0].ErrorInfo != null && results[0].ErrorInfo!.StatusCode != 0))
                {
                    // Could not create tags - delete the asset
                    errorInfo = results.Select(r => r.ErrorInfo)
                        .FirstOrDefault(r => r != null);
                    errorInfo ??= new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadUnexpectedError,
                        ErrorMessage = "Failed to find any tags for the asset."
                    };
                    await DeleteAssetAsync(request.Header, entry, ct).ConfigureAwait(false);
                    return new ServiceResponse<PublishedNodesEntryModel>
                    {
                        Result = entry,
                        ErrorInfo = errorInfo
                    };
                }
                return results[0];
            }
            catch (Exception ex)
            {
                await DeleteAssetAsync(request.Header, entry, ct).ConfigureAwait(false);
                return new ServiceResponse<PublishedNodesEntryModel>
                {
                    Result = entry,
                    ErrorInfo = ex.ToServiceResultModel()
                };
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResultModel> DeleteAssetAsync(
            PublishedNodeDeleteAssetRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Entry);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterId);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterGroup);

            using var trace = _activitySource.StartActivity("DeleteAsset");

            // First remove the entry
            try
            {
                await _configuration.RemoveDataSetWriterEntryAsync(
                    request.Entry.DataSetWriterGroup, request.Entry.DataSetWriterId,
                    ct: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!request.Force)
                {
                    return ex.ToServiceResultModel();
                }
                _logger.DiscardErrorBecauseForceWasSet(ex);
            }
            var errorInfo = await DeleteAssetAsync(request.Header, request.Entry,
                ct).ConfigureAwait(false);
            return errorInfo ?? new ServiceResultModel();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <summary>
        /// Delete the asset
        /// </summary>
        /// <param name="header"></param>
        /// <param name="entry"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<ServiceResultModel?> DeleteAssetAsync(RequestHeaderModel? header,
            PublishedNodesEntryModel entry, CancellationToken ct)
        {
            return await _client.ExecuteAsync(entry.ToConnectionModel(), async context =>
            {
                var assetId = entry.DataSetWriterId.ToNodeId(context.Session.MessageContext);
                if (NodeId.IsNull(assetId))
                {
                    return new ServiceResultModel
                    {
                        StatusCode = StatusCodes.BadNodeIdInvalid,
                        ErrorMessage = "Invalid node id and browse path in file system object"
                    };
                }
                return await context.Session.DeleteAssetAsync(header.ToRequestHeader(_timeProvider),
                    assetId, ct).ConfigureAwait(false);
            }, header, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Validate nodes are valid
        /// </summary>
        /// <param name="opcNodes"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        internal static IList<OpcNodeModel> ValidateNodes(IList<OpcNodeModel> opcNodes)
        {
            var set = new HashSet<string>();
            foreach (var node in opcNodes)
            {
                if (!node.TryGetId(out var id))
                {
                    throw new BadRequestException("Node must contain a node ID");
                }
                node.DataSetFieldId ??= id;
                set.Add(node.DataSetFieldId);
                if (node.OpcPublishingInterval != null ||
                    node.OpcPublishingIntervalTimespan != null)
                {
                    throw new BadRequestException(
                        "Publishing interval not allowed on node level. " +
                        "Must be set at writer level.");
                }
            }
            if (set.Count != opcNodes.Count)
            {
                throw new BadRequestException("Field ids must be present and unique.");
            }
            return opcNodes;
        }

        private readonly IPublishedNodesServices _configuration;
        private readonly IOptions<PublisherOptions> _options;
        private readonly IOpcUaClientManager<ConnectionModel> _client;
        private readonly ILogger<ConfigurationServices> _logger;
        private readonly TimeProvider _timeProvider;
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
    }

    /// <summary>
    /// Source-generated logging extensions for ConfigurationServices
    /// </summary>
    internal static partial class ConfigurationServicesLogging
    {
        private const int EventClass = 100;

        [LoggerMessage(EventId = EventClass + 0, Level = LogLevel.Information,
            Message = "Discard error because force was set.")]
        public static partial void DiscardErrorBecauseForceWasSet(this ILogger logger, Exception ex);
    }
}
