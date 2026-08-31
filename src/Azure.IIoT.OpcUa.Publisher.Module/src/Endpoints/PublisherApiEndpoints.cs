// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;

    /// <summary>
    /// Maps the OPC Publisher REST surface as minimal API endpoint groups. Each
    /// endpoint delegates into the same controller methods that also back the
    /// IoT Hub direct method dispatch (through the Core MethodRouter), so there is
    /// a single implementation for both transports. The routes, verbs and response
    /// shapes are identical to the former MVC controllers to keep the REST wire
    /// behavior unchanged after removing <c>AddControllers()</c>.
    /// </summary>
    internal static class PublisherApiEndpoints
    {
        /// <summary>
        /// Map all publisher REST endpoints under the "v2" prefix.
        /// </summary>
        /// <param name="endpoints"></param>
        public static IEndpointRouteBuilder MapPublisherApi(this IEndpointRouteBuilder endpoints)
        {
            var v2 = endpoints.MapGroup("v2")
                .RequireAuthorization()
                .AddEndpointFilter<RestExceptionFilter>();

            MapCertificates(v2.MapGroup("pki"));
            MapConfiguration(v2.MapGroup("configuration"));
            MapDiagnostics(v2);
            MapDiscovery(v2.MapGroup("discovery"));
            MapFileSystem(v2.MapGroup("filesystem"));
            MapGeneral(v2);
            MapHistory(v2.MapGroup("history"));
            MapWriter(v2.MapGroup("writer"));
            return endpoints;
        }

        private static void MapCertificates(RouteGroupBuilder g)
        {
            g.MapGet("{store}/certs", (
                [FromServices] CertificatesController c, string store, CancellationToken ct)
                => c.ListCertificatesAsync(store, ct));
            g.MapGet("{store}/crls", (
                [FromServices] CertificatesController c, string store, CancellationToken ct)
                => c.ListCertificateRevocationListsAsync(store, ct));
            g.MapPatch("{store}/certs", (
                [FromServices] CertificatesController c, string store,
                [FromBody] byte[] pfxBlob, [FromQuery] string? password, CancellationToken ct)
                => c.AddCertificateAsync(store, pfxBlob, password, ct));
            g.MapPatch("{store}/crls", (
                [FromServices] CertificatesController c, string store,
                [FromBody] byte[] crl, CancellationToken ct)
                => c.AddCertificateRevocationListAsync(store, crl, ct));
            g.MapPost("trusted/certs", (
                [FromServices] CertificatesController c,
                [FromBody] byte[] certificateChain, CancellationToken ct)
                => c.AddCertificateChainAsync(certificateChain, ct));
            g.MapPost("rejected/certs/{thumbprint}/approve", (
                [FromServices] CertificatesController c, string thumbprint, CancellationToken ct)
                => c.ApproveRejectedCertificateAsync(thumbprint, ct));
            g.MapPost("https/certs", (
                [FromServices] CertificatesController c,
                [FromBody] byte[] certificateChain, CancellationToken ct)
                => c.AddTrustedHttpsCertificateAsync(certificateChain, ct));
            g.MapDelete("{store}/certs/{thumbprint}", (
                [FromServices] CertificatesController c, string store, string thumbprint,
                CancellationToken ct)
                => c.RemoveCertificateAsync(store, thumbprint, ct));
            g.MapDelete("{store}/crls", (
                [FromServices] CertificatesController c, string store,
                [FromBody] byte[] crl, CancellationToken ct)
                => c.RemoveCertificateRevocationListAsync(store, crl, ct));
            g.MapDelete("{store}", (
                [FromServices] CertificatesController c, string store, CancellationToken ct)
                => c.RemoveAllAsync(store, ct));
        }

        private static void MapConfiguration(RouteGroupBuilder g)
        {
            g.MapPost("start", (
                [FromServices] ConfigurationController c,
                RequestEnvelope<PublishStartRequestModel> request)
                => c.PublishStartAsync(request));
            g.MapPost("stop", (
                [FromServices] ConfigurationController c,
                RequestEnvelope<PublishStopRequestModel> request)
                => c.PublishStopAsync(request));
            g.MapPost("bulk", (
                [FromServices] ConfigurationController c,
                RequestEnvelope<PublishBulkRequestModel> request)
                => c.PublishBulkAsync(request));
            g.MapPost("list", (
                [FromServices] ConfigurationController c,
                RequestEnvelope<PublishedItemListRequestModel> request)
                => c.PublishListAsync(request));
            g.MapPost("nodes", (
                [FromServices] ConfigurationController c,
                PublishedNodesEntryModel request, CancellationToken ct)
                => c.PublishNodesAsync(request, ct));
            g.MapPost("nodes/unpublish", (
                [FromServices] ConfigurationController c,
                PublishedNodesEntryModel request, CancellationToken ct)
                => c.UnpublishNodesAsync(request, ct));
            g.MapPost("nodes/unpublish/all", (
                [FromServices] ConfigurationController c,
                [FromBody] PublishedNodesEntryModel? request, CancellationToken ct)
                => c.UnpublishAllNodesAsync(request, ct));
            g.MapPatch("", (
                [FromServices] ConfigurationController c,
                IReadOnlyList<PublishedNodesEntryModel> request, CancellationToken ct)
                => c.AddOrUpdateEndpointsAsync(request, ct));
            g.MapGet("", (
                [FromServices] ConfigurationController c,
                [FromQuery] bool? includeNodes, CancellationToken ct)
                => c.GetConfiguredEndpointsAsync(
                    new GetConfiguredEndpointsRequestModel { IncludeNodes = includeNodes }, ct));
            g.MapPut("", (
                [FromServices] ConfigurationController c,
                SetConfiguredEndpointsRequestModel request, CancellationToken ct)
                => c.SetConfiguredEndpointsAsync(request, ct));
            g.MapPost("endpoints/list/nodes", (
                [FromServices] ConfigurationController c,
                PublishedNodesEntryModel request, CancellationToken ct)
                => c.GetConfiguredNodesOnEndpointAsync(request, ct));
            g.MapPost("diagnostics", (
                [FromServices] ConfigurationController c, CancellationToken ct)
                => c.GetDiagnosticInfoAsync(ct));
        }

        private static void MapDiagnostics(RouteGroupBuilder g)
        {
            g.MapGet("reset", (
                [FromServices] DiagnosticsController c, CancellationToken ct)
                => c.ResetAllConnectionsAsync(ct));
            g.MapGet("connections", (
                [FromServices] DiagnosticsController c, CancellationToken ct)
                => c.GetActiveConnectionsAsync(ct));
            g.MapGet("diagnostics/writergroups/{dataSetWriterGroup}", (
                [FromServices] DiagnosticsController c, string dataSetWriterGroup,
                CancellationToken ct)
                => c.GetWriterGroupStateAsync(dataSetWriterGroup, ct));
            g.MapGet("diagnostics/writergroups", (
                [FromServices] DiagnosticsController c, CancellationToken ct)
                => c.GetAllWriterGroupStatesAsync(ct));
            g.MapPost("diagnostics/writergroups/{dataSetWriterGroup}/keyframe", (
                [FromServices] DiagnosticsController c, string dataSetWriterGroup,
                CancellationToken ct)
                => c.SendWriterGroupKeyFrameAsync(dataSetWriterGroup, ct));
            g.MapPost("diagnostics/writergroups/{dataSetWriterGroup}/writers/{dataSetWriterId}/keyframe", (
                [FromServices] DiagnosticsController c, string dataSetWriterGroup,
                string dataSetWriterId, CancellationToken ct)
                => c.SendDataSetWriterKeyFrameAsync(dataSetWriterGroup, dataSetWriterId, ct));
            g.MapGet("diagnostics/connections", (
                [FromServices] DiagnosticsController c, CancellationToken ct)
                => c.GetConnectionDiagnosticsAsync(ct));
            g.MapGet("diagnostics/channels", (
                [FromServices] DiagnosticsController c, CancellationToken ct)
                => c.GetChannelDiagnosticsAsync(ct));
            g.MapGet("diagnostics/channels/watch", (
                [FromServices] DiagnosticsController c, CancellationToken ct)
                => c.WatchChannelDiagnosticsAsync(ct));
        }

        private static void MapDiscovery(RouteGroupBuilder g)
        {
            g.MapPost("findserver", (
                [FromServices] DiscoveryController c,
                ServerEndpointQueryModel endpoint, CancellationToken ct)
                => c.FindServerAsync(endpoint, ct));
            g.MapPost("register", (
                [FromServices] DiscoveryController c,
                ServerRegistrationRequestModel request, CancellationToken ct)
                => c.RegisterAsync(request, ct));
            g.MapPost("", (
                [FromServices] DiscoveryController c,
                DiscoveryRequestModel request, CancellationToken ct)
                => c.DiscoverAsync(request, ct));
            g.MapPost("cancel", (
                [FromServices] DiscoveryController c,
                DiscoveryCancelRequestModel request, CancellationToken ct)
                => c.CancelAsync(request, ct));
        }

        private static void MapFileSystem(RouteGroupBuilder g)
        {
            g.MapPost("list", (
                [FromServices] FileSystemController c,
                ConnectionModel connection, CancellationToken ct)
                => c.GetFileSystemsAsync(connection, ct));
            g.MapPost("list/directories", (
                [FromServices] FileSystemController c,
                RequestEnvelope<FileSystemObjectModel> request, CancellationToken ct)
                => c.GetDirectoriesAsync(request, ct));
            g.MapPost("list/files", (
                [FromServices] FileSystemController c,
                RequestEnvelope<FileSystemObjectModel> request, CancellationToken ct)
                => c.GetFilesAsync(request, ct));
            g.MapPost("parent", (
                [FromServices] FileSystemController c,
                RequestEnvelope<FileSystemObjectModel> request, CancellationToken ct)
                => c.GetParentAsync(request, ct));
            g.MapPost("info/file", (
                [FromServices] FileSystemController c,
                RequestEnvelope<FileSystemObjectModel> request, CancellationToken ct)
                => c.GetFileInfoAsync(request, ct));
            g.MapPost("create/file/{name}", (
                [FromServices] FileSystemController c,
                RequestEnvelope<FileSystemObjectModel> request, string name, CancellationToken ct)
                => c.CreateFileAsync(request, name, ct));
            g.MapPost("create/directory/{name}", (
                [FromServices] FileSystemController c,
                RequestEnvelope<FileSystemObjectModel> request, string name, CancellationToken ct)
                => c.CreateDirectoryAsync(request, name, ct));
            g.MapPost("delete", (
                [FromServices] FileSystemController c,
                RequestEnvelope<FileSystemObjectModel> request, CancellationToken ct)
                => c.DeleteFileSystemObjectAsync(request, ct));
            g.MapPost("delete/{fileOrDirectoryNodeId}", (
                [FromServices] FileSystemController c,
                RequestEnvelope<FileSystemObjectModel> request, string fileOrDirectoryNodeId,
                CancellationToken ct)
                => c.DeleteFileOrDirectoryAsync(request, fileOrDirectoryNodeId, ct));
            g.MapGet("download", (
                [FromServices] FileSystemController c,
                [FromHeader(Name = "x-ms-connection")] string connectionJson,
                [FromHeader(Name = "x-ms-target")] string fileObjectJson,
                HttpContext httpContext, CancellationToken ct)
                => c.DownloadAsync(connectionJson, fileObjectJson, httpContext, ct));
            g.MapPost("upload", (
                [FromServices] FileSystemController c,
                [FromHeader(Name = "x-ms-connection")] string connectionJson,
                [FromHeader(Name = "x-ms-target")] string fileObjectJson,
                [FromHeader(Name = "x-ms-options")] string writeOptionsJson,
                HttpContext httpContext, CancellationToken ct)
                => c.UploadAsync(connectionJson, fileObjectJson, writeOptionsJson, httpContext, ct));
        }

        private static void MapGeneral(RouteGroupBuilder g)
        {
            g.MapPost("capabilities", (
                [FromServices] GeneralController c,
                RequestEnvelope<RequestHeaderModel?> request, CancellationToken ct)
                => c.GetServerCapabilitiesAsync(request, ct));
            g.MapPost("browse/first", (
                [FromServices] GeneralController c,
                RequestEnvelope<BrowseFirstRequestModel> request, CancellationToken ct)
                => c.BrowseAsync(request, ct));
            g.MapPost("browse/next", (
                [FromServices] GeneralController c,
                RequestEnvelope<BrowseNextRequestModel> request, CancellationToken ct)
                => c.BrowseNextAsync(request, ct));
            g.MapPost("browse", (
                [FromServices] GeneralController c,
                RequestEnvelope<BrowseStreamRequestModel> request, CancellationToken ct)
                => c.BrowseStreamAsync(request, ct));
            g.MapPost("browse/path", (
                [FromServices] GeneralController c,
                RequestEnvelope<BrowsePathRequestModel> request, CancellationToken ct)
                => c.BrowsePathAsync(request, ct));
            g.MapPost("read", (
                [FromServices] GeneralController c,
                RequestEnvelope<ValueReadRequestModel> request, CancellationToken ct)
                => c.ValueReadAsync(request, ct));
            g.MapPost("write", (
                [FromServices] GeneralController c,
                RequestEnvelope<ValueWriteRequestModel> request, CancellationToken ct)
                => c.ValueWriteAsync(request, ct));
            g.MapPost("metadata", (
                [FromServices] GeneralController c,
                RequestEnvelope<NodeMetadataRequestModel> request, CancellationToken ct)
                => c.GetMetadataAsync(request, ct));
            g.MapPost("query/compile", (
                [FromServices] GeneralController c,
                RequestEnvelope<QueryCompilationRequestModel> request, CancellationToken ct)
                => c.CompileQueryAsync(request, ct));
            g.MapPost("call/$metadata", (
                [FromServices] GeneralController c,
                RequestEnvelope<MethodMetadataRequestModel> request, CancellationToken ct)
                => c.MethodMetadataAsync(request, ct));
            g.MapPost("call", (
                [FromServices] GeneralController c,
                RequestEnvelope<MethodCallRequestModel> request, CancellationToken ct)
                => c.MethodCallAsync(request, ct));
            g.MapPost("read/attributes", (
                [FromServices] GeneralController c,
                RequestEnvelope<ReadRequestModel> request, CancellationToken ct)
                => c.NodeReadAsync(request, ct));
            g.MapPost("write/attributes", (
                [FromServices] GeneralController c,
                RequestEnvelope<WriteRequestModel> request, CancellationToken ct)
                => c.NodeWriteAsync(request, ct));
            g.MapPost("historyread/first", (
                [FromServices] GeneralController c,
                RequestEnvelope<HistoryReadRequestModel<JsonNode>> request, CancellationToken ct)
                => c.HistoryReadAsync(request, ct));
            g.MapPost("historyread/next", (
                [FromServices] GeneralController c,
                RequestEnvelope<HistoryReadNextRequestModel> request, CancellationToken ct)
                => c.HistoryReadNextAsync(request, ct));
            g.MapPost("historyupdate", (
                [FromServices] GeneralController c,
                RequestEnvelope<HistoryUpdateRequestModel<JsonNode>> request, CancellationToken ct)
                => c.HistoryUpdateAsync(request, ct));
            g.MapPost("certificate", (
                [FromServices] GeneralController c,
                EndpointModel endpoint, CancellationToken ct)
                => c.GetEndpointCertificateAsync(endpoint, ct));
            g.MapPost("history/capabilities", (
                [FromServices] GeneralController c,
                RequestEnvelope<RequestHeaderModel?> request, CancellationToken ct)
                => c.HistoryGetServerCapabilitiesAsync(request, ct));
            g.MapPost("history/configuration", (
                [FromServices] GeneralController c,
                RequestEnvelope<HistoryConfigurationRequestModel> request, CancellationToken ct)
                => c.HistoryGetConfigurationAsync(request, ct));
            g.MapPost("test", (
                [FromServices] GeneralController c,
                RequestEnvelope<TestConnectionRequestModel> request, CancellationToken ct)
                => c.TestConnectionAsync(request, ct));
        }

        private static void MapHistory(RouteGroupBuilder g)
        {
            g.MapPost("events/replace", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryReplaceEventsAsync(request, ct));
            g.MapPost("events/insert", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryInsertEventsAsync(request, ct));
            g.MapPost("events/upsert", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryUpsertEventsAsync(request, ct));
            g.MapPost("events/delete", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<DeleteEventsDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryDeleteEventsAsync(request, ct));
            g.MapPost("values/delete/attimes", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryDeleteValuesAtTimesAsync(request, ct));
            g.MapPost("values/delete/modified", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryDeleteModifiedValuesAsync(request, ct));
            g.MapPost("values/delete", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryDeleteValuesAsync(request, ct));
            g.MapPost("values/replace", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryReplaceValuesAsync(request, ct));
            g.MapPost("values/insert", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryInsertValuesAsync(request, ct));
            g.MapPost("values/upsert", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryUpsertValuesAsync(request, ct));
            g.MapPost("events/read/first", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadEventsDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryReadEventsAsync(request, ct));
            g.MapPost("events/read/next", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadNextRequestModel> request, CancellationToken ct)
                => c.HistoryReadEventsNextAsync(request, ct));
            g.MapPost("values/read/first", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryReadValuesAsync(request, ct));
            g.MapPost("values/read/first/attimes", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryReadValuesAtTimesAsync(request, ct));
            g.MapPost("values/read/first/processed", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadProcessedValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryReadProcessedValuesAsync(request, ct));
            g.MapPost("values/read/first/modified", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadModifiedValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryReadModifiedValuesAsync(request, ct));
            g.MapPost("values/read/next", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadNextRequestModel> request, CancellationToken ct)
                => c.HistoryReadValuesNextAsync(request, ct));
            g.MapPost("values/read", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryStreamValuesAsync(request, ct));
            g.MapPost("values/read/modified", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadModifiedValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryStreamModifiedValuesAsync(request, ct));
            g.MapPost("values/read/attimes", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryStreamValuesAtTimesAsync(request, ct));
            g.MapPost("values/read/processed", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadProcessedValuesDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryStreamProcessedValuesAsync(request, ct));
            g.MapPost("events/read", (
                [FromServices] HistoryController c,
                RequestEnvelope<HistoryReadRequestModel<ReadEventsDetailsModel>> request,
                CancellationToken ct)
                => c.HistoryStreamEventsAsync(request, ct));
        }

        private static void MapWriter(RouteGroupBuilder g)
        {
            g.MapPut("", (
                [FromServices] WriterController c,
                PublishedNodesEntryModel dataSetWriterEntry, CancellationToken ct)
                => c.CreateOrUpdateDataSetWriterEntryAsync(dataSetWriterEntry, ct));
            g.MapGet("{dataSetWriterGroup}/{dataSetWriterId}", (
                [FromServices] WriterController c, string dataSetWriterGroup,
                string dataSetWriterId, CancellationToken ct)
                => c.GetDataSetWriterEntryAsync(dataSetWriterGroup, dataSetWriterId, ct));
            g.MapPost("{dataSetWriterGroup}/{dataSetWriterId}/add", (
                [FromServices] WriterController c, string dataSetWriterGroup,
                string dataSetWriterId, [FromBody] IReadOnlyList<OpcNodeModel> opcNodes,
                [FromQuery] string? insertAfterFieldId, CancellationToken ct)
                => c.AddOrUpdateNodesAsync(dataSetWriterGroup, dataSetWriterId, opcNodes,
                    insertAfterFieldId, ct));
            g.MapPut("{dataSetWriterGroup}/{dataSetWriterId}", (
                [FromServices] WriterController c, string dataSetWriterGroup,
                string dataSetWriterId, [FromBody] OpcNodeModel opcNode,
                [FromQuery] string? insertAfterFieldId, CancellationToken ct)
                => c.AddOrUpdateNodeAsync(dataSetWriterGroup, dataSetWriterId, opcNode,
                    insertAfterFieldId, ct));
            g.MapPost("{dataSetWriterGroup}/{dataSetWriterId}/remove", (
                [FromServices] WriterController c, string dataSetWriterGroup,
                string dataSetWriterId, [FromBody] IReadOnlyList<string> dataSetFieldIds,
                CancellationToken ct)
                => c.RemoveNodesAsync(dataSetWriterGroup, dataSetWriterId, dataSetFieldIds, ct));
            g.MapDelete("{dataSetWriterGroup}/{dataSetWriterId}/{dataSetFieldId}", (
                [FromServices] WriterController c, string dataSetWriterGroup,
                string dataSetWriterId, string dataSetFieldId, CancellationToken ct)
                => c.RemoveNodeAsync(dataSetWriterGroup, dataSetWriterId, dataSetFieldId, ct));
            g.MapGet("{dataSetWriterGroup}/{dataSetWriterId}/{dataSetFieldId}", (
                [FromServices] WriterController c, string dataSetWriterGroup,
                string dataSetWriterId, string dataSetFieldId, CancellationToken ct)
                => c.GetNodeAsync(dataSetWriterGroup, dataSetWriterId, dataSetFieldId, ct));
            g.MapGet("{dataSetWriterGroup}/{dataSetWriterId}/nodes", (
                [FromServices] WriterController c, string dataSetWriterGroup,
                string dataSetWriterId, [FromQuery] string? lastDataSetFieldId,
                [FromQuery] int? pageSize, HttpRequest httpRequest, CancellationToken ct)
                => c.GetNodesAsync(dataSetWriterGroup, dataSetWriterId, lastDataSetFieldId,
                    pageSize, httpRequest, ct));
            g.MapDelete("{dataSetWriterGroup}/{dataSetWriterId}", (
                [FromServices] WriterController c, string dataSetWriterGroup,
                string dataSetWriterId, [FromQuery] bool force, CancellationToken ct)
                => c.RemoveDataSetWriterEntryAsync(dataSetWriterGroup, dataSetWriterId, force, ct));
            g.MapPost("expand", (
                [FromServices] WriterController c,
                PublishedNodesEntryRequestModel<PublishedNodeExpansionModel> request,
                CancellationToken ct)
                => c.ExpandWriterAsync(request, ct));
            g.MapPost("", (
                [FromServices] WriterController c,
                PublishedNodesEntryRequestModel<PublishedNodeExpansionModel> request,
                CancellationToken ct)
                => c.ExpandAndCreateOrUpdateDataSetWriterEntriesAsync(request, ct));
            g.MapPost("assets/create", (
                [FromServices] WriterController c,
                PublishedNodeCreateAssetRequestModel<byte[]> request, CancellationToken ct)
                => c.CreateOrUpdateAsset2Async(request, ct));
            g.MapPost("assets", (
                [FromServices] WriterController c,
                PublishedNodeCreateAssetRequestModel<JsonNode> request, CancellationToken ct)
                => c.CreateOrUpdateAssetAsync(request, ct));
            g.MapPost("assets/list", (
                [FromServices] WriterController c,
                PublishedNodesEntryRequestModel<RequestHeaderModel> request, CancellationToken ct)
                => c.GetAllAssetsAsync(request, ct));
            g.MapPost("assets/delete", (
                [FromServices] WriterController c,
                PublishedNodeDeleteAssetRequestModel request, CancellationToken ct)
                => c.DeleteAssetAsync(request, ct));
        }
    }
}
