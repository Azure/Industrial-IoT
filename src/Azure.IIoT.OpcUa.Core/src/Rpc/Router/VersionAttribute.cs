// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Router
{
    using System;

    /// <summary>
    /// Attribute to version a controller implementation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class VersionAttribute : Attribute
    {
        /// <summary>
        /// Return string version
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Create versioning attribute
        /// </summary>
        /// <param name="value"></param>
        public VersionAttribute(string value)
        {
            Value = value;
        }
    }
}
