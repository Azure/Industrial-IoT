// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;

    public partial class Applications {

        public ApplicationInfoApiModel ApplicationData { get; set; }
      
        protected override async Task GetItems(bool getNextPage) {
            Items = await RegistryHelper.GetApplicationListAsync(Items, getNextPage);
        }

        protected override async Task SubscribeEvents() {
            _events = await RegistryServiceEvents.SubscribeApplicationEventsAsync(
                    async data => {
                        await InvokeAsync(() => ApplicationEvent(data));
                    });
        }

        /// <summary>
        /// Unregister application and remove it from UI
        /// </summary>
        /// <param name="applicationId"></param>
        private async Task UnregisterApplicationUIAsync(string applicationId) {
            var index = Items.Results.Select(t => t.ApplicationId).ToList().IndexOf(applicationId);
            Items.Results.RemoveAt(index);
            StateHasChanged();

            Status = await RegistryHelper.UnregisterApplicationAsync(applicationId);
        }

        // <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        private void OpenDrawer(ApplicationInfoApiModel application) {
            IsOpen = true;
            ApplicationData = application;
        }

        /// <summary>
        /// Action on ApplicationEvent
        /// </summary>
        /// <param name="ev"></param>
        private Task ApplicationEvent(ApplicationEventApiModel ev) {
            Items.Results.Update(ev);
            CheckErrorOrEmpty();
            StateHasChanged();
            return Task.CompletedTask;
        }
    }
}