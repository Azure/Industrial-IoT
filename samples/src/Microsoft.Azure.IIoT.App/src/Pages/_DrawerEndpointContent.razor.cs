// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System.Threading.Tasks;

    public partial class _DrawerEndpointContent {
        [Parameter]
        public EndpointInfo EndpointData { get; set; }
        public ApplicationRegistrationApiModel Application{ get; set; }

        public bool IsLoading { get; set; }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized() {
            IsLoading = true;
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                Application = await RegistryService.GetApplicationAsync(EndpointData.EndpointModel.ApplicationId);
                IsLoading = false;
                StateHasChanged();
            }
        }   
    }
}
