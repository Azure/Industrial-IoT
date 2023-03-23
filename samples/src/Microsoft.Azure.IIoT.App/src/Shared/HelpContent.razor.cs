// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Shared
{
    using Microsoft.AspNetCore.Components;
    using Blazored.Modal;
    using Blazored.Modal.Services;

    public partial class HelpContent
    {
        [CascadingParameter] private BlazoredModalInstance BlazoredModal { get; set; }

        private void Close()
        {
            BlazoredModal.Close(ModalResult.Ok(true));
        }
    }
}
