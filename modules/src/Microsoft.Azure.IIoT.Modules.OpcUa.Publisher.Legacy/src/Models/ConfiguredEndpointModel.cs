// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models
{
    /// <summary>
    /// Model for configured endpoint response element.
    /// </summary>
    public class ConfiguredEndpointModel
    {
        public ConfiguredEndpointModel(string endpointUrl)
        {
            EndpointUrl = endpointUrl;
        }

        public string EndpointUrl { get; set; }
    }
}
