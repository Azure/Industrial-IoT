﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.App.Services;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.App.Models;

    public partial class Browser {
        [Parameter]
        public string DiscovererId { get; set; } = string.Empty;

        [Parameter]
        public string EndpointId { get; set; } = string.Empty;

        [Parameter]
        public string ApplicationId { get; set; } = string.Empty;

        [Parameter]
        public string SupervisorId { get; set; } = string.Empty;

        [Parameter]
        public string Page { get; set; } = "1";

        public PagedResult<ListNode> NodeList = new PagedResult<ListNode>();
        public PagedResult<ListNode> PagedNodeList = new PagedResult<ListNode>();
        public PagedResult<PublishedItemApiModel> PublishedNodes = new PagedResult<PublishedItemApiModel>();
        public CredentialModel Credential = new CredentialModel();
        public bool IsOpened { get; set; } = false;
        public ListNode NodeData { get; set; }
        public string EndpointUrl { get; set; } = null;
        private IAsyncDisposable _publishEvent { get; set; }
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";
        private List<string> _parentId { get; set; }
        private enum drawer {
            Action = 0,
            Publisher
        }
        private drawer _drawerType { get; set; }
        private const int _firstPage = 1;


        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChanged(int page) {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            if (!string.IsNullOrEmpty(NodeList.ContinuationToken) && page > PagedNodeList.PageCount) {
                await BrowseTreeAsync(BrowseDirection.Forward, 0, false, page);
            }
            PagedNodeList = NodeList.GetPaged(page, CommonHelper.PageLength, null);
            foreach (var node in PagedNodeList.Results) {
                //fetch the actual value
                if (node.NodeClass == NodeClass.Variable) {
                    node.Value = await BrowseManager.ReadValueAsync(EndpointId, node.Id, Credential);
                }
            }
            CommonHelper.Spinner = string.Empty;
            StateHasChanged();
        }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized() {
            CommonHelper.Spinner = "loader-big";
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                var endpoint = await registryService.GetEndpointAsync(EndpointId);
                EndpointUrl = endpoint?.Registration?.EndpointUrl;
                Credential = await GetSecureItemAsync<CredentialModel>(CommonHelper.CredentialKey);
                await BrowseTreeAsync(BrowseDirection.Forward, 0, true, _firstPage, string.Empty, new List<string>());
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty<ListNode>(PagedNodeList, ref _tableView, ref _tableEmpty);
                StateHasChanged();
                if (PublishedNodes.Results.Count > 0) {
                    _publishEvent = await PublisherServiceEvents.NodePublishSubscribeByEndpointAsync(EndpointId,
                    samples => InvokeAsync(() => GetPublishedNodeData(samples)));
                }
            }
        }

        /// <summary>
        /// Browse forward the tree nodes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        private async Task GetTreeAsync(string id, List<string> parentId) {
            await BrowseTreeAsync(BrowseDirection.Forward, 0, true, _firstPage, id, parentId);
        }

        /// <summary>
        /// Browse backward the tree nodes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        private async Task GetTreeBackAsync(string id, List<string> parentId, int index) {
            await BrowseTreeAsync(BrowseDirection.Backward, index, true, _firstPage, id, parentId);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "browser/1/" + DiscovererId + "/" + ApplicationId + "/" + SupervisorId + "/" + EndpointId);
        }

        /// <summary>
        /// Browse the tree nodes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        /// <param name="direction"></param>
        private async Task BrowseTreeAsync(BrowseDirection direction, int index, bool firstPage, int page, string id = null, List<string> parentId = null) {
            CommonHelper.Spinner = "loader-big";

            if (firstPage) {
                _parentId = parentId;
                NodeList = await BrowseManager.GetTreeAsync(EndpointId,
                                            id,
                                            parentId,
                                            DiscovererId,
                                            direction,
                                            index,
                                            Credential);
            }
            else {
                NodeList = await BrowseManager.GetTreeNextAsync(EndpointId,
                                                _parentId,
                                                DiscovererId,
                                                Credential,
                                                NodeList);
            }

            PublishedNodes = await Publisher.PublishedAsync(EndpointId);

            foreach (var node in NodeList.Results) {
                if (node.NodeClass == NodeClass.Variable) {
                    // check if publishing enabled
                    foreach (var publishedItem in PublishedNodes.Results) {
                        if (node.Id == publishedItem.NodeId) {
                            node.PublishedItem = publishedItem;
                            node.Publishing = true;
                            break;
                        }
                    }
                }
            }

            PagedNodeList = NodeList.GetPaged(page, CommonHelper.PageLength, NodeList.Error);
            if (string.IsNullOrEmpty(DiscovererId)) {
                NavigationManager.NavigateTo(NavigationManager.BaseUri + "browser/" + page + "/" + ApplicationId + "/" + EndpointId);
            }
            else {
                NavigationManager.NavigateTo(NavigationManager.BaseUri + "browser/" + page + "/" + DiscovererId + "/" + ApplicationId + "/" + SupervisorId + "/" + EndpointId);
            }
            CommonHelper.Spinner = "";
        }

        /// <summary>
        /// Manage Publishing a node
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="node"></param>
        private async Task SetPublishingAsync(string endpointId, ListNode node) {
            if (!node.Publishing) {
                await PublishNodeAsync(endpointId, node);
            }
            else {
                await UnPublishNodeAsync(endpointId, node);
            }
        }

        /// <summary>
        /// Publish a node
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="node"></param>
        private async Task PublishNodeAsync(string endpointId, ListNode node) {
            node.Publishing = true;
            var publishingInterval = node.PublishedItem?.PublishingInterval == null ? TimeSpan.FromMilliseconds(1000) : node.PublishedItem.PublishingInterval;
            var samplingInterval = node.PublishedItem?.SamplingInterval == null ? TimeSpan.FromMilliseconds(1000) : node.PublishedItem.SamplingInterval;
            var heartbeatInterval = node.PublishedItem?.HeartbeatInterval;
            var result = await Publisher.StartPublishingAsync(endpointId, node.Id, node.NodeName, samplingInterval, publishingInterval, heartbeatInterval, Credential);
            if (result) {
                node.PublishedItem = new OpcUa.Api.Publisher.Models.PublishedItemApiModel() {
                    NodeId = node.Id,
                    DisplayName = node.NodeName,
                    PublishingInterval = publishingInterval,
                    SamplingInterval = samplingInterval,
                    HeartbeatInterval = heartbeatInterval
                };
                if (_publishEvent == null) {
                    _publishEvent = Task.Run(async () => await PublisherServiceEvents.NodePublishSubscribeByEndpointAsync(EndpointId,
                        samples => InvokeAsync(() => GetPublishedNodeData(samples)))).Result;
                }
            }
            else {
                node.PublishedItem = null;
                node.Publishing = false;
            }
        }

        /// <summary>
        /// UnPublish a node
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="node"></param>
        private async Task UnPublishNodeAsync(string endpointId, ListNode node) {
            var result = await Publisher.StopPublishingAsync(endpointId, node.Id, Credential);
            if (result) {
                node.PublishedItem = null;
                node.Publishing = false;
            }
        }

        /// <summary>
        /// Open the Drawer
        /// </summary>
        /// <param name="node"></param>
        private void OpenDrawer(ListNode node, drawer type) {
            IsOpened = true;
            NodeData = node;
            _drawerType = type;
        }

        /// <summary>
        /// Close the Drawer
        /// </summary>
        private void CloseDrawer() {
            IsOpened = false;
            BrowseManager.MethodCallResponse = null;
            this.StateHasChanged();
        }

        /// <summary>
        /// GetPublishedNodeData
        /// </summary>
        /// <param name="samples"></param>
        private Task GetPublishedNodeData(MonitoredItemMessageApiModel samples) {
            foreach (var node in PagedNodeList.Results) {
                if (node.Id == samples.NodeId) {
                    node.Value = samples.Value?.ToJson()?.TrimQuotes();
                    node.Status = string.IsNullOrEmpty(samples.Status) ? "Good" : samples.Status;
                    node.Timestamp = samples.Timestamp.Value.ToLocalTime().ToString();
                    this.StateHasChanged();
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// ClickHandler
        /// </summary>
        async Task ClickHandler(ListNode node) {
            CloseDrawer();
            await PublishNodeAsync(EndpointId, node);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public async void Dispose() {
            if (_publishEvent != null) {
                await _publishEvent.DisposeAsync();
            }
        }

        /// <summary>
        /// Get Item stored in session storage
        /// </summary>
        /// <param name="key"></param>
        private async Task<T> GetSecureItemAsync<T>(string key) {
            var serializedProtectedData = await sessionStorage.GetItemAsync<string>(key);
            return secureData.UnprotectDeserialize<T>(serializedProtectedData);
        }
    }
}