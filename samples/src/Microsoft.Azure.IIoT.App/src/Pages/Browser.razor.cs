// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages
{
    using global::Azure.IIoT.OpcUa.Models;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Extensions;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.App.Services;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;

    public sealed partial class Browser
    {
        [Parameter]
        public string DiscovererId { get; set; } = string.Empty;

        [Parameter]
        public string EndpointId { get; set; } = string.Empty;

        [Parameter]
        public string ApplicationId { get; set; } = string.Empty;

        [Parameter]
        public string SupervisorId { get; set; } = string.Empty;

        public PagedResult<ListNode> NodeList { get; set; } = new PagedResult<ListNode>();
        public PagedResult<ListNode> PagedNodeList { get; set; } = new PagedResult<ListNode>();
        public PagedResult<ListNode> PublishedNodes { get; set; } = new PagedResult<ListNode>();
        public UsernamePassword Credential { get; set; } = new UsernamePassword();
        public bool IsOpen { get; set; }
        public ListNode NodeData { get; set; }
        public EndpointInfoModel EndpointModel { get; set; }
        private IAsyncDisposable PublishEvent { get; set; }
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";
        private List<string> ParentId { get; set; }
        private enum Drawer
        {
            Action = 0,
            Publisher
        }
        private Drawer DrawerType { get; set; }
        private const int FirstPage = 1;

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page)
        {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            if (!string.IsNullOrEmpty(NodeList.ContinuationToken) && page > PagedNodeList.PageCount)
            {
                await BrowseTreeAsync(BrowseDirection.Forward, 0, false, page).ConfigureAwait(false);
            }
            PagedNodeList = NodeList.GetPaged(page, CommonHelper.PageLength, null);
            foreach (var node in PagedNodeList.Results)
            {
                //fetch the actual value
                if (node.NodeClass == NodeClass.Variable)
                {
                    node.Value = await BrowseManager.ReadValueAsync(EndpointId, node.Id).ConfigureAwait(false);
                }
            }
            CommonHelper.Spinner = string.Empty;
            StateHasChanged();
        }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized()
        {
            CommonHelper.Spinner = "loader-big";
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                EndpointModel = await registryService.GetEndpointAsync(EndpointId).ConfigureAwait(false);
                Credential = await GetSecureItemAsync<UsernamePassword>(CommonHelper.CredentialKey).ConfigureAwait(false);
                await BrowseTreeAsync(BrowseDirection.Forward, 0, true, FirstPage, string.Empty, new List<string>()).ConfigureAwait(false);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(PagedNodeList, ref _tableView, ref _tableEmpty);
                StateHasChanged();
                if (PublishedNodes.Results.Count > 0)
                {
                    PublishEvent = await PublisherServiceEvents.NodePublishSubscribeByEndpointAsync(EndpointId,
                    samples => InvokeAsync(() => GetPublishedNodeDataAsync(samples))).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Browse forward the tree nodes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        private async Task GetTreeAsync(string id, List<string> parentId)
        {
            await BrowseTreeAsync(BrowseDirection.Forward, 0, true, FirstPage, id, parentId).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse backward the tree nodes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        /// <param name="index"></param>
        private async Task GetTreeBackAsync(string id, List<string> parentId, int index)
        {
            await BrowseTreeAsync(BrowseDirection.Backward, index, true, FirstPage, id, parentId).ConfigureAwait(false);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "browser/1/" + DiscovererId + "/" + ApplicationId + "/" + SupervisorId + "/" + EndpointId);
        }

        /// <summary>
        /// Browse the tree nodes
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="index"></param>
        /// <param name="firstPage"></param>
        /// <param name="page"></param>
        /// <param name="id"></param>
        /// <param name="parentId"></param>
        private async Task BrowseTreeAsync(BrowseDirection direction, int index,
            bool firstPage, int page, string id = null, List<string> parentId = null)
        {
            CommonHelper.Spinner = "loader-big";

            if (firstPage)
            {
                ParentId = parentId;
                NodeList = await BrowseManager.GetTreeAsync(EndpointId,
                                            id,
                                            parentId,
                                            DiscovererId,
                                            direction,
                                            index).ConfigureAwait(false);
            }
            else
            {
                NodeList = await BrowseManager.GetTreeNextAsync(EndpointId,
                                                ParentId,
                                                DiscovererId,
                                                NodeList).ConfigureAwait(false);
            }

            PublishedNodes = await Publisher.PublishedAsync(EndpointId, false).ConfigureAwait(false);

            foreach (var node in NodeList.Results)
            {
                if (node.NodeClass == NodeClass.Variable)
                {
                    // check if publishing enabled
                    foreach (var publishedNode in PublishedNodes.Results)
                    {
                        if (node.Id == publishedNode.PublishedItem.NodeId)
                        {
                            node.PublishedItem = publishedNode.PublishedItem;
                            node.Publishing = true;
                            break;
                        }
                    }
                }
            }

            PagedNodeList = NodeList.GetPaged(page, CommonHelper.PageLength, NodeList.Error);
            if (string.IsNullOrEmpty(DiscovererId))
            {
                NavigationManager.NavigateTo(NavigationManager.BaseUri + "browser/" + page + "/" + ApplicationId + "/" + EndpointId);
            }
            else
            {
                NavigationManager.NavigateTo(NavigationManager.BaseUri + "browser/" + page + "/" + DiscovererId + "/" + ApplicationId + "/" + SupervisorId + "/" + EndpointId);
            }
            CommonHelper.Spinner = "";
        }

        /// <summary>
        /// Manage Publishing a node
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="node"></param>
        private async Task SetPublishingAsync(string endpointId, ListNode node)
        {
            if (!node.Publishing)
            {
                await PublishNodeAsync(endpointId, node).ConfigureAwait(false);
            }
            else
            {
                await UnPublishNodeAsync(endpointId, node).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Publish a node
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="node"></param>
        private async Task PublishNodeAsync(string endpointId, ListNode node)
        {
            node.Publishing = true;
            var item = node.PublishedItem;
            var publishingInterval = IsTimeIntervalSet(item?.PublishingInterval) ? item.PublishingInterval : TimeSpan.FromMilliseconds(1000);
            var samplingInterval = IsTimeIntervalSet(item?.SamplingInterval) ? item.SamplingInterval : TimeSpan.FromMilliseconds(1000);
            var heartbeatInterval = IsTimeIntervalSet(item?.HeartbeatInterval) ? item.HeartbeatInterval : null;
            var result = await Publisher.StartPublishingAsync(endpointId, node.Id, node.NodeName, samplingInterval, publishingInterval, heartbeatInterval).ConfigureAwait(false);
            if (result)
            {
                node.PublishedItem = new PublishedItemModel()
                {
                    NodeId = node.Id,
                    PublishingInterval = publishingInterval,
                    SamplingInterval = samplingInterval,
                    HeartbeatInterval = heartbeatInterval
                };
                PublishEvent ??= Task.Run(async () => await PublisherServiceEvents.NodePublishSubscribeByEndpointAsync(EndpointId,
                        samples => InvokeAsync(() => GetPublishedNodeDataAsync(samples))).ConfigureAwait(false)).Result;
            }
            else
            {
                node.Publishing = false;
            }
        }

        /// <summary>
        /// UnPublish a node
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="node"></param>
        private async Task UnPublishNodeAsync(string endpointId, ListNode node)
        {
            var result = await Publisher.StopPublishingAsync(endpointId, node.Id).ConfigureAwait(false);
            if (result)
            {
                node.Publishing = false;
            }
        }

        /// <summary>
        /// Open the Drawer
        /// </summary>
        /// <param name="node"></param>
        /// <param name="type"></param>
        private void OpenDrawer(ListNode node, Drawer type)
        {
            IsOpen = true;
            NodeData = node;
            DrawerType = type;
        }

        /// <summary>
        /// Close the Drawer
        /// </summary>
        private void CloseDrawer()
        {
            IsOpen = false;
            BrowseManager.MethodCallResponse = null;
            StateHasChanged();
        }

        /// <summary>
        /// GetPublishedNodeData
        /// </summary>
        /// <param name="samples"></param>
        private Task GetPublishedNodeDataAsync(MonitoredItemMessageModel samples)
        {
            foreach (var node in PagedNodeList.Results)
            {
                if (node.Id == samples.NodeId)
                {
                    node.Value = samples.Value?.ToJson()?.TrimQuotes();
                    node.Status = string.IsNullOrEmpty(samples.Status) ? "Good" : samples.Status;
                    node.Timestamp = samples.Timestamp.Value.ToLocalTime().ToString(CultureInfo.InvariantCulture);
                    StateHasChanged();
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// ClickHandler
        /// </summary>
        /// <param name="node"></param>
        private async Task ClickHandlerAsync(ListNode node)
        {
            CloseDrawer();
            await PublishNodeAsync(EndpointId, node).ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (PublishEvent != null)
            {
                await PublishEvent.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get Item stored in session storage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        private async Task<T> GetSecureItemAsync<T>(string key)
        {
            var serializedProtectedData = await sessionStorage.GetItemAsync<string>(key).ConfigureAwait(false);
            return secureData.UnprotectDeserialize<T>(serializedProtectedData);
        }

        /// <summary>
        /// Checks whether the time interval is set or not
        /// </summary>
        /// <param name="interval"></param>
        /// <returns>True when the interval is set, false otherwise</returns>
        private static bool IsTimeIntervalSet(TimeSpan? interval)
        {
            return interval != null && interval.Value != TimeSpan.MinValue;
        }
    }
}
