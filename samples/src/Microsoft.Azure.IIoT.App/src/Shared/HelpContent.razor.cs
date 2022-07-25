// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Shared {
    using Blazored.Modal;
    using Blazored.Modal.Services;
    using Microsoft.AspNetCore.Components;
    using System.Threading.Tasks;

    public partial class HelpContent {
        [CascadingParameter] BlazoredModalInstance BlazoredModal { get; set; }

        Task Close() => BlazoredModal.CloseAsync(ModalResult.Ok(true));
    }
}