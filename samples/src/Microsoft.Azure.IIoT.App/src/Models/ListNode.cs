// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using System.Collections.Generic;
    public class ListNode {
        public string Id { get; set; }
        public NodeClass NodeClass { get; set; }
        public NodeAccessLevel AccessLevel { get; set; }
        public string Executable { get; set; }
        public NodeEventNotifier EventNotifier { get; set; }
        public string NextParentId { get; set; }
        public string ParentName { get; set; }
        public bool Children { get; set; }
        public string ImageUrl { get; set; }
        public string NodeName { get; set; }
        public string SupervisorId { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }

        public List<string> ParentIdList { get; set; }

        public ListNode() {
            ParentIdList = new List<string>();
        }
    }
}
