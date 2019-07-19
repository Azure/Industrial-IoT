// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Test {
    using Microsoft.Azure.IIoT.Module.Deployment.Models;
    using Microsoft.Azure.IIoT.Hub.Models;
    using AutoFixture;
    using Docker.DotNet.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class EdgeDeploymentBaseTest {

        [Fact]
        protected void TestDeploymentManifestAndJson1() {

            var deployment1 = new EdgeDeploymentBase()
                .WithModule("test1", "microsoft/test2:latest")
                .WithModule("test2", "microsoft/test1:latest")
                .WithRoute("test12", "test1", "test2");

            var deployment2 = new EdgeDeploymentBase(
                JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                    deployment1.ToString()));

            var config1 = JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                deployment1.ToString());
            var config2 = JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                deployment2.ToString());
            Assert.True(JToken.DeepEquals(JToken.FromObject(config1), JToken.FromObject(config2)));
        }

        [Fact]
        protected void TestDeploymentManifestAndJson2() {
            var deployment1 = new EdgeDeploymentBase()
                .WithModule("test1", "microsoft/test2:latest", new CreateContainerParameters {
                    Env = new List<string> {
                        "FOOBAR1=zzzzzzz",
                        "FOOBAR2=zzzzzzz",
                        "FOOBAR3=zzzzzzz",
                        "FOOBAR4=zzzzzzz",
                    },
                    NetworkingConfig = new NetworkingConfig {
                        EndpointsConfig = new Dictionary<string, EndpointSettings> {
                            ["test1"] = new EndpointSettings {
                                IPAddress = "1.2.1.3",
                                NetworkID = "ddid"
                            },
                            ["test2"] = new EndpointSettings {
                                IPAddress = "1.2.1.3",
                                NetworkID = "ddid"
                            }
                        }
                    },
                })
                .WithModule("test2", "microsoft/test1:latest", new Dictionary<string, dynamic> {
                    ["On"] = new {
                        Test = "test",
                        Swa = "swa",
                        b = new {
                            x = 1,
                            z = 1.0
                        }
                    }
                })
                .WithRoute("test12", "test1", "test2");

            var deployment2 = new EdgeDeploymentBase(
                JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                    deployment1.ToString()));

            var config1 = JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                deployment1.ToString());
            var config2 = JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                deployment2.ToString());
            Assert.True(JToken.DeepEquals(JToken.FromObject(config1), JToken.FromObject(config2)));
        }

        [Fact]
        protected void TestDeploymentManifestAndJson3() {
            var fixture = new Fixture();
            var customization = new SupportMutableValueTypesCustomization();
            customization.Customize(fixture);
            var modules = fixture.CreateMany<EdgeDeploymentModuleModel>(30).ToList();
            var routes = fixture.CreateMany<EdgeDeploymentRouteModel>(10).ToList();
            for (var i = 0; i < routes.Count; i++) {
                routes[i].To = modules[i].Name;
                routes[i].From = modules[i + 10].Name;
            }
            var deployment1 = new EdgeDeploymentBase();
            deployment1.WithManifest(new EdgeDeploymentManifestModel {
                Modules = modules,
                Routes = routes
            });
            var json1 = deployment1.ToString();
            var deployment2 = new EdgeDeploymentBase(
                JsonConvertEx.DeserializeObject<ConfigurationContentModel>(json1));
            var config1 = JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                json1);
            var config2 = JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                deployment2.ToString());
            Assert.True(JToken.DeepEquals(JToken.FromObject(config1), JToken.FromObject(config2)));
        }

        [Fact]
        protected void TestDeploymentManifestAndJsonTwin() {

            var deployment1 = new EdgeDeploymentBase()
                .WithModule("twin", "marcschier/azure-iiot-opc-twin-module", new CreateContainerParameters {
                    HostConfig = new HostConfig {
                        Privileged = true
                    }
                });

            var json1 = deployment1.ToString();
            var deployment2 = new EdgeDeploymentBase(
                JsonConvertEx.DeserializeObject<ConfigurationContentModel>(json1));
            var config1 = JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                json1);
            var config2 = JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                deployment2.ToString());
            Assert.True(JToken.DeepEquals(JToken.FromObject(config1), JToken.FromObject(config2)));
        }

    }
}
