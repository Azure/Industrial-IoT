// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Browser.Helpers {
    using System;


    public static class NodeParseHelper {
        public static readonly string[] delimiter = { "__$__" };

        public static string Parse(string jstreeNode, out string parentNode) {
            // This delimiter is used to allow the storing of the OPC UA parent node ID
            // together with the OPC UA child node ID in jstree data structures and provide
            // it as parameter to Ajax calls.

            var jstreeNodeSplit = jstreeNode.Split(delimiter, 3, StringSplitOptions.None);
            switch (jstreeNodeSplit.Length) {
                case 1:
                    parentNode = null;
                    return jstreeNodeSplit[0];
                case 2:
                    parentNode = jstreeNodeSplit[0];
                    return jstreeNodeSplit[1];
                default:
                    throw new ArgumentException("bad jstreenode string");
            }
        }

        public static string Format(string nodeId, string parentNode) {
            return $"__{nodeId}{delimiter[0]}{parentNode}";
        }
    }
}
