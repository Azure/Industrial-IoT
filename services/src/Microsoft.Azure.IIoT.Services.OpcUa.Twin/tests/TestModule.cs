// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin {
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using System;
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
    public sealed class TestIdentity : IIdentity {
        public string Gateway => Utils.GetHostName();
        public string DeviceId => Gateway;
        public string ModuleId => "TestModule";
        public string SiteId => "TestSite";
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
            BrowseRequestModel request) {
            return _browser.NodeBrowseFirstAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<BrowseNextResultModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestModel request) {
            return _browser.NodeBrowseNextAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<BrowsePathResultModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestModel request) {
            return _browser.NodeBrowsePathAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<ValueReadResultModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestModel request) {
            return _nodes.NodeValueReadAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<ValueWriteResultModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestModel request) {
            return _nodes.NodeValueWriteAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestModel request) {
            return _nodes.NodeMethodGetMetadataAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<MethodCallResultModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestModel request) {
            return _nodes.NodeMethodCallAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<ReadResultModel> NodeReadAsync(
            string endpointId, ReadRequestModel request) {
            return _nodes.NodeReadAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<WriteResultModel> NodeWriteAsync(
            string endpointId, WriteRequestModel request) {
            return _nodes.NodeWriteAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResultModel<VariantValue>> HistoryReadAsync(
            string endpointId, HistoryReadRequestModel<VariantValue> request) {
            return _history.HistoryReadAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResultModel<VariantValue>> HistoryReadNextAsync(
            string endpointId, HistoryReadNextRequestModel request) {
            return _history.HistoryReadNextAsync(Endpoint, request);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryUpdateAsync(
            string endpointId, HistoryUpdateRequestModel<VariantValue> request) {
            return _history.HistoryUpdateAsync(Endpoint, request);
        }

        private readonly IBrowseServices<EndpointModel> _browser;
        private readonly IHistoricAccessServices<EndpointModel> _history;
        private readonly INodeServices<EndpointModel> _nodes;
    }
}
