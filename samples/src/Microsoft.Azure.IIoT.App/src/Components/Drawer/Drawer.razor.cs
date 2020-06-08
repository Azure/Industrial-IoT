// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Components.Drawer {
    using System;
    using Microsoft.AspNetCore.Components;

    public partial class Drawer {
        [Parameter]
        public string HeaderText { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public Object ObjectData { get; set; }

        [Parameter]
        public Action CloseDrawer { get; set; }

        [Parameter]
        public bool IsOpened { get; set; }

        private string _divClass { get; set; } = "drawer";
        private string _closeIcon { get; set; } = "oi oi-x closebtn";

        private void OpenPanel() {
            _divClass = "drawer drawer-right-open";
            _closeIcon = "oi oi-x closebtn";
        }

        private void ClosePanel() {
            _divClass = "drawer drawer-close";
            _closeIcon = string.Empty;
            IsOpened = false;
            CloseDrawer.Invoke();
        }
    }
}