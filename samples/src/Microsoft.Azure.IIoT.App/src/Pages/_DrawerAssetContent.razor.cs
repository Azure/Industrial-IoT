// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.AspNetCore.Components;

    public partial class _DrawerAssetContent {
        [Parameter]
        public ApplicationInfoApiModel ApplicationData { get; set; }
    }
}
