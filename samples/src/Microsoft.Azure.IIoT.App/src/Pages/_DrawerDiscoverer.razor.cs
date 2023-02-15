// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.AspNetCore.Components;
    using System.Threading.Tasks;

    public partial class _DrawerDiscoverer {
        [Parameter]
        public DiscovererInfo DiscovererData { get; set; }

        [Parameter]
        public EventCallback Onclick { get; set; }

        private DiscovererInfoRequested InputData { get; set; }
        private string DiscoveryUrl { get; set; }
        private string Status { get; set; }
        private string ButtonLabel { get; set; }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized() {
            ButtonLabel = DiscovererData.isAdHocDiscovery ? "Apply & Scan" : "Apply";
            InputData = new DiscovererInfoRequested();
        }

        /// <summary>
        /// Close Drawer and update discovery
        /// </summary>
        private async Task UpdateDiscovererConfigAsync() {
            DiscovererData.TryUpdateData(InputData);
            await Onclick.InvokeAsync(DiscovererData);
            if (!DiscovererData.isAdHocDiscovery) {
                Status = await RegistryHelper.UpdateDiscovererAsync(DiscovererData);
            }
        }
    }
}