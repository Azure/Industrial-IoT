// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.App.Models;

    public partial class Publishers {

        public PublisherInfo Publisher { get; set; }

        protected override async Task GetItems(bool getNextPage) {
            Items = await RegistryHelper.GetPublisherListAsync(Items, getNextPage);
        }

        protected override async Task SubscribeEvents() {
            _events = await RegistryServiceEvents.SubscribePublisherEventsAsync(
                    ev => InvokeAsync(() => PublisherEvent(ev)));
        }

        private Task PublisherEvent(PublisherEventApiModel ev) {
            Items.Results.Update(ev);
            StateHasChanged();
            return Task.CompletedTask;
        }

        private bool IsTimeIntervalSet(TimeSpan? interval) {
            return interval != null && interval.Value != TimeSpan.MinValue;
        }

        /// <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        private void OpenDrawer(PublisherApiModel publisherModel) {
            IsOpen = true;
            Publisher = new PublisherInfo { PublisherModel = publisherModel };
        }

        /// <summary>
        /// ClickHandler
        /// </summary>
        private void ClickHandler() {
            CloseDrawer();
        }
    }
}