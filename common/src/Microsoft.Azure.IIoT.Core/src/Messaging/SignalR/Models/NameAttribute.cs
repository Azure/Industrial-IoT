// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR {
    using System;
    using System.Reflection;

    /// <summary>
    /// Name of the hub
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class NameAttribute : Attribute {

        /// <summary>
        /// Create attribute
        /// </summary>
        /// <param name="name"></param>
        public NameAttribute(string name) {
            Name = name;
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get name of hub with type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetName(Type type) {
            var name = type.GetCustomAttribute<NameAttribute>(false)?.Name;
            if (string.IsNullOrEmpty(name)) {
                name = type.Name.ToLowerInvariant();
                if (name.EndsWith(nameof(Hub), StringComparison.OrdinalIgnoreCase)) {
                    name = name.Replace(nameof(Hub), "", StringComparison.OrdinalIgnoreCase);
                }
            }
            return name;
        }
    }
}
