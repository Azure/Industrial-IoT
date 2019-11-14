// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    using k8s;
    using k8s.Models;
    using Serilog;

    class IIoTK8SClient {

        private readonly k8s.KubeConfigModels.K8SConfiguration _k8sConfiguration;
        private readonly KubernetesClientConfiguration _k8sClientConfiguration;
        private readonly IKubernetes _k8sClient;

        private string _iiotNamespace = null;

        public IIoTK8SClient(string kubeConfigContent) {
            _k8sConfiguration = k8s.Yaml.LoadFromString<k8s.KubeConfigModels.K8SConfiguration>(kubeConfigContent);
            _k8sClientConfiguration = k8s.KubernetesClientConfiguration.BuildConfigFromConfigObject(_k8sConfiguration);

            _k8sClient = new Kubernetes(_k8sClientConfiguration);
        }

        public void SetIIoTNamespace(string namespaceName) {
            _iiotNamespace = namespaceName;
        }

        public async Task<V1Namespace> CreateIIoTNamespaceAsync() {
            try {
                Log.Verbose("Loading k8s Namespace definition ...");

                var iiotNamespaceDefinition = Yaml
                    .LoadFromString<V1Namespace>(
                        Resources.IIoTK8SResources._00_industrial_iot_namespace
                    );

                if (null != _iiotNamespace) {
                    iiotNamespaceDefinition.Metadata.Name = _iiotNamespace;
                }

                Log.Verbose($"Creating k8s Namespace: {iiotNamespaceDefinition.Metadata.Name} ...");

                var iiotNamespace = await _k8sClient.CreateNamespaceAsync(iiotNamespaceDefinition);

                Log.Verbose($"Created k8s Namespace: {iiotNamespace.Metadata.Name}");

                return iiotNamespace;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Namespace");
                throw;
            }
        }
        
        public async Task<V1Secret> CreateIIoTEnvSecretAsync(
            IDictionary<string, string> env
        ) {
            try {
                Log.Verbose("Loading k8s Secret definition ...");

                var iiotSecretDefinition = Yaml
                    .LoadFromString<V1Secret>(
                        Resources.IIoTK8SResources._01_industrial_iot_env_secret
                    );

                foreach (var envKVP in env) {
                    iiotSecretDefinition.Data[envKVP.Key] = Encoding.UTF8.GetBytes(envKVP.Value);
                }

                if (null != _iiotNamespace) {
                    iiotSecretDefinition.Metadata.NamespaceProperty = _iiotNamespace;
                }

                Log.Verbose($"Creating k8s Secret: {iiotSecretDefinition.Metadata.Name} ...");

                var iiotSecret = await _k8sClient
                    .CreateNamespacedSecretAsync(
                        iiotSecretDefinition,
                        iiotSecretDefinition.Metadata.NamespaceProperty
                    );

                Log.Verbose($"Created k8s Secret: {iiotSecret.Metadata.Name}");

                return iiotSecret;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Secret");
                throw;
            }
        }

        private async Task<Extensionsv1beta1Deployment> CreateExtensionsv1beta1DeploymentAsync(
            string extensionsv1beta1DeploymentContent
        ) {
            try {
                Log.Verbose("Loading k8s Deployment definition ...");

                var extensionsv1beta1Deployment = Yaml
                    .LoadFromString<Extensionsv1beta1Deployment>(
                        extensionsv1beta1DeploymentContent
                    );

                if (null != _iiotNamespace) {
                    extensionsv1beta1Deployment.Metadata.NamespaceProperty = _iiotNamespace;
                }

                Log.Verbose($"Creating k8s Deployment: {extensionsv1beta1Deployment.Metadata.Name} ...");

                var deployment = await _k8sClient.CreateNamespacedDeployment3Async(
                    extensionsv1beta1Deployment,
                    extensionsv1beta1Deployment.Metadata.NamespaceProperty
                );

                Log.Verbose($"Created k8s Deployment: {deployment.Metadata.Name}");

                return deployment;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Deployment");
                throw;
            }
        }

        private async Task<V1Service> CreateV1ServiceAsync(
            string v1ServiceContent
        ) {
            try {
                Log.Verbose("Loading k8s Service definition ...");

                var v1Service = Yaml
                    .LoadFromString<k8s.Models.V1Service>(
                        v1ServiceContent
                    );

                if (null != _iiotNamespace) {
                    v1Service.Metadata.NamespaceProperty = _iiotNamespace;
                }

                Log.Verbose($"Creating k8s Service: {v1Service.Metadata.Name} ...");

                var service = await _k8sClient.CreateNamespacedServiceAsync(
                    v1Service,
                    v1Service.Metadata.NamespaceProperty
                );

                Log.Verbose($"Created k8s Service: {service.Metadata.Name}");

                return service;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Service");
                throw;
            }
        }

        public async Task DeployIIoTServicesAsync() {
            try {
                Log.Information("Deploying Industrial IoT microservices to Azure AKS cluster ...");

                // Deploy registry service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._02_registry_deployment);
                await CreateV1ServiceAsync(Resources.IIoTK8SResources._02_registry_service);

                // Deploy twin service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._03_twin_deployment);
                await CreateV1ServiceAsync(Resources.IIoTK8SResources._03_twin_service);

                // Deploy history service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._04_history_deployment);
                await CreateV1ServiceAsync(Resources.IIoTK8SResources._04_history_service);

                // Deploy gateway service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._05_gateway_deployment);
                await CreateV1ServiceAsync(Resources.IIoTK8SResources._05_gateway_service);

                // Deploy vault service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._06_vault_deployment);
                await CreateV1ServiceAsync(Resources.IIoTK8SResources._06_vault_service);

                // Deploy alerting service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._07_alerting_deployment);

                // Deploy onboarding service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._08_onboarding_deployment);

                // Deploy jobs service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._09_jobs_deployment);

                // Deploy modelprocessor service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._10_modelprocessor_deployment);

                // Deploy blobnotification service
                await CreateExtensionsv1beta1DeploymentAsync(Resources.IIoTK8SResources._11_blobnotification_deployment);

                Log.Information($"Deployed Industrial IoT microservices to Azure AKS cluster");
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to deploy Industrial IoT microservices to Azure AKS cluster");
                throw;
            }
        }

        public async Task<V1Secret> CreateNGINXDefaultSSLCertificateSecretAsync(
            string certPem,
            string privateKeyPem
        ) {
            try {
                const string tlsCrt = "tls.crt";
                const string tlsKey = "tls.key";

                var certPemBytes = Encoding.UTF8.GetBytes(certPem);
                var privateKeyPemBytes = Encoding.UTF8.GetBytes(privateKeyPem);

                Log.Verbose("Loading k8s Secret definition ...");

                var v1SecretDefinition = Yaml
                    .LoadFromString<k8s.Models.V1Secret>(
                        Resources.IIoTK8SResources._20_web_app_secret
                    );

                v1SecretDefinition.Data[tlsCrt] = certPemBytes;
                v1SecretDefinition.Data[tlsKey] = privateKeyPemBytes;

                if (null != _iiotNamespace) {
                    v1SecretDefinition.Metadata.NamespaceProperty = _iiotNamespace;
                }

                Log.Verbose($"Creating k8s Secret: {v1SecretDefinition.Metadata.Name} ...");

                var v1Secret = await _k8sClient
                    .CreateNamespacedSecretAsync(
                        v1SecretDefinition,
                        v1SecretDefinition.Metadata.NamespaceProperty
                    );

                Log.Verbose($"Created k8s Secret: {v1Secret.Metadata.Name}");

                return v1Secret;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Secret");
                throw;
            }
        }
    }
}
