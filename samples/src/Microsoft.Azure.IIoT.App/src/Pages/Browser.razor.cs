// ------------------------------------------------------------
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
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;

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

        public PagedResult<ListNode> NodeList { get; set; } = new PagedResult<ListNode>();
        public PagedResult<ListNode> PagedNodeList { get; set; } = new PagedResult<ListNode>();
        public PagedResult<ListNode> PublishedNodes { get; set; } = new PagedResult<ListNode>();
        public CredentialModel Credential { get; set; } = new CredentialModel();
        public bool IsOpen { get; set; } = false;
        public ListNode NodeData { get; set; }
        public EndpointInfoApiModel EndpointModel { get; set; }
        private IAsyncDisposable PublishEvent { get; set; }
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";
        private List<string> ParentId { get; set; }
        private enum Drawer {
            Action = 0,
            Publisher
        }
        private Drawer DrawerType { get; set; }
        private const int FirstPage = 1;

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page) {
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
                EndpointModel = await registryService.GetEndpointAsync(EndpointId);
                Credential = await GetSecureItemAsync<CredentialModel>(CommonHelper.CredentialKey);
                await BrowseTreeAsync(BrowseDirection.Forward, 0, true, FirstPage, string.Empty, new List<string>());
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(PagedNodeList, ref _tableView, ref _tableEmpty);
                StateHasChanged();
                if (PublishedNodes.Results.Count > 0) {
                    PublishEvent = await PublisherServiceEvents.NodePublishSubscribeByEndpointAsync(EndpointId,
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
            await BrowseTreeAsync(BrowseDirection.Forward, 0, true, FirstPage, id, parentId);
        }

        /// <summary>
        /// Browse backward the tree nodes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        private async Task GetTreeBackAsync(string id, List<string> parentId, int index) {
            await BrowseTreeAsync(BrowseDirection.Backward, index, true, FirstPage, id, parentId);
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
                ParentId = parentId;
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
                                                ParentId,
                                                DiscovererId,
                                                Credential,
                                                NodeList);
            }

            PublishedNodes = await Publisher.PublishedAsync(EndpointId, false);

            foreach (var node in NodeList.Results) {
                if (node.NodeClass == NodeClass.Variable) {
                    // check if publishing enabled
                    foreach (var publishedNode in PublishedNodes.Results) {
                        if (node.Id == publishedNode.PublishedItem.NodeId) {
                            node.PublishedItem = publishedNode.PublishedItem;
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
            var item = node.PublishedItem;
            var publishingInterval = IsTimeIntervalSet(item?.PublishingInterval) ? item.PublishingInterval : TimeSpan.FromMilliseconds(1000);
            var samplingInterval = IsTimeIntervalSet(item?.SamplingInterval) ? item.SamplingInterval : TimeSpan.FromMilliseconds(1000);
            var heartbeatInterval = IsTimeIntervalSet(item?.HeartbeatInterval) ? item.HeartbeatInterval : null;
            var result = await Publisher.StartPublishingAsync(endpointId, node.Id, node.NodeName, samplingInterval, publishingInterval, heartbeatInterval, Credential);
            if (result) {
                node.PublishedItem = new PublishedItemApiModel() {
                    NodeId = node.Id,
                    PublishingInterval = publishingInterval,
                    SamplingInterval = samplingInterval,
                    HeartbeatInterval = heartbeatInterval
                };
                if (PublishEvent == null) {
                    PublishEvent = Task.Run(async () => await PublisherServiceEvents.NodePublishSubscribeByEndpointAsync(EndpointId,
                        samples => InvokeAsync(() => GetPublishedNodeData(samples)))).Result;
                }
            }
            else {
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
                node.Publishing = false;
            }
        }

        /// <summary>
        /// Open the Drawer
        /// </summary>
        /// <param name="node"></param>
        private void OpenDrawer(ListNode node, Drawer type) {
            IsOpen = true;
            NodeData = node;
            DrawerType = type;
        }

        /// <summary>
        /// Close the Drawer
        /// </summary>
        private void CloseDrawer() {
            IsOpen = false;
            BrowseManager.MethodCallResponse = null;
            StateHasChanged();
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
                    StateHasChanged();
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// ClickHandler
        /// </summary>
        async Task ClickHandlerAsync(ListNode node) {
            CloseDrawer();
            await PublishNodeAsync(EndpointId, node);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public async void Dispose() {
            if (PublishEvent != null) {
                await PublishEvent.DisposeAsync();
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

        /// <summary>
        /// Checks whether the time interval is set or not
        /// </summary>
        /// <param name="interval"></param>
        /// <returns>True when the interval is set, false otherwise</returns>
        private bool IsTimeIntervalSet(TimeSpan? interval) {
            return interval != null && interval.Value != TimeSpan.MinValue;
        }
    }
}