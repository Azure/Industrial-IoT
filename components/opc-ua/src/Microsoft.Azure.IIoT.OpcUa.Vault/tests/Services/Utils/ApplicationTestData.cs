// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Gds;
    using System.Collections.Generic;
    using Xunit;
    public class ApplicationTestData {
        public ApplicationTestData() {
            Initialize();
        }

        private void Initialize() {
            ApplicationRecord = new ApplicationRecordDataType();
            GroupId = null;
            CertificateType = null;
            CertificateRequestId = null;
            DomainNames = new StringCollection();
            Subject = null;
            PrivateKeyFormat = PrivateKeyFormat.PFX;
            PrivateKeyPassword = "";
            Certificate = null;
            PrivateKey = null;
            IssuerCertificates = null;
        }

        public ApplicationInfoModel Model { get; set; }
        public ApplicationRecordDataType ApplicationRecord { get; set; }
        public NodeId GroupId { get; set; }
        public NodeId CertificateType { get; set; }
        public NodeId CertificateRequestId { get; set; }
        public StringCollection DomainNames { get; set; }
        public string Subject { get; set; }
        public PrivateKeyFormat PrivateKeyFormat { get; set; }
        public string PrivateKeyPassword { get; set; }
        public byte[] Certificate { get; set; }
        public byte[] PrivateKey { get; set; }
        public byte[][] IssuerCertificates { get; set; }
        public IList<string> RequestIds { get; set; }

        /// <summary>
        /// Helper to assert the application model data which should remain equal.
        /// </summary>
        /// <param name="expected">The expected Application model data</param>
        /// <param name="actual">The actualy Application model data</param>
        public static void AssertEqualApplicationModelData(ApplicationInfoModel expected, ApplicationInfoModel actual) {
            Assert.Equal(expected.ApplicationName, actual.ApplicationName);
            Assert.Equal(expected.ApplicationType, actual.ApplicationType);
            Assert.Equal(expected.ApplicationUri, actual.ApplicationUri);
            Assert.Equal(expected.DiscoveryProfileUri, actual.DiscoveryProfileUri);
            Assert.Equal(expected.ProductUri, actual.ProductUri);
            Assert.True(expected.Capabilities.SetEqualsSafe(actual.Capabilities),
                ApplicationDocumentEx.GetServerCapabilitiesAsString(expected.Capabilities) + " != " +
                ApplicationDocumentEx.GetServerCapabilitiesAsString(actual.Capabilities));
            Assert.Equal(JsonConvert.SerializeObject(expected.LocalizedNames), JsonConvert.SerializeObject(actual.LocalizedNames));
            Assert.Equal(JsonConvert.SerializeObject(expected.DiscoveryUrls), JsonConvert.SerializeObject(actual.DiscoveryUrls));
        }

        public static ApplicationInfoModel ApplicationDeepCopy(ApplicationInfoModel app) {
            // serialize/deserialize to avoid using MemberwiseClone
            return (ApplicationInfoModel)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(app), typeof(ApplicationInfoModel));
        }
    }

}
