// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    /// <summary>
    /// Options
    /// </summary>
    public sealed class AvroFileWriterOptions
    {
        /// <summary>
        /// Do not write avro files
        /// </summary>
        public bool Disabled { get; set; }
    }
}
