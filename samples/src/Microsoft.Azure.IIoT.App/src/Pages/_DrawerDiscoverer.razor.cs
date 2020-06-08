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

        private DiscovererInfoRequested _inputData { get; set; }
        private string _discoveryUrl { get; set; }
        private string _status { get; set; }
        private string _buttonLabel { get; set; }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized() {
            if (DiscovererData.isAdHocDiscovery) {
                _buttonLabel = "Apply & Scan";
            }
            else {
                _buttonLabel = "Apply";
            }
            _inputData = new DiscovererInfoRequested();
        }

        /// <summary>
        /// Close Drawer and update discovery
        /// </summary>
        private async Task UpdateDiscovererConfigAsync() {
            DiscovererData.TryUpdateData(_inputData);
            await Onclick.InvokeAsync(DiscovererData);
            if (!DiscovererData.isAdHocDiscovery) {
                _status = await RegistryHelper.UpdateDiscovererAsync(DiscovererData);
            }
        }
    }
}