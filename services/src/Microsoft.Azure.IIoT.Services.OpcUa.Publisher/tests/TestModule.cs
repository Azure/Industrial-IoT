// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITestModule {

        /// <summary>
        /// Endpoint
        /// </summary>
        EndpointModel Endpoint { get; set; }
    }

    /// <summary>
    /// Test identity
    /// </summary>
    public sealed class TestIdentity : IProcessIdentity {
        public string Id => Utils.GetHostName();
        public string SiteId => "TestSite";
        public string ProcessId => null;
        public string Name => null;
    }

    /// <summary>
    /// Test twin module
    /// </summary>
    public sealed class TestModule : IBrowseServices<string>, IHistoricAccessServices<string>,
        INodeServices<string>, ITestModule {

        /// <summary>
        /// The endpoint
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        public TestModule(IBrowseServices<EndpointModel> browser,
            IHistoricAccessServices<EndpointModel> history,
            INodeServices<EndpointModel> nodes) {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        /// <inheritdoc/>
        public Task<BrowseResultModel> NodeBrowseFirstAsync(string endpointId,
            BrowseRequestModel request, CancellationToken ct) {
            return _browser.NodeBrowseFirstAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<BrowseNextResultModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestModel request, CancellationToken ct) {
            return _browser.NodeBrowseNextAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<BrowsePathResultModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestModel request, CancellationToken ct) {
            return _browser.NodeBrowsePathAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<ValueReadResultModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestModel request, CancellationToken ct) {
            return _nodes.NodeValueReadAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<ValueWriteResultModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestModel request, CancellationToken ct) {
            return _nodes.NodeValueWriteAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestModel request, CancellationToken ct) {
            return _nodes.NodeMethodGetMetadataAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<MethodCallResultModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestModel request, CancellationToken ct) {
            return _nodes.NodeMethodCallAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<ReadResultModel> NodeReadAsync(
            string endpointId, ReadRequestModel request, CancellationToken ct) {
            return _nodes.NodeReadAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<WriteResultModel> NodeWriteAsync(
            string endpointId, WriteRequestModel request, CancellationToken ct) {
            return _nodes.NodeWriteAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResultModel<VariantValue>> HistoryReadAsync(
            string endpointId, HistoryReadRequestModel<VariantValue> request, CancellationToken ct) {
            return _history.HistoryReadAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResultModel<VariantValue>> HistoryReadNextAsync(
            string endpointId, HistoryReadNextRequestModel request, CancellationToken ct) {
            return _history.HistoryReadNextAsync(Endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryUpdateAsync(string endpointId,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct) {
            return _history.HistoryUpdateAsync(Endpoint, request, ct);
        }

        private readonly IBrowseServices<EndpointModel> _browser;
        private readonly IHistoricAccessServices<EndpointModel> _history;
        private readonly INodeServices<EndpointModel> _nodes;
    }
}
