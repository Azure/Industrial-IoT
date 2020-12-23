// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Shared {
    public partial class WorkbookLink {
        private string Link { get; set; } = null;

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized() {
            CreateWorkbookLink();
        }

        /// <summary>
        /// Create workbook link
        /// </summary>
        private void CreateWorkbookLink() {
            if (!string.IsNullOrEmpty(Configuration.WorkbookId)) {
                Link = "https://portal.azure.com/#@" + Configuration.TenantId +
                    "/resource/subscriptions/" + Configuration.SubscriptionId +
                    "/resourceGroups/" + Configuration.ResourceGroup +
                    "/providers/microsoft.insights/workbooks/" +
                    Configuration.WorkbookId + "/workbook";
            }
        }
    }
}