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
    using Newtonsoft.Json.Linq;

    class IIoTK8SClient {

        private readonly K8SConfiguration _k8sConfiguration;
        private readonly KubernetesClientConfiguration _k8sClientConfiguration;
        private readonly IKubernetes _k8sClient;

        private string _iiotNamespace = null;

        /// <summary>
        /// Constructor of IIoTK8SClient.
        /// </summary>
        /// <param name="kubeConfigContent"></param>
        public IIoTK8SClient(string kubeConfigContent) {
            if (string.IsNullOrWhiteSpace(kubeConfigContent)) {
                throw new ArgumentNullException(nameof(kubeConfigContent));
            }

            _k8sConfiguration = Yaml.LoadFromString<K8SConfiguration>(kubeConfigContent);
            _k8sClientConfiguration = KubernetesClientConfiguration
                .BuildConfigFromConfigObject(_k8sConfiguration);

            _k8sClient = new Kubernetes(_k8sClientConfiguration);
        }

        /// <summary>
        /// Set namespace that should be used for Azure IIoT components.
        /// </summary>
        /// <param name="namespaceName"></param>
        public void SetIIoTNamespace(string namespaceName) {
            _iiotNamespace = namespaceName;
        }

        public async Task<V1Namespace> CreateV1NamespaceAsync(
            string v1NamespaceContent,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(v1NamespaceContent)) {
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

        /// <summary>
        /// Create namespace for Azure Industrial IoT components.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1Namespace> CreateIIoTNamespaceAsync(
            CancellationToken cancellationToken = default
        ) {
            return await CreateV1NamespaceAsync(
                Resources.IIoTK8SResources._00_industrial_iot_namespace,
                cancellationToken
            );
        }

        /// <summary>
        /// Create service account, role and role binding for Azure Industrial IoT components.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Deploy oms agent configuration to enable scraping of Prometheus metrics.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1ConfigMap> EnablePrometheusMetricsScrapingAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                // Create configuration for oms agent that will enable scraping of Prometheus metrics.
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

        public async Task<V1Secret> CreateV1SecretAsync(
            string v1SecretContent,
            string namespaceParameter = null,
            IDictionary<string, string> data = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(v1SecretContent)) {
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
            if (string.IsNullOrWhiteSpace(v1ConfigMapContent)) {
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

        private async Task<V1Deployment> CreateV1DeploymentAsync(
            string v1DeploymentContent,
            string namespaceParameter = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(v1DeploymentContent)) {
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
            if (string.IsNullOrWhiteSpace(v1ServiceContent)) {
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

        /// <summary>
        /// Deploy Azure Industrial IoT components.
        /// </summary>
        /// <param name="env"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeployIIoTServicesAsync(
            IDictionary<string, string> env,
            CancellationToken cancellationToken = default
        ) {
            if (env is null) {
                throw new ArgumentNullException(nameof(env));
            }

            try {
                Log.Information("Deploying Industrial IoT services to Azure AKS cluster ...");

                // Create configuration secret for Azure Industrial IoT components.
                var aiiotEnvSecret = await CreateV1SecretAsync(
                    Resources.IIoTK8SResources._10_industrial_iot_env_secret,
                    _iiotNamespace,
                    env,
                    cancellationToken
                );

                // Deploy registry service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._20_registry_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._20_registry_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy twin service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._21_twin_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._21_twin_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy history service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._22_history_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._22_history_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy gateway service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._23_gateway_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._23_gateway_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy vault service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._24_vault_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._24_vault_service,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy alerting service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._25_alerting_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy onboarding service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._26_onboarding_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._26_onboarding_svc,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy jobs service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._27_jobs_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy modelprocessor service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._28_modelprocessor_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy blobnotification service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._29_blobnotification_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy publisher service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._30_publisher_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._30_publisher_svc,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy configuration service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._31_configuration_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._31_configuration_svc,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy edge manager service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._32_edge_manager_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._32_edge_manager_svc,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy events processor service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._33_events_processor_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy engineering tool
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._34_frontend_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._34_frontend_svc,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy identity service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._35_identity_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy edge jobs service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._36_edge_jobs_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._36_edge_jobs_svc,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy publisher jobs service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._37_publisher_jobs_deployment,
                    _iiotNamespace,
                    cancellationToken
                );
                await CreateV1ServiceAsync(
                    Resources.IIoTK8SResources._37_publisher_jobs_svc,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy telemetry cdm processor service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._38_telemetry_cdm_processor_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy telemetry processor service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._39_telemetry_processor_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy telemetry ux processor service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._40_telemetry_ux_processor_deployment,
                    _iiotNamespace,
                    cancellationToken
                );

                // Deploy registry events forwarder service
                await CreateV1DeploymentAsync(
                    Resources.IIoTK8SResources._41_opc_registry_events_forwarder_deployment,
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

        /// <summary>
        /// Create Ingress for Azure Industrial IoT components. 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Networkingv1beta1Ingress> CreateIIoTIngressAsync(
            string ingressHostname = null,
            CancellationToken cancellationToken = default
        ) {
            return await CreateNetworkingv1beta1IngressAsync(
                Resources.IIoTK8SResources._50_industrial_iot_ingress,
                _iiotNamespace,
                ingressHostname,
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
            Networkingv1beta1Ingress networkingv1beta1Ingress,
            CancellationToken cancellationToken = default
        ) {
            if (networkingv1beta1Ingress is null) {
                throw new ArgumentNullException(nameof(networkingv1beta1Ingress));
            }

            Exception exception = null;

            try {
                var tmpIngress = networkingv1beta1Ingress;

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
                        .ListNamespacedIngress1Async(
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

        public async Task<V1ServiceAccount> CreateV1ServiceAccountAsync(
            string v1ServiceAccountContent,
            string namespaceParameter = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(v1ServiceAccountContent)) {
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
            if (string.IsNullOrWhiteSpace(v1ClusterRoleContent)) {
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
            if (string.IsNullOrWhiteSpace(v1RoleContent)) {
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
            if (string.IsNullOrWhiteSpace(v1RoleBindingContent)) {
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
            if (string.IsNullOrWhiteSpace(v1ClusterRoleBindingContent)) {
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

        public async Task<Networkingv1beta1Ingress> CreateNetworkingv1beta1IngressAsync(
            string networkingv1beta1IngressContent,
            string namespaceParameter = null,
            string ingressHostname = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(networkingv1beta1IngressContent)) {
                throw new ArgumentNullException(nameof(networkingv1beta1IngressContent));
            }

            try {
                Log.Verbose("Loading k8s Ingress definition ...");
                var networkingv1beta1IngressDefinition = Yaml
                    .LoadFromString<Networkingv1beta1Ingress>(
                        networkingv1beta1IngressContent
                    );

                if (!string.IsNullOrWhiteSpace(namespaceParameter)) {
                    networkingv1beta1IngressDefinition.Metadata.NamespaceProperty = namespaceParameter;
                }

                // If hostname is provided then we will update Spec
                if (!string.IsNullOrWhiteSpace(ingressHostname)) {
                    // Add entry to Spec.Tls
                    const string secretName = "tls-secret";
                    var tls = new Networkingv1beta1IngressTLS {
                        Hosts = new List<string> { ingressHostname },
                        SecretName = secretName
                    };
                    if (networkingv1beta1IngressDefinition.Spec.Tls is null) {
                        networkingv1beta1IngressDefinition.Spec.Tls = 
                            new List<Networkingv1beta1IngressTLS> { tls };
                    } else {
                        networkingv1beta1IngressDefinition.Spec.Tls.Add(tls);
                    }

                    // Set host in Spec.Rules[0]
                    networkingv1beta1IngressDefinition.Spec.Rules.First().Host = ingressHostname;
                }

                Log.Verbose($"Creating k8s Ingress: " +
                    $"{networkingv1beta1IngressDefinition.Metadata.Name} ...");
                var networkingv1beta1Ingress = await _k8sClient
                    .CreateNamespacedIngress1Async(
                        networkingv1beta1IngressDefinition,
                        networkingv1beta1IngressDefinition.Metadata.NamespaceProperty,
                        cancellationToken: cancellationToken
                    );

                Log.Verbose($"Created k8s Ingress: {networkingv1beta1Ingress.Metadata.Name}");

                return networkingv1beta1Ingress;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create k8s Ingress");
                throw;
            }
        }

        /// <summary>
        /// Create a custom ClusterIssuer object.
        /// </summary>
        /// <param name="clusterIssuerContent"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1Alpha2ClusterIssuer> CreateV1Alpha2ClusterIssuerAsync(
            string clusterIssuerContent,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(clusterIssuerContent)) {
                throw new ArgumentNullException(nameof(clusterIssuerContent));
            }

            Log.Verbose("Loading k8s ClusterIssuer definition ...");
            var clusterIssuerDefinition = Yaml
            .LoadFromString<V1Alpha2ClusterIssuer>(
                clusterIssuerContent
            );

            Log.Verbose($"Creating k8s ClusterIssuer: {clusterIssuerDefinition.Metadata.Name} ...");
            var clusterIssuerObject = await _k8sClient
                .CreateClusterCustomObjectAsync(
                    clusterIssuerDefinition,
                    V1Alpha2ClusterIssuer.KubeGroup,
                    V1Alpha2ClusterIssuer.KubeApiVersion,
                    V1Alpha2ClusterIssuer.KubeKindPlural,
                    cancellationToken: cancellationToken
                );

            // Convert returned object back to V1Alpha2ClusterIssuer.
            if (!(clusterIssuerObject is JObject)) {
                throw new Exception($"Returned object is of unexpected type: {clusterIssuerObject.GetType()}");
            }

            // Note: Here we will loose extra fields that are not present in our V1Alpha2ClusterIssuer.
            var clusterIssuerJObject = (JObject) clusterIssuerObject;
            var clusterIssuer = clusterIssuerJObject.ToObject<V1Alpha2ClusterIssuer>();

            Log.Verbose($"Created k8s ClusterIssuer: {clusterIssuerDefinition.Metadata.Name} ...");
            return clusterIssuer;
        }

        /// <summary>
        /// Create a ClusterIssuer that uses prod endpoint of Let's Encrypt.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1Alpha2ClusterIssuer> CreateLetsencryptClusterIssuerAsync(
            CancellationToken cancellationToken = default
        ) {
            var clusterIssuer = await CreateV1Alpha2ClusterIssuerAsync(
                Resources.IIoTK8SResources._90_letsencrypt_cluster_issuer,
                cancellationToken
            );

            return clusterIssuer;
        }
    }
}
