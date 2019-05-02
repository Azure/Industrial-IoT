// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Gds;
using Opc.Ua.Test;
using Xunit;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test
{
    public class ApplicationTestData
    {
        public ApplicationTestData()
        {
            Initialize();
        }

        private void Initialize()
        {
            ApplicationRecord = new ApplicationRecordDataType();
            CertificateGroupId = null;
            CertificateTypeId = null;
            CertificateRequestId = null;
            DomainNames = new StringCollection();
            Subject = null;
            PrivateKeyFormat = "PFX";
            PrivateKeyPassword = "";
            Certificate = null;
            PrivateKey = null;
            IssuerCertificates = null;
        }

        public Application Model;
        public ApplicationRecordDataType ApplicationRecord;
        public NodeId CertificateGroupId;
        public NodeId CertificateTypeId;
        public NodeId CertificateRequestId;
        public StringCollection DomainNames;
        public string Subject;
        public string PrivateKeyFormat;
        public string PrivateKeyPassword;
        public byte[] Certificate;
        public byte[] PrivateKey;
        public byte[][] IssuerCertificates;
        public IList<string> RequestIds;

        /// <summary>
        /// Convert the Server Capability array representation to a comma separated string.
        /// </summary>
        public static string ServerCapabilities(string[] serverCapabilities)
        {
            StringBuilder capabilities = new StringBuilder();
            if (serverCapabilities != null)
            {
                foreach (var capability in serverCapabilities)
                {
                    if (String.IsNullOrEmpty(capability))
                    {
                        continue;
                    }

                    if (capabilities.Length > 0)
                    {
                        capabilities.Append(',');
                    }
                    capabilities.Append(capability);
                }
            }
            return capabilities.ToString();
        }

        /// <summary>
        /// Helper to assert the application model data which should remain equal.
        /// </summary>
        /// <param name="expected">The expected Application model data</param>
        /// <param name="actual">The actualy Application model data</param>
        public static void AssertEqualApplicationModelData(Application expected, Application actual)
        {
            Assert.Equal(expected.ApplicationName, actual.ApplicationName);
            Assert.Equal(expected.ApplicationType, actual.ApplicationType);
            Assert.Equal(expected.ApplicationUri, actual.ApplicationUri);
            Assert.Equal(expected.DiscoveryProfileUri, actual.DiscoveryProfileUri);
            Assert.Equal(expected.ProductUri, actual.ProductUri);
            Assert.Equal(ServerCapabilities(expected), ServerCapabilities(actual));
            Assert.Equal(JsonConvert.SerializeObject(expected.ApplicationNames), JsonConvert.SerializeObject(actual.ApplicationNames));
            Assert.Equal(JsonConvert.SerializeObject(expected.DiscoveryUrls), JsonConvert.SerializeObject(actual.DiscoveryUrls));
        }

        /// <summary>
        /// Normalize and validate the server capabilites.
        /// </summary>
        /// <param name="application">The application with server capabilities.</param>
        /// <returns></returns>
        public static string ServerCapabilities(Application application)
        {
            if ((int)application.ApplicationType != (int)Types.ApplicationType.Client)
            {
                if (application.ServerCapabilities == null || application.ServerCapabilities.Length == 0)
                {
                    throw new ArgumentException("At least one Server Capability must be provided.", nameof(application.ServerCapabilities));
                }
            }

            // TODO validate against specified capabilites.

            StringBuilder capabilities = new StringBuilder();
            if (application.ServerCapabilities != null)
            {
                var sortedCaps = application.ServerCapabilities.Split(",").ToList();
                sortedCaps.Sort();
                foreach (var capability in sortedCaps)
                {
                    if (String.IsNullOrEmpty(capability))
                    {
                        continue;
                    }

                    if (capabilities.Length > 0)
                    {
                        capabilities.Append(',');
                    }

                    capabilities.Append(capability);
                }
            }

            return capabilities.ToString();
        }

        public static Application ApplicationDeepCopy(Application app)
        {
            // serialize/deserialize to avoid using MemberwiseClone
            return (Application)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(app), typeof(Application));
        }

    }

    public class ApplicationTestDataGenerator
    {

        public ApplicationTestDataGenerator(int randomStart = 1)
        {
            _randomStart = randomStart;
            _randomSource = new RandomSource(_randomStart);
            _dataGenerator = new DataGenerator(_randomSource);
            _serverCapabilities = new Opc.Ua.Gds.Client.ServerCapabilities();
        }

        public ApplicationTestData RandomApplicationTestData()
        {
            Opc.Ua.ApplicationType appType = (Opc.Ua.ApplicationType)_randomSource.NextInt32((int)Opc.Ua.ApplicationType.ClientAndServer);
            string pureAppName = _dataGenerator.GetRandomString("en");
            pureAppName = Regex.Replace(pureAppName, @"[^\w\d\s]", "");
            string pureAppUri = Regex.Replace(pureAppName, @"[^\w\d]", "");
            string appName = "UA " + pureAppName;
            StringCollection domainNames = RandomDomainNames();
            string localhost = domainNames[0];
            string privateKeyFormat = _randomSource.NextInt32(1) == 0 ? "PEM" : "PFX";
            string appUri = ("urn:localhost:opcfoundation.org:" + pureAppUri.ToLower()).Replace("localhost", localhost);
            string prodUri = "http://opcfoundation.org/UA/" + pureAppUri;
            StringCollection discoveryUrls = new StringCollection();
            StringCollection serverCapabilities = new StringCollection();
            switch (appType)
            {
                case Opc.Ua.ApplicationType.Client:
                    appName += " Client";
                    break;
                case Opc.Ua.ApplicationType.ClientAndServer:
                    appName += " Client and";
                    goto case Opc.Ua.ApplicationType.Server;
                case Opc.Ua.ApplicationType.Server:
                    appName += " Server";
                    int port = (_dataGenerator.GetRandomInt16() & 0x1fff) + 50000;
                    discoveryUrls = RandomDiscoveryUrl(domainNames, port, pureAppUri);
                    serverCapabilities = RandomServerCapabilities();
                    break;
            }
            ApplicationTestData testData = new ApplicationTestData
            {
                Model = new Application
                {
                    ApplicationUri = appUri,
                    ApplicationName = appName,
                    ApplicationType = (Types.ApplicationType)appType,
                    ProductUri = prodUri,
                    ServerCapabilities = ApplicationTestData.ServerCapabilities(serverCapabilities.ToArray()),
                    ApplicationNames = new ApplicationName[] { new ApplicationName { Locale = "en-us", Text = appName } },
                    DiscoveryUrls = discoveryUrls.ToArray()
                },
                ApplicationRecord = new ApplicationRecordDataType
                {
                    ApplicationNames = new LocalizedTextCollection { new LocalizedText("en-us", appName) },
                    ApplicationUri = appUri,
                    ApplicationType = appType,
                    ProductUri = prodUri,
                    DiscoveryUrls = discoveryUrls,
                    ServerCapabilities = serverCapabilities
                },
                DomainNames = domainNames,
                Subject = string.Format("CN={0},DC={1},O=OPC Foundation", appName, localhost),
                PrivateKeyFormat = privateKeyFormat,
                RequestIds = new List<string>()
            };
            return testData;
        }

        private string RandomLocalHost()
        {
            string localhost = Regex.Replace(_dataGenerator.GetRandomSymbol("en").Trim().ToLower(), @"[^\w\d]", "");
            if (localhost.Length >= 12)
            {
                localhost = localhost.Substring(0, 12);
            }
            return localhost;
        }

        private string[] RandomDomainNames()
        {
            int count = _randomSource.NextInt32(8) + 1;
            string[] result = new string[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = RandomLocalHost();
            }
            return result;
        }

        private StringCollection RandomDiscoveryUrl(StringCollection domainNames, int port, string appUri)
        {
            StringCollection result = new StringCollection();
            foreach (string name in domainNames)
            {
                int random = _randomSource.NextInt32(7);
                if ((result.Count == 0) || (random & 1) == 0)
                {
                    result.Add(string.Format("opc.tcp://{0}:{1}/{2}", name, (port++).ToString(), appUri));
                }
                if ((random & 2) == 0)
                {
                    result.Add(string.Format("http://{0}:{1}/{2}", name, (port++).ToString(), appUri));
                }
                if ((random & 4) == 0)
                {
                    result.Add(string.Format("https://{0}:{1}/{2}", name, (port++).ToString(), appUri));
                }
            }
            return result;
        }

        private StringCollection RandomServerCapabilities()
        {
            var serverCapabilities = new StringCollection();
            int capabilities = _randomSource.NextInt32(8);
            foreach (var cap in _serverCapabilities)
            {
                if (_randomSource.NextInt32(100) > 50)
                {
                    serverCapabilities.Add(cap.Id);
                    if (capabilities-- == 0)
                    {
                        break;
                    }
                }
            }
            return serverCapabilities;
        }

        private int _randomStart = 1;
        private RandomSource _randomSource;
        private DataGenerator _dataGenerator;
        private Opc.Ua.Gds.Client.ServerCapabilities _serverCapabilities;
    }

}
