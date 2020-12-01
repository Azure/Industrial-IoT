// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Linq;

namespace Microsoft.Azure.IIoT.Deployment.Resources {

    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;

    static class Scripts {

        /// <summary>
        /// Content of jumpbox.sh bash script.
        /// </summary>
        public static string JumpboxSh {
            get {
                // Relative resource path of jumpbox.sh
                const string jumpboxRelPath = "scripts.jumpbox.sh";

                var assembly = Assembly.GetExecutingAssembly();
                var embeddedResourceNames = assembly.GetManifestResourceNames();
                var jumpboxResourceName = $"{typeof(Scripts).Namespace}.{jumpboxRelPath}";

                if (!embeddedResourceNames.Contains(jumpboxResourceName)) {
                    throw new Exception($"Assembly embedded resources do not contain '{jumpboxResourceName}'.");
                }

                using (var stream = assembly.GetManifestResourceStream(jumpboxResourceName))
                using (var reader = new StreamReader(stream, Encoding.UTF8)) {
                    var content = reader.ReadToEnd();
                    return content;
                }
            }
        }
    }
}
