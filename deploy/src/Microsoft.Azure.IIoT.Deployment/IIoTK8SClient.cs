// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using k8s;
    using k8s.Models;
    using k8s.KubeConfigModels;
    using Serilog;
    using System.Linq;
    using System.Diagnostics;

    class IIoTK8SClient {

        private readonly K8SConfiguration _k8sConfiguration;
        private readonly KubernetesClientConfiguration _k8sClientConfiguration;
        private readonly IKubernetes _k8sClient;

        private string _iiotNamespace = null;
        private string _nginxNamespace = null;

        /// <summary>
        /// Constructor of IIoTK8SClient.
        /// </summary>
        /// <param name="kubeConfigContent"></param>
        public IIoTK8SClient(string kubeConfigContent) {
            if (string.IsNullOrEmpty(kubeConfigContent)) {
                throw new ArgumentNullException(nameof(kubeConfigContent));
            }

            _k8sConfiguration = Yaml.LoadFromString<K8SConfiguration>(kubeConfigContent);
            _k8sClientConfiguration = KubernetesClientConfiguration
                .BuildConfigFromConfigObject(_k8sConfiguration);

            _k8sClient = new Kubernetes(_k8sClientConfiguration);
        }

        public void SetIIoTNamespace(string namespaceName) {
            _iiotNamespace = namespaceName;
        }

        public void SetNGINXIngressControllerNamespace(string namespaceName) {
            _nginxNamespace = namespaceName;
        }

        public async Task<V1Namespace> CreateV1NamespaceAsync(
            string v1NamespaceContent,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1NamespaceContent)) {
                throw new ArgumentNullException(nameof(v1NamespaceContent));
            }

            try {
                Log.Verbose("Loading k8s Namespace definition ...");
                var v1NamespaceDefinition = Yaml.LoadFromString<V1Namespace>(v1NamespaceContent);

                Log.Verbose($"Creating k8s Namespace: {v1NamespaceDefinition.Metadata.Name} ...");
                var v1Namespace = await _k8sClient
                    .CreateNamespaceAsync(
                        v1NamespaceDefinition,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s Namespace: {v1Namespace.Metadata.Name}");

                return v1Namespace;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Namespace");
                throw;
            }
        }

        public async Task<V1Namespace> CreateIIoTNamespaceAsync(
            CancellationToken cancellationToken = default
        ) {
            return await CreateV1NamespaceAsync(
                Resources.IIoTK8SResources._00_industrial_iot_namespace,
                cancellationToken
            );
        }

        public async Task<V1Namespace> CreateNGINXNamespaceAsync(
            CancellationToken cancellationToken = default
        ) {
            return await CreateV1NamespaceAsync(
                Resources.IIoTK8SResources._40_ingress_nginx_namespace,
                cancellationToken
            );
        }

        public async Task<V1ServiceAccount> SetupIIoTServiceAccountAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                // Create ServiceAcconut, Role and RoleBinding for Industrial IoT services.
                var v1ServiceAccount = await CreateV1ServiceAccountAsync(
                    Resources.IIoTK8SResources._01_industrial_iot_serviceaccount,
                    _iiotNamespace,
                    cancellationToken
                );

                await CreateV1RoleAsync(
                    Resources.IIoTK8SResources._02_industrial_iot_role,
                    _iiotNamespace,
                    cancellationToken
                );

                await CreateV1RoleBindingAsync(
                    Resources.IIoTK8SResources._03_industrial_iot_role_binding,
                    _iiotNamespace,
                    cancellationToken
                );

                return v1ServiceAccount;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create industrial-iot-serviceaccount and role for it");
                throw;
            }
        }

        public async Task<V1ConfigMap> EnablePrometheusMetricsScrapingAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                // Create configuration for oms agent that will enable scraping of prometheus metrics.
                // Here is the source of 04_oms_agent_configmap.yaml:
                // https://github.com/microsoft/OMS-docker/blob/ci_feature_prod/Kubernetes/container-azm-ms-agentconfig.yaml
                var v1ConfigMap = await CreateV1ConfigMapAsync(
                    Resources.IIoTK8SResources._04_oms_agent_configmap,
                    cancellationToken: cancellationToken
                );

                return v1ConfigMap;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create configuration for oms agent.");
                throw;
            }
        }

        public async Task<V1ServiceAccount> SetupNGINXServiceAccountAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                // Create ServiceAccount, Role, ClusterRole, RoleBinding and
                // ClusterRoleBinding for NGINX Ingress controller.
                var v1ServiceAccount = await CreateV1ServiceAccountAsync(
                    Resources.IIoTK8SResources._41_nginx_ingress_serviceaccount,
                    _nginxNamespace,
                    cancellationToken
                );

                await CreateV1ClusterRoleAsync(
                    Resources.IIoTK8SResources._42_nginx_ingress_clusterrole,
                    cancellationToken
                );

                await CreateV1RoleAsync(
                    Resources.IIoTK8SResources._43_nginx_ingress_role,
                    _nginxNamespace,
                    cancellationToken
                );

                await CreateV1RoleBindingAsync(
                    Resources.IIoTK8SResources._44_nginx_ingress_role_nisa_binding,
                    _nginxNamespace,
                    cancellationToken
                );

                await CreateV1ClusterRoleBindingAsync(
                    Resources.IIoTK8SResources._45_nginx_ingress_clusterrole_nisa_binding,
                    cancellationToken
                );

                return v1ServiceAccount;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create nginx-ingress-serviceaccount and roles for it");
                throw;
            }
        }

        public async Task<V1Secret> CreateV1SecretAsync(
            string v1SecretContent,
            string namespaceParameter = null,
            IDictionary<string, string> data = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1SecretContent)) {
                throw new ArgumentNullException(nameof(v1SecretContent));
            }

            try {
                Log.Verbose("Loading k8s Secret definition ...");
                var v1SecretDefinition = Yaml.LoadFromString<V1Secret>(v1SecretContent);

                if (null != data) {
                    foreach (var dataKVP in data) {
                        v1SecretDefinition.Data[dataKVP.Key] = Encoding.UTF8.GetBytes(dataKVP.Value);
                    }
                }

                if (null != namespaceParameter) {
                    v1SecretDefinition.Metadata.NamespaceProperty = namespaceParameter;
                }

                Log.Verbose($"Creating k8s Secret: {v1SecretDefinition.Metadata.Name} ...");
                var v1Secret = await _k8sClient
                    .CreateNamespacedSecretAsync(
                        v1SecretDefinition,
                        v1SecretDefinition.Metadata.NamespaceProperty,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s Secret: {v1Secret.Metadata.Name}");

                return v1Secret;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Secret");
                throw;
            }
        }

        public async Task<V1ConfigMap> CreateV1ConfigMapAsync(
            string v1ConfigMapContent,
            string namespaceParameter = null,
            IDictionary<string, string> data = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1ConfigMapContent)) {
                throw new ArgumentNullException(nameof(v1ConfigMapContent));
            }

            try {
                Log.Verbose("Loading k8s ConfigMap definition ...");
                var v1ConfigMapDefinition = Yaml.LoadFromString<V1ConfigMap>(v1ConfigMapContent);

                if (null != data) {
                    foreach (var dataKVP in data) {
                        v1ConfigMapDefinition.Data[dataKVP.Key] = dataKVP.Value;
                    }
                }

                if (null != namespaceParameter) {
                    v1ConfigMapDefinition.Metadata.NamespaceProperty = namespaceParameter;
                }

                Log.Verbose($"Creating k8s ConfigMap: {v1ConfigMapDefinition.Metadata.Name} ...");
                var v1ConfigMap = await _k8sClient
                    .CreateNamespacedConfigMapAsync(
                        v1ConfigMapDefinition,
                        v1ConfigMapDefinition.Metadata.NamespaceProperty,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s ConfigMap: {v1ConfigMap.Metadata.Name}");

                return v1ConfigMap;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s ConfigMap");
                throw;
            }
        }

        public async Task<V1Secret> CreateIIoTEnvSecretAsync(
            IDictionary<string, string> env,
            CancellationToken cancellationToken = default
        ) {
            if (env is null) {
                throw new ArgumentNullException(nameof(env));
            }

            return await CreateV1SecretAsync(
                Resources.IIoTK8SResources._10_industrial_iot_env_secret,
                _iiotNamespace,
                env,
                cancellationToken
            );
        }

        private async Task<V1Deployment> CreateV1DeploymentAsync(
            string v1DeploymentContent,
            string namespaceParameter = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1DeploymentContent)) {
                throw new ArgumentNullException(nameof(v1DeploymentContent));
            }

            try {
                Log.Verbose("Loading k8s Deployment definition ...");
                var v1DeploymentDefinition = Yaml
                    .LoadFromString<V1Deployment>(
                        v1DeploymentContent
                    );

                if (null != namespaceParameter) {
                    v1DeploymentDefinition.Metadata.NamespaceProperty = namespaceParameter;
                }

                Log.Verbose($"Creating k8s Deployment: {v1DeploymentDefinition.Metadata.Name} ...");
                var v1Deployment = await _k8sClient
                    .CreateNamespacedDeploymentAsync(
                        v1DeploymentDefinition,
                        v1DeploymentDefinition.Metadata.NamespaceProperty,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s Deployment: {v1Deployment.Metadata.Name}");

                return v1Deployment;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Deployment");
                throw;
            }
        }

        private async Task<V1Service> CreateV1ServiceAsync(
            string v1ServiceContent,
            string namespaceParameter = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1ServiceContent)) {
                throw new ArgumentNullException(nameof(v1ServiceContent));
            }

            try {
                Log.Verbose("Loading k8s Service definition ...");
                var v1Service = Yaml.LoadFromString<V1Service>(v1ServiceContent);

                if (null != namespaceParameter) {
                    v1Service.Metadata.NamespaceProperty = namespaceParameter;
                }

                Log.Verbose($"Creating k8s Service: {v1Service.Metadata.Name} ...");
                var service = await _k8sClient
                    .CreateNamespacedServiceAsync(
                        v1Service,
                        v1Service.Metadata.NamespaceProperty,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s Service: {service.Metadata.Name}");

                return service;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Service");
                throw;
            }
        }

        public async Task DeployIIoTServicesAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Information("Deploying Industrial IoT services to Azure AKS cluster ...");

                // Deploy registry service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._11_registry_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._11_registry_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy twin service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._12_twin_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._12_twin_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy history service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._13_history_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._13_history_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy gateway service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._14_gateway_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._14_gateway_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy vault service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._15_vault_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._15_vault_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy alerting service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._16_alerting_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy onboarding service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._17_onboarding_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy jobs service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._18_jobs_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy modelprocessor service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._19_modelprocessor_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy blobnotification service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._20_blobnotification_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                Log.Information($"Deployed Industrial IoT services to Azure AKS cluster");
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to deploy Industrial IoT services to Azure AKS cluster");
                throw;
            }
        }

        public async Task<Extensionsv1beta1Ingress> CreateIIoTIngressAsync(
            CancellationToken cancellationToken = default
        ) {
            return await CreateExtensionsv1beta1IngressAsync(
                Resources.IIoTK8SResources._30_industrial_iot_ingress,
                _iiotNamespace,
                cancellationToken
            );
        }

        /// <summary>
        /// Wait untill IP address of Ingress LoadBalancer is available.
        /// </summary>
        /// <param name="ingress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<V1LoadBalancerIngress>> WaitForIngressIPAsync(
            Extensionsv1beta1Ingress extensionsv1beta1Ingress,
            CancellationToken cancellationToken = default
        ) {
            if (extensionsv1beta1Ingress is null) {
                throw new ArgumentNullException(nameof(extensionsv1beta1Ingress));
            }

            Exception exception = null;

            try {
                var tmpIngress = extensionsv1beta1Ingress;

                var namespaceProperty = tmpIngress.Metadata.NamespaceProperty;
                var labels = tmpIngress.Metadata.Labels
                    .Select(kvp => $"{kvp.Key}={kvp.Value}")
                    .ToList();
                var labelsStr = string.Join(",", labels);

                const int secondsDelay = 5;
                const int secondsRetry = 900;

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Now we will loop untill LoadBalancer Ingress IP address is available.
                while (null == tmpIngress.Status
                    || null == tmpIngress.Status.LoadBalancer
                    || null == tmpIngress.Status.LoadBalancer.Ingress
                    || 0 == tmpIngress.Status.LoadBalancer.Ingress.Count()
                ) {
                    if (stopwatch.Elapsed >= TimeSpan.FromSeconds(secondsRetry)) {
                        exception = new Exception($"Ingress IP address is not available after " +
                            $"{secondsRetry} seconds for: {tmpIngress.Metadata.Name}");
                        break;
                    }

                    await Task.Delay(secondsDelay * 1000, cancellationToken);

                    var ingresses = await _k8sClient
                        .ListNamespacedIngressAsync(
                            namespaceProperty,
                            labelSelector: labelsStr,
                            cancellationToken: cancellationToken
                        );

                    tmpIngress = ingresses
                        .Items
                        .Where(ingress => ingress.Metadata.Name == tmpIngress.Metadata.Name)
                        .FirstOrDefault();
                }

                if (null == exception) {
                    return tmpIngress.Status.LoadBalancer.Ingress.ToList();
                }
            } catch (Exception ex) {
                Log.Error(ex, $"Failure while waiting for IP address of Ingress LoadBalancer");
                throw;
            }

            throw exception;
        }

        public async Task<V1Secret> CreateNGINXDefaultSSLCertificateSecretAsync(
            string certPem,
            string privateKeyPem,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(certPem)) {
                throw new ArgumentNullException(nameof(certPem));
            }
            if (string.IsNullOrEmpty(privateKeyPem)) {
                throw new ArgumentNullException(nameof(privateKeyPem));
            }

            const string tlsCrt = "tls.crt";
            const string tlsKey = "tls.key";

            var defaultSslCertificateSecret = await CreateV1SecretAsync(
                Resources.IIoTK8SResources._25_default_ssl_certificate_secret,
                _iiotNamespace,
                new Dictionary<string, string> {
                    { tlsCrt, certPem },
                    { tlsKey, privateKeyPem}
                },
                cancellationToken
            );

            return defaultSslCertificateSecret;
        }

        public async Task DeployNGINXIngressControllerAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                await CreateV1ConfigMapAsync(
                    Resources.IIoTK8SResources._50_nginx_ingress_configuration_configmap,
                    _nginxNamespace,
                    cancellationToken: cancellationToken
                );

                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._51_nginx_ingress_controller_deployment,
                    _nginxNamespace,
                    cancellationToken
                );

                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._52_ingress_nginx_service,
                    _nginxNamespace,
                    cancellationToken
                );

                Log.Verbose($"Deployed NGINX Ingress Controller to Azure AKS cluster");
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to deploy NGINX Ingress Controller to Azure AKS cluster");
                throw;
            }
        }

        public async Task<V1ServiceAccount> CreateV1ServiceAccountAsync(
            string v1ServiceAccountContent,
            string namespaceParameter = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1ServiceAccountContent)) {
                throw new ArgumentNullException(nameof(v1ServiceAccountContent));
            }

            try {
                Log.Verbose("Loading k8s ServiceAccount definition ...");
                var v1ServiceAccountDefinition = Yaml
                    .LoadFromString<V1ServiceAccount>(v1ServiceAccountContent);

                if (null != namespaceParameter) {
                    v1ServiceAccountDefinition.Metadata.NamespaceProperty = namespaceParameter;
                }

                Log.Verbose($"Creating k8s ServiceAccount: " +
                    $"{v1ServiceAccountDefinition.Metadata.Name} ...");
                var v1ServiceAccount = await _k8sClient
                    .CreateNamespacedServiceAccountAsync(
                        v1ServiceAccountDefinition,
                        v1ServiceAccountDefinition.Metadata.NamespaceProperty,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s ServiceAccount: {v1ServiceAccount.Metadata.Name}");

                return v1ServiceAccount;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s ServiceAccount");
                throw;
            }
        }

        public async Task<V1ClusterRole> CreateV1ClusterRoleAsync(
            string v1ClusterRoleContent,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1ClusterRoleContent)) {
                throw new ArgumentNullException(nameof(v1ClusterRoleContent));
            }

            try {
                Log.Verbose("Loading k8s ClusterRole definition ...");
                var v1ClusterRoleDefinition = Yaml
                    .LoadFromString<V1ClusterRole>(v1ClusterRoleContent);

                Log.Verbose($"Creating k8s ClusterRole: " +
                    $"{v1ClusterRoleDefinition.Metadata.Name} ...");
                var v1ClusterRole = await _k8sClient
                    .CreateClusterRoleAsync(
                        v1ClusterRoleDefinition,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s ClusterRole: {v1ClusterRole.Metadata.Name}");

                return v1ClusterRole;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s ClusterRole");
                throw;
            }
        }

        public async Task<V1Role> CreateV1RoleAsync(
            string v1RoleContent,
            string namespaceParameter = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1RoleContent)) {
                throw new ArgumentNullException(nameof(v1RoleContent));
            }

            try {
                Log.Verbose("Loading k8s Role definition ...");
                var v1RoleDefinition = Yaml
                    .LoadFromString<V1Role>(v1RoleContent);

                if (null != namespaceParameter) {
                    v1RoleDefinition.Metadata.NamespaceProperty = namespaceParameter;
                }

                Log.Verbose($"Creating k8s Role: {v1RoleDefinition.Metadata.Name} ...");
                var v1Role = await _k8sClient
                    .CreateNamespacedRoleAsync(
                        v1RoleDefinition,
                        v1RoleDefinition.Metadata.NamespaceProperty,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s Role: {v1Role.Metadata.Name}");

                return v1Role;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Role");
                throw;
            }
        }

        public async Task<V1RoleBinding> CreateV1RoleBindingAsync(
            string v1RoleBindingContent,
            string namespaceParameter = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1RoleBindingContent)) {
                throw new ArgumentNullException(nameof(v1RoleBindingContent));
            }

            try {
                Log.Verbose("Loading k8s RoleBinding definition ...");
                var v1RoleBindingDefinition = Yaml
                    .LoadFromString<V1RoleBinding>(v1RoleBindingContent);

                if (null != namespaceParameter) {
                    v1RoleBindingDefinition.Metadata.NamespaceProperty = namespaceParameter;
                }

                Log.Verbose($"Creating k8s RoleBinding: {v1RoleBindingDefinition.Metadata.Name} ...");
                var v1RoleBinding = await _k8sClient
                    .CreateNamespacedRoleBindingAsync(
                        v1RoleBindingDefinition,
                        v1RoleBindingDefinition.Metadata.NamespaceProperty,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s RoleBinding: {v1RoleBindingDefinition.Metadata.Name}");

                return v1RoleBinding;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s RoleBinding");
                throw;
            }
        }

        public async Task<V1ClusterRoleBinding> CreateV1ClusterRoleBindingAsync(
            string v1ClusterRoleBindingContent,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(v1ClusterRoleBindingContent)) {
                throw new ArgumentNullException(nameof(v1ClusterRoleBindingContent));
            }

            try {
                Log.Verbose("Loading k8s ClusterRoleBinding definition ...");
                var v1ClusterRoleBindingDefinition = Yaml
                    .LoadFromString<V1ClusterRoleBinding>(v1ClusterRoleBindingContent);

                Log.Verbose($"Creating k8s ClusterRoleBinding: " +
                    $"{v1ClusterRoleBindingDefinition.Metadata.Name} ...");
                var v1ClusterRoleBinding = await _k8sClient
                    .CreateClusterRoleBindingAsync(
                        v1ClusterRoleBindingDefinition,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s ClusterRoleBinding: " +
                    $"{v1ClusterRoleBinding.Metadata.Name}");

                return v1ClusterRoleBinding;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s ClusterRoleBinding");
                throw;
            }
        }

        public async Task<Extensionsv1beta1Ingress> CreateExtensionsv1beta1IngressAsync(
            string extensionsv1beta1IngressContent,
            string namespaceParameter = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(extensionsv1beta1IngressContent)) {
                throw new ArgumentNullException(nameof(extensionsv1beta1IngressContent));
            }

            try {
                Log.Verbose("Loading k8s Ingress definition ...");
                var extensionsv1beta1IngressDefinition = Yaml
                    .LoadFromString<Extensionsv1beta1Ingress>(
                        extensionsv1beta1IngressContent
                    );

                if (null != namespaceParameter) {
                    extensionsv1beta1IngressDefinition.Metadata.NamespaceProperty = namespaceParameter;
                }

                Log.Verbose($"Creating k8s Ingress: " +
                    $"{extensionsv1beta1IngressDefinition.Metadata.Name} ...");
                var extensionsv1beta1Ingress = await _k8sClient
                    .CreateNamespacedIngressAsync(
                        extensionsv1beta1IngressDefinition,
                        extensionsv1beta1IngressDefinition.Metadata.NamespaceProperty,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s Ingress: {extensionsv1beta1Ingress.Metadata.Name}");

                return extensionsv1beta1Ingress;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Ingress");
                throw;
            }
        }
    }
}
