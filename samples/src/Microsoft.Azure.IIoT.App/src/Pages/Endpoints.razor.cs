// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.App.Models;

    public partial class Endpoints {
        [Parameter]
        public string Page { get; set; } = "1";

        [Parameter]
        public string DiscovererId { get; set; } = string.Empty;

        [Parameter]
        public string ApplicationId { get; set; } = string.Empty;

        [Parameter]
        public string SupervisorId { get; set; } = string.Empty;

        public string Status { get; set; }
        private PagedResult<EndpointInfo> EndpointList { get; set; } =
            new PagedResult<EndpointInfo>();
        private PagedResult<EndpointInfo> PagedendpointList { get; set; } =
            new PagedResult<EndpointInfo>();
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";
        private IAsyncDisposable _endpointEvents;

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page) {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            if (!string.IsNullOrEmpty(EndpointList.ContinuationToken) && page > PagedendpointList.PageCount) {
                EndpointList = await RegistryHelper.GetEndpointListAsync(DiscovererId, ApplicationId, SupervisorId, EndpointList);
            }
            PagedendpointList = EndpointList.GetPaged(page, CommonHelper.PageLength, null);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "endpoints/" + page + "/" + DiscovererId + "/" + ApplicationId + "/" + SupervisorId);
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
                EndpointList = await RegistryHelper.GetEndpointListAsync(DiscovererId, ApplicationId, SupervisorId);
                Page = "1";
                PagedendpointList = EndpointList.GetPaged(int.Parse(Page), CommonHelper.PageLength, EndpointList.Error);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(PagedendpointList, ref _tableView, ref _tableEmpty);
                StateHasChanged();

                _endpointEvents = await RegistryServiceEvents.SubscribeEndpointEventsAsync(
                    ev => InvokeAsync(() => EndpointEvent(ev)));
            }
        }

        private Task EndpointEvent(EndpointEventApiModel ev) {
            EndpointList.Results.Update(ev);
            PagedendpointList = EndpointList.GetPaged(int.Parse(Page), CommonHelper.PageLength, EndpointList.Error);
            StateHasChanged();
            return Task.CompletedTask;
        }

        public async void Dispose() {
            if (_endpointEvents != null) {
                await _endpointEvents.DisposeAsync();
            }
        }

        private bool IsEndpointSeen(EndpointInfo endpoint) {
            return endpoint.EndpointModel?.NotSeenSince == null;
        }

        private bool IsIdGiven(string id) {
            return !string.IsNullOrEmpty(id) && id != RegistryHelper.PathAll;
        }

        /// <summary>
        /// Checks whether the endpoint is activated
        /// </summary>
        /// <param name="endpoint">The endpoint info</param>
        /// <returns>True if the endpoint is activated, false otherwise</returns>
        private bool IsEndpointActivated(EndpointInfo endpoint) {
            return endpoint.EndpointModel.ActivationState == EndpointActivationState.Activated ||
                 endpoint.EndpointModel.ActivationState == EndpointActivationState.ActivatedAndConnected;
        }

        /// <summary>
        /// Creates a css string for an endpoint row based on activity and availability of the endpoint
        /// </summary>
        /// <param name="endpoint">The endpoint info</param>
        /// <returns>The css string</returns>
        private string GetEndpointVisibilityString(EndpointInfo endpoint) {
            if (!IsEndpointSeen(endpoint)) {
                return "enabled-false";
            }
            else if (IsEndpointActivated(endpoint)) {
                return "enabled-true activated-true";
            }
            else {
                return "enabled-true";
            }
        }

        /// <summary>
        /// Activate or deactivate an endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="checkedValue"></param>
        /// <returns></returns>
        private async Task SetActivationAsync(EndpointInfo endpoint) {
            string endpointId = endpoint.EndpointModel.Registration.Id;

            if (!IsEndpointActivated(endpoint)) {
                try {
                    await RegistryService.ActivateEndpointAsync(endpointId);
                }
                catch (Exception e) {
                    if (e.Message.Contains("404103")) {
                        Status = "The endpoint is not available.";
                    }
                    else {
                        Status = e.Message;
                    }
                }
            }
            else {
                try {
                    await RegistryService.DeactivateEndpointAsync(endpointId);
                }
                catch (Exception e) {
                    Status = e.Message;
                }
            }
        }
    }
}