// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Shared {
    public partial class Help {
        private void OpenModal() {
            Modal.Show<HelpContent>("Engineering Tool help page");
        }
    }
}