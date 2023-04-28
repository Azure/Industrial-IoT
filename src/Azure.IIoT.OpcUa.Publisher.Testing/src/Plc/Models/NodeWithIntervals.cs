// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc.PluginNodes.Models
{
    using Opc.Ua;

    public sealed class NodeWithIntervals
    {
        public string NodeId { get; set; }
        public string NodeIdTypePrefix { get; set; } = "s";
        public string Namespace { get; set; }
        public uint PublishingInterval { get; set; }
        public uint SamplingInterval { get; set; }

        internal static string GetPrefix(IdType idType)
        {
            switch (idType)
            {
                case IdType.Numeric:
                    return "i";
                case IdType.Guid:
                    return "g";
                case IdType.Opaque:
                    return "b";
                default:
                    return "s";
            }
        }
    }
}
