// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;

    public partial class PublishedNodes {
        [Parameter]
        public string Page { get; set; } = "1";

        [Parameter]
        public string EndpointId { get; set; } = string.Empty;

        [Parameter]
        public string DiscovererId { get; set; } = string.Empty;

        [Parameter]
        public string ApplicationId { get; set; } = string.Empty;

        [Parameter]
        public string SupervisorId { get; set; } = string.Empty;

        public string Status { get; set; }
        private PagedResult<ListNode> NodeList { get; set; } =
            new PagedResult<ListNode>();
        private PagedResult<ListNode> PagedNodeList { get; set; } =
            new PagedResult<ListNode>();
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";
        private IAsyncDisposable PublishEvent { get; set; }
        private const string _valueGood = "Good";

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page) {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            if (!string.IsNullOrEmpty(NodeList.ContinuationToken) && page > PagedNodeList.PageCount) {
                NodeList = await PublisherHelper.PublishedAsync(EndpointId, true);
            }
            PagedNodeList = NodeList.GetPaged(page, CommonHelper.PageLength, null);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "PublishedNodes/" + page + "/" + EndpointId);
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
                NodeList = await PublisherHelper.PublishedAsync(EndpointId, true);
                Page = "1";
                PagedNodeList = NodeList.GetPaged(int.Parse(Page), CommonHelper.PageLength, NodeList.Error);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(PagedNodeList, ref _tableView, ref _tableEmpty);
                StateHasChanged();
                PublishEvent = await PublisherServiceEvents.NodePublishSubscribeByEndpointAsync(EndpointId,
                samples => InvokeAsync(() => GetPublishedNodeData(samples)));
            }
        }

        private bool IsIdGiven(string id) {
            return !string.IsNullOrEmpty(id);
        }

        /// <summary>
        /// GetPublishedNodeData
        /// </summary>
        /// <param name="samples"></param>
        private Task GetPublishedNodeData(MonitoredItemMessageApiModel samples) {
            foreach (var node in PagedNodeList.Results) {
                if (node.PublishedItem.NodeId == samples.NodeId) {
                    node.Value = samples.Value?.ToJson()?.TrimQuotes();
                    node.Status = string.IsNullOrEmpty(samples.Status) ? _valueGood : samples.Status;
                    node.Timestamp = samples.Timestamp.Value.ToLocalTime().ToString();
                    StateHasChanged();
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public async void Dispose() {
            if (PublishEvent != null) {
                await PublishEvent.DisposeAsync();
            }
        }
    }
}