// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages
{
    using global::Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.AspNetCore.Components;

    public partial class _DrawerAssetContent
    {
        [Parameter]
        public ApplicationInfoModel ApplicationData { get; set; }
    }
}
