// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System.Threading.Tasks;

    public partial class _DrawerSupervisorContent {
        [Parameter]
        public string SupervisorId { get; set; }

        public SupervisorStatusApiModel SupervisorStatus { get; set; }

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
                SupervisorStatus = await RegistryHelper.GetSupervisorStatusAsync(SupervisorId);
                CommonHelper.Spinner = "";
                StateHasChanged();
            }
        }
    }
}