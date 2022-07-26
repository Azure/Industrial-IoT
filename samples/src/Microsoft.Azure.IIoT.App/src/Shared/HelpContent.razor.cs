// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Shared {
    using Blazored.Modal;
    using Blazored.Modal.Services;
    using Microsoft.AspNetCore.Components;

    public partial class HelpContent {
        [CascadingParameter] BlazoredModalInstance BlazoredModal { get; set; }

        void Close() => BlazoredModal.Close(ModalResult.Ok(true));
    }
}