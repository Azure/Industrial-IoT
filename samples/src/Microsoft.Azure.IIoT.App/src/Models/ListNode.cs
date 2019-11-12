// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using System.Collections.Generic;
    public class ListNode {
        public string Id { get; set; }
        public string NodeClass { get; set; }
        public string AccessLevel { get; set; }
        public string Executable { get; set; }
        public string EventNotifier { get; set; }
        public string nextParentId { get; set; }
        public string parentName { get; set; }
        public bool children { get; set; }
        public string ImageUrl { get; set; }
        public string nodeName { get; set; }
        public string supervisorId { get; set; }

        public List<string> ParentIdList { get; set; }

        public ListNode() {
            ParentIdList = new List<string>();
        }
    }
}
