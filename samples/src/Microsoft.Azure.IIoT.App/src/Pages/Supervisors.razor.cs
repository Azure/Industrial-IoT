// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;

    public partial class Supervisors {
        public string SupervisorId { get; set; }

        protected override async Task GetItems(bool getNextPage) {
            Items = await RegistryHelper.GetSupervisorListAsync(Items, getNextPage);
        }

        protected override async Task SubscribeEvents() {
            _events = await RegistryServiceEvents.SubscribeSupervisorEventsAsync(
                    async data => {
                        await InvokeAsync(() => SupervisorEvent(data));
                    });
        }

        // <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        private void OpenDrawer(string supervisorId) {
            IsOpen = true;
            SupervisorId = supervisorId;
        }

        /// <summary>
        /// Reset Supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        private async Task ResetSupervisorUIAsync(string supervisorId) {
            Status = await RegistryHelper.ResetSupervisorAsync(supervisorId);
        }

        /// <summary>
        /// action on Supervisor Event
        /// </summary>
        /// <param name="ev"></param>
        private Task SupervisorEvent(SupervisorEventApiModel ev) {
            Items.Results.Update(ev);
            StateHasChanged();
            return Task.CompletedTask;
        }
    }
}