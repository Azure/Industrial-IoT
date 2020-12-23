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
        public object ObjectData { get; set; }

        [Parameter]
        public Action CloseDrawer { get; set; }

        [Parameter]
        public bool IsOpen { get; set; }

        private string DivClass { get; set; } = "drawer";
        private string CloseIcon { get; set; } = "oi oi-x closebtn";

        private void OpenPanel() {
            DivClass = "drawer drawer-right-open";
            CloseIcon = "oi oi-x closebtn";
        }

        private void ClosePanel() {
            DivClass = "drawer drawer-close";
            CloseIcon = string.Empty;
            IsOpen = false;
            CloseDrawer.Invoke();
        }
    }
}