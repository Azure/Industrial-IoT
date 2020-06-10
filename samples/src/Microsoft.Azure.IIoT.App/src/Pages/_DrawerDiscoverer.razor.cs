// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Models;

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
            if (DiscovererData.isAdHocDiscovery) {
                ButtonLabel = "Apply & Scan";
            }
            else {
                ButtonLabel = "Apply";
            }
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