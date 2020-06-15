// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Components.ErrorMessage {
    using Microsoft.AspNetCore.Components;

    public partial class ErrorMessage {

        [Parameter]
        public string PageError { get; set; }

        [Parameter]
        public string Status { get; set; }

        public void CloseErrorMessage() {
            PageError = null;
            Status = null;
        }
    }
}