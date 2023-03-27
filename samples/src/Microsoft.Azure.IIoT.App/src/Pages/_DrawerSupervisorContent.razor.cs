// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages
{
    using Microsoft.AspNetCore.Components;
    using System.Threading.Tasks;

#pragma warning disable CA1707 // Identifiers should not contain underscores
    public partial class _DrawerSupervisorContent
#pragma warning restore CA1707 // Identifiers should not contain underscores
    {
        [Parameter]
        public string SupervisorId { get; set; }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized()
        {
            CommonHelper.Spinner = "loader-big";
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                CommonHelper.Spinner = "";
                StateHasChanged();
            }
            return Task.CompletedTask;
        }
    }
}
