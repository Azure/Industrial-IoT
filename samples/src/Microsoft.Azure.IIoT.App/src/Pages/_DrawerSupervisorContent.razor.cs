// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using Microsoft.AspNetCore.Components;
    using global::Azure.IIoT.OpcUa.Api.Models;
    using System.Threading.Tasks;

    public partial class _DrawerSupervisorContent {
        [Parameter]
        public string SupervisorId { get; set; }

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
        protected override Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                CommonHelper.Spinner = "";
                StateHasChanged();
            }
            return Task.CompletedTask;
        }
    }
}