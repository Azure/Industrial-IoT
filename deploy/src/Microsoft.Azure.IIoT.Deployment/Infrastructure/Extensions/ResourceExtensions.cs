// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure.Extensions {

    using Microsoft.Azure.Management.ResourceManager.Fluent;

    public static class ResourceExtensions {

        /// <summary>
        /// Checks whether Azure resource is managed by Microsoft.Azure.IIoT.Deployment application.
        ///
        /// This will be done by checking value of "managed-by" tag if it is present.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>True if resource is managed by Microsoft.Azure.IIoT.Deployment, False otherwise.</returns>
        public static bool ManagedByIIoTDeployment(this Resource resource) {
            // Check if resource tags indicate that it is managed by Microsoft.Azure.IIoT.Deployment.
            if (null != resource.Tags
                && resource.Tags.ContainsKey(Resources.IIoTDeploymentTags.KEY_MANAGED_BY)
                && resource.Tags[Resources.IIoTDeploymentTags.KEY_MANAGED_BY] == Resources.IIoTDeploymentTags.VALUE_MANAGED_BY_IIOT
            ) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check whether Azure resource part of "industrial-iot" application.
        ///
        /// This will be done by checking value of "application" tag if it is present.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>True if resource is part of "industrial-iot" application, False othersize.</returns>
        public static bool PartOfIIoTApplication(this Resource resource) {
            // Check if resource tags indicate that it is part of "industrial-iot" application.
            if (null != resource.Tags
                && resource.Tags.ContainsKey(Resources.IIoTDeploymentTags.KEY_APPLICATION)
                && resource.Tags[Resources.IIoTDeploymentTags.KEY_APPLICATION] == Resources.IIoTDeploymentTags.VALUE_APPLICATION_IIOT
            ) {
                return true;
            }

            return false;
        }
    }
}
