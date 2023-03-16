// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages
{
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.AspNetCore.Components;
    using global::Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading.Tasks;

    public partial class _DrawerPublisherContent
    {
        [Parameter]
        public ListNode NodeData { get; set; }

        [Parameter]
        public EventCallback Onclick { get; set; }

        private ListNodeRequested InputData { get; set; }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized()
        {
            if (NodeData.PublishedItem == null)
            {
                NodeData.PublishedItem = new PublishedItemModel();
            }

            InputData = new ListNodeRequested(NodeData.PublishedItem);
        }

        /// <summary>
        /// Close Drawer and update discovery
        /// </summary>
        /// <param name="discoverer"></param>
        private async Task UpdatePublishedNodeConfigAsync()
        {
            NodeData.TryUpdateData(InputData);
            await Onclick.InvokeAsync(NodeData).ConfigureAwait(false);
        }
    }
}
