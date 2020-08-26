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
        public string DiscovererId { get; set; } = string.Empty;

        [Parameter]
        public string ApplicationId { get; set; } = string.Empty;

        [Parameter]
        public string SupervisorId { get; set; } = string.Empty;

        public EndpointInfo EndpointData { get; set; }

        protected override async Task GetItems(bool getNextPage) {
            Items = await RegistryHelper.GetEndpointListAsync(DiscovererId, ApplicationId, SupervisorId, Items, getNextPage);
        }

        protected override async Task SubscribeEvents() {
            _events = await RegistryServiceEvents.SubscribeEndpointEventsAsync(
                    ev => InvokeAsync(() => EndpointEvent(ev)));
        }

        private Task EndpointEvent(EndpointEventApiModel ev) {
            Items.Results.Update(ev);
            StateHasChanged();
            return Task.CompletedTask;
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

        // <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        private void OpenDrawer(EndpointInfo endpoint) {
            IsOpen = true;
            EndpointData = endpoint;
        }
    }
}