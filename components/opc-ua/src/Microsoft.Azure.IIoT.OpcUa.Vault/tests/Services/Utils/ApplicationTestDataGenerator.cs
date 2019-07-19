// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Registry.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Opc.Ua;
    using Opc.Ua.Gds;
    using Opc.Ua.Test;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class ApplicationTestDataGenerator {

        public ApplicationTestDataGenerator(int randomStart = 1) {
            _randomStart = randomStart;
            _randomSource = new RandomSource(_randomStart);
            _dataGenerator = new DataGenerator(_randomSource);
            _serverCapabilities = new Opc.Ua.Gds.Client.ServerCapabilities();
        }

        public ApplicationTestData RandomApplicationTestData() {
            var appType = (Opc.Ua.ApplicationType)_randomSource.NextInt32((int)Opc.Ua.ApplicationType.ClientAndServer);
            var pureAppName = _dataGenerator.GetRandomString("en");
            pureAppName = Regex.Replace(pureAppName, @"[^\w\d\s]", "");
            var pureAppUri = Regex.Replace(pureAppName, @"[^\w\d]", "");
            var appName = "UA " + pureAppName;
            StringCollection domainNames = RandomDomainNames();
            var localhost = domainNames[0];
            var privateKeyFormat = _randomSource.NextInt32(1) == 0 ? PrivateKeyFormat.PEM : PrivateKeyFormat.PFX;
            var appUri = ("urn:localhost:opcfoundation.org:" + pureAppUri.ToLower()).Replace("localhost", localhost);
            var prodUri = "http://opcfoundation.org/UA/" + pureAppUri;
            var discoveryUrls = new StringCollection();
            var serverCapabilities = new StringCollection();
            switch (appType) {
                case Opc.Ua.ApplicationType.Client:
                    appName += " Client";
                    break;
                case Opc.Ua.ApplicationType.ClientAndServer:
                    appName += " Client and";
                    goto case Opc.Ua.ApplicationType.Server;
                case Opc.Ua.ApplicationType.Server:
                    appName += " Server";
                    var port = (_dataGenerator.GetRandomInt16() & 0x1fff) + 50000;
                    discoveryUrls = RandomDiscoveryUrl(domainNames, port, pureAppUri);
                    serverCapabilities = RandomServerCapabilities();
                    break;
            }
            var testData = new ApplicationTestData {
                Model = new ApplicationInfoModel {
                    SupervisorId = "test",
                    SiteId = "test",
                    ApplicationUri = appUri,
                    ApplicationName = appName,
                    ApplicationType = (Models.ApplicationType)appType,
                    ProductUri = prodUri,
                    Capabilities = serverCapabilities.ToHashSet(),
                    LocalizedNames = new Dictionary<string, string> {
                        ["en-us"] = appName
                    },
                    DiscoveryUrls = discoveryUrls.ToHashSet()
                },
                ApplicationRecord = new ApplicationRecordDataType {
                    ApplicationNames = new LocalizedTextCollection {
                        new LocalizedText("en-us", appName)
                    },
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

        private string RandomLocalHost() {
            var localhost = Regex.Replace(_dataGenerator.GetRandomSymbol("en").Trim().ToLower(), @"[^\w\d]", "");
            if (localhost.Length >= 12) {
                localhost = localhost.Substring(0, 12);
            }
            return localhost;
        }

        private string[] RandomDomainNames() {
            var count = _randomSource.NextInt32(8) + 1;
            var result = new string[count];
            for (var i = 0; i < count; i++) {
                result[i] = RandomLocalHost();
            }
            return result;
        }

        private StringCollection RandomDiscoveryUrl(StringCollection domainNames, int port, string appUri) {
            var result = new StringCollection();
            foreach (var name in domainNames) {
                var random = _randomSource.NextInt32(7);
                if ((result.Count == 0) || (random & 1) == 0) {
                    result.Add(string.Format("opc.tcp://{0}:{1}/{2}", name, port++.ToString(), appUri));
                }
                if ((random & 2) == 0) {
                    result.Add(string.Format("http://{0}:{1}/{2}", name, port++.ToString(), appUri));
                }
                if ((random & 4) == 0) {
                    result.Add(string.Format("https://{0}:{1}/{2}", name, port++.ToString(), appUri));
                }
            }
            return result;
        }

        private StringCollection RandomServerCapabilities() {
            var serverCapabilities = new StringCollection();
            var capabilities = _randomSource.NextInt32(8);
            foreach (var cap in _serverCapabilities) {
                if (_randomSource.NextInt32(100) > 50) {
                    serverCapabilities.Add(cap.Id);
                    if (capabilities-- == 0) {
                        break;
                    }
                }
            }
            return serverCapabilities;
        }

        private readonly int _randomStart;
        private readonly RandomSource _randomSource;
        private readonly DataGenerator _dataGenerator;
        private readonly Opc.Ua.Gds.Client.ServerCapabilities _serverCapabilities;
    }

}
