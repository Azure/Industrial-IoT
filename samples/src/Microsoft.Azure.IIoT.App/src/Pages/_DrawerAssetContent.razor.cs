// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages
{
    using Microsoft.AspNetCore.Components;
    using global::Azure.IIoT.OpcUa.Publisher.Models;

#pragma warning disable CA1707 // Identifiers should not contain underscores
    public partial class _DrawerAssetContent
#pragma warning restore CA1707 // Identifiers should not contain underscores
    {
        [Parameter]
        public ApplicationInfoModel ApplicationData { get; set; }
    }
}
