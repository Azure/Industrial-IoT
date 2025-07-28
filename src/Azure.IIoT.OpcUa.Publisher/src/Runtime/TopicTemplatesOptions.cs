// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    /// <summary>
    /// Topic templates
    /// </summary>
    public sealed record class TopicTemplatesOptions
    {
        /// <summary>
        /// Root topic template
        /// </summary>
        public string? Root { get; set; }

        /// <summary>
        /// Method topic template
        /// </summary>
        public string? Method { get; set; }

        /// <summary>
        /// Events topic template
        /// </summary>
        public string? Events { get; set; }

        /// <summary>
        /// Diagnostics topic template
        /// </summary>
        public string? Diagnostics { get; set; }

        /// <summary>
        /// Telemetry topic template
        /// </summary>
        public string? Telemetry { get; set; }

        /// <summary>
        /// Default metadata queue name
        /// </summary>
        public string? DataSetMetaData { get; set; }

        /// <summary>
        /// Default schema topic when schemas are published
        /// </summary>
        public string? Schema { get; set; }
    }
}
