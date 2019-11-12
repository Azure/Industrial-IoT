// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher
{
    /// <summary>
    /// Interface to control the telemetry publish, name and pattern properties.
    /// </summary>
    public interface ITelemetrySettings
    {
        /// <summary>
        /// Flag to control if the value should be published.
        /// </summary>
        bool? Publish { get; set; }

        /// <summary>
        /// The name under which the telemetry value should be published.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The pattern which should be applied to the telemetry value.
        /// </summary>
        string Pattern { get; set; }

        /// <summary>
        /// Method to apply the regex to the given value if one is defined, otherwise we return the string passed in.
        /// </summary>
        string PatternMatch(string stringToParse);
    }
}
