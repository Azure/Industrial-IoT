// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Opc.Ua;

    /// <summary>
    /// Monitored item notification
    /// </summary>
    public sealed record class MonitoredItemNotificationModel
    {
        /// <summary>
        /// Order of the item
        /// </summary>
        public required int Order { get; init; }

        /// <summary>
        /// Identifier of the item that originated the message
        /// </summary>
        public required string MonitoredItemId { get; init; }

        /// <summary>
        /// Id of the field that changed
        /// </summary>
        public required string FieldId { get; init; }

        /// <summary>
        /// Identifier to relate notifications to a value
        /// </summary>
        public uint MessageId => SequenceNumber ?? (uint)GetHashCode();

        /// <summary>
        /// Node Id in string format as configured
        /// </summary>
        public string? NodeId { get; internal set; }

        /// <summary>
        /// Browse path from root folder
        /// </summary>
        public RelativePath? PathFromRoot { get; internal set; }

        /// <summary>
        /// Sequence number
        /// </summary>
        public uint? SequenceNumber { get; set; }

        /// <summary>
        /// Overflow indicator counts the number of messages likely missed
        /// </summary>
        public int Overflow { get; set; }

        /// <summary>
        /// Value of variable change notification
        /// </summary>
        public DataValue? Value { get; set; }

        /// <summary>
        /// Source flags
        /// </summary>
        public MonitoredItemSourceFlags Flags { get; set; }

        /// <summary>
        /// Opaque context
        /// </summary>
        public required object? Context { get; set; }
    }
}
