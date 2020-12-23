// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.OpenApi {
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    /// <summary>
    /// Add extensions for autorest to schemas
    /// </summary>
    internal class ApiVersionExtensions : IDocumentFilter {

        /// <inheritdoc/>
        public void Apply(OpenApiDocument doc, DocumentFilterContext context) {
            var paths = new OpenApiPaths();
            foreach (var path in doc.Paths) {
                paths.Add(path.Key.Replace("v{version}", doc.Info.Version), path.Value);
            }
            doc.Paths = paths;
        }
    }
}

