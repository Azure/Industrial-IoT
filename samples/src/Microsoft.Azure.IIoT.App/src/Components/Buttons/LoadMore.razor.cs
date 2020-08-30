// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Components.Buttons {
    using System;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Data;

    public partial class LoadMore {
        [Parameter]
        public PagedResultBase Result { get; set; }

        [Parameter]
        public bool IsLoading { get; set; }

        [Parameter]
        public Action LoadMoreItems { get; set; }

        protected void ButtonClicked() {
            LoadMoreItems?.Invoke();
        }
    }
}
