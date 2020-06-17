// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Data;
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
        private PagedResult<PublishedItemApiModel> NodeList { get; set; } =
            new PagedResult<PublishedItemApiModel>();
        private PagedResult<PublishedItemApiModel> PagedNodeList { get; set; } =
            new PagedResult<PublishedItemApiModel>();
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page) {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            if (!string.IsNullOrEmpty(NodeList.ContinuationToken) && page > PagedNodeList.PageCount) {
                NodeList = await PublisherHelper.PublishedAsync(EndpointId);
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
                NodeList = await PublisherHelper.PublishedAsync(EndpointId);
                Page = "1";
                PagedNodeList = NodeList.GetPaged(int.Parse(Page), CommonHelper.PageLength, NodeList.Error);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(PagedNodeList, ref _tableView, ref _tableEmpty);
                StateHasChanged();
            }
        }

        private bool IsIdGiven(string id) {
            return !string.IsNullOrEmpty(id);
        }
    }
}