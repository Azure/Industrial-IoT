// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Components.Loader {
    using Microsoft.AspNetCore.Components;

    public partial class Spinner {
        [Parameter]
        public bool IsLoading { get; set;  }
    }
}
