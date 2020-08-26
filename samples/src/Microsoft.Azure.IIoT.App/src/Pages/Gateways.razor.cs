// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;

    public partial class Gateways {
        protected override async Task GetItems(bool getNextPage) {
            Items = await RegistryHelper.GetGatewayListAsync(Items, getNextPage);
        }

        protected override async Task SubscribeEvents() {
            _events = await RegistryServiceEvents.SubscribeGatewayEventsAsync(
                    ev => InvokeAsync(() => GatewayEvent(ev)));
        }

        private Task GatewayEvent(GatewayEventApiModel ev) {
            Items.Results.Update(ev);
            StateHasChanged();
            return Task.CompletedTask;
        }
    }
}