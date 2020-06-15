// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Components.Pager {
    using System;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.AspNetCore.Components;

    public partial class Pager {
        [Parameter]
        public PagedResultBase Result { get; set; }

        [Parameter]
        public Action<int> PageChanged { get; set; }

        protected int StartIndex { get; private set; } = 0;
        protected int FinishIndex { get; private set; } = 0;

        protected override void OnParametersSet() {
            StartIndex = Math.Max(Result.CurrentPage - 10, 1);
            FinishIndex = Math.Min(Result.CurrentPage + 10, Result.PageCount);

            base.OnParametersSet();
        }

        protected void PagerButtonClicked(int page) {
            PageChanged?.Invoke(page);
        }
    }
}