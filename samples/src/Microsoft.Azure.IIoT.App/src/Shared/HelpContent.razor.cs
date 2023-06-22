// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Shared
{
    using Microsoft.AspNetCore.Components;
    using Blazored.Modal;
    using Blazored.Modal.Services;
    using System.Threading.Tasks;

    public partial class HelpContent
    {
        [CascadingParameter] private BlazoredModalInstance BlazoredModal { get; set; }

        private Task CloseAsync()
        {
            return BlazoredModal.CloseAsync(ModalResult.Ok(true));
        }
    }
}
