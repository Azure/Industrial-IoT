﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class IIoTK8SResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IIoTK8SResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Azure.IIoT.Deployment.Resources.IIoTK8SResources", typeof(IIoTK8SResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: Namespace
        ///metadata:
        ///  name: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: industrial-iot
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///.
        /// </summary>
        internal static string _00_industrial_iot_namespace {
            get {
                return ResourceManager.GetString("_00_industrial_iot_namespace", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: ServiceAccount
        ///metadata:
        ///  name: industrial-iot-serviceaccount
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: industrial-iot
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///.
        /// </summary>
        internal static string _01_industrial_iot_serviceaccount {
            get {
                return ResourceManager.GetString("_01_industrial_iot_serviceaccount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: rbac.authorization.k8s.io/v1
        ///kind: Role
        ///metadata:
        ///  name: industrial-iot-role
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: industrial-iot
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///rules:
        ///  - apiGroups:
        ///      - &quot;&quot;
        ///    resources:
        ///      - configmaps
        ///      - pods
        ///      - secrets
        ///      - namespaces
        ///    verbs:
        ///      - get
        ///.
        /// </summary>
        internal static string _02_industrial_iot_role {
            get {
                return ResourceManager.GetString("_02_industrial_iot_role", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: rbac.authorization.k8s.io/v1
        ///kind: RoleBinding
        ///metadata:
        ///  name: industrial-iot-role-binding
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: industrial-iot
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///roleRef:
        ///  apiGroup: rbac.authorization.k8s.io
        ///  kind: Role
        ///  name: industrial-iot-role
        ///subjects:
        ///  - kind: ServiceAccount
        ///    name: industrial-iot-serviceaccoun [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _03_industrial_iot_role_binding {
            get {
                return ResourceManager.GetString("_03_industrial_iot_role_binding", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: Secret
        ///metadata:
        ///  name: industrial-iot-env
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: industrial-iot
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///type: Opaque
        ///data:
        ///  _HUB_CS: null
        ///  PCS_IOTHUB_CONNSTRING: null
        ///  PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING: null
        ///  PCS_TELEMETRY_DOCUMENTDB_CONNSTRING: null
        ///  PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING: [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _10_industrial_iot_env_secret {
            get {
                return ResourceManager.GetString("_10_industrial_iot_env_secret", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: registry-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: registry-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: registry-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadata:
        ///      labels:
        ///   [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _11_registry_deployment {
            get {
                return ResourceManager.GetString("_11_registry_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: Service
        ///metadata:
        ///  name: registry-service
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: registry-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  type: ClusterIP
        ///  selector:
        ///    app.kubernetes.io/name: registry-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///  ports:
        ///  - port: 443
        ///    protocol: TCP
        ///    name: https
        ///  - port: 904 [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _11_registry_service {
            get {
                return ResourceManager.GetString("_11_registry_service", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: twin-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: twin-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: twin-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadata:
        ///      labels:
        ///        app.ku [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _12_twin_deployment {
            get {
                return ResourceManager.GetString("_12_twin_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: Service
        ///metadata:
        ///  name: twin-service
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: twin-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  type: ClusterIP
        ///  selector:
        ///    app.kubernetes.io/name: twin-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///  ports:
        ///  - port: 443
        ///    protocol: TCP
        ///    name: https
        ///  - port: 9041
        ///    proto [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _12_twin_service {
            get {
                return ResourceManager.GetString("_12_twin_service", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: history-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: history-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: history-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadata:
        ///      labels:
        ///      [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _13_history_deployment {
            get {
                return ResourceManager.GetString("_13_history_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: Service
        ///metadata:
        ///  name: history-service
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: history-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  type: ClusterIP
        ///  selector:
        ///    app.kubernetes.io/name: history-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///  ports:
        ///  - port: 443
        ///    protocol: TCP
        ///    name: https
        ///  - port: 9043
        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _13_history_service {
            get {
                return ResourceManager.GetString("_13_history_service", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: gateway-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: gateway-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: gateway-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadata:
        ///      labels:
        ///      [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _14_gateway_deployment {
            get {
                return ResourceManager.GetString("_14_gateway_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: Service
        ///metadata:
        ///  name: gateway-service
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: gateway-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  type: ClusterIP
        ///  selector:
        ///    app.kubernetes.io/name: gateway-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///  ports:
        ///  - port: 443
        ///    protocol: TCP
        ///    name: https
        ///  - port: 9040
        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _14_gateway_service {
            get {
                return ResourceManager.GetString("_14_gateway_service", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: vault-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: vault-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: vault-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadata:
        ///      labels:
        ///        app [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _15_vault_deployment {
            get {
                return ResourceManager.GetString("_15_vault_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: Service
        ///metadata:
        ///  name: vault-service
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: vault-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  type: ClusterIP
        ///  selector:
        ///    app.kubernetes.io/name: vault-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///  ports:
        ///  - port: 443
        ///    protocol: TCP
        ///    name: https
        ///  - port: 9044
        ///    pr [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _15_vault_service {
            get {
                return ResourceManager.GetString("_15_vault_service", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: alerting-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: alerting-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: alerting-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadata:
        ///      labels:
        ///   [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _16_alerting_deployment {
            get {
                return ResourceManager.GetString("_16_alerting_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: onboarding-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: onboarding-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: onboarding-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadata:
        ///      label [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _17_onboarding_deployment {
            get {
                return ResourceManager.GetString("_17_onboarding_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: jobs-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: jobs-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: jobs-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadata:
        ///      labels:
        ///        app.ku [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _18_jobs_deployment {
            get {
                return ResourceManager.GetString("_18_jobs_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: model-processor-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: model-processor-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: model-processor-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadat [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _19_modelprocessor_deployment {
            get {
                return ResourceManager.GetString("_19_modelprocessor_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: blob-notification-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: blob-notification-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: blob-notification-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    m [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _20_blobnotification_deployment {
            get {
                return ResourceManager.GetString("_20_blobnotification_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: twin-webui-deployment
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: twin-webui-service
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  replicas: 1
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: twin-webui-service
        ///      app.kubernetes.io/part-of: industrial-iot
        ///  template:
        ///    metadata:
        ///      label [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _21_twinwebui_deployment {
            get {
                return ResourceManager.GetString("_21_twinwebui_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: Secret
        ///metadata:
        ///  name: default-ssl-certificate
        ///  namespace: industrial-iot
        ///  labels:
        ///    app.kubernetes.io/name: industrial-iot
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///type: kubernetes.io/tls
        ///data:
        ///  tls.crt: null
        ///  tls.key: null
        ///.
        /// </summary>
        internal static string _25_default_ssl_certificate_secret {
            get {
                return ResourceManager.GetString("_25_default_ssl_certificate_secret", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: extensions/v1beta1
        ///kind: Ingress
        ///metadata:
        ///  name: industrial-iot-ingress
        ///  namespace: industrial-iot
        ///  annotations:
        ///    kubernetes.io/ingress.class: nginx
        ///    nginx.ingress.kubernetes.io/rewrite-target: /$1
        ///  labels:
        ///    app.kubernetes.io/name: industrial-iot-ingress
        ///    app.kubernetes.io/part-of: industrial-iot
        ///    app.kubernetes.io/version: 2.5.1
        ///    app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        ///spec:
        ///  rules:
        ///  - http:
        ///      paths:
        ///      - path: /registry/(.* [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _30_industrial_iot_ingress {
            get {
                return ResourceManager.GetString("_30_industrial_iot_ingress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: Namespace
        ///metadata:
        ///  name: ingress-nginx
        ///  labels:
        ///    app.kubernetes.io/name: ingress-nginx
        ///    app.kubernetes.io/part-of: ingress-nginx
        ///.
        /// </summary>
        internal static string _40_ingress_nginx_namespace {
            get {
                return ResourceManager.GetString("_40_ingress_nginx_namespace", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: v1
        ///kind: ServiceAccount
        ///metadata:
        ///  name: nginx-ingress-serviceaccount
        ///  namespace: ingress-nginx
        ///  labels:
        ///    app.kubernetes.io/name: ingress-nginx
        ///    app.kubernetes.io/part-of: ingress-nginx
        ///.
        /// </summary>
        internal static string _41_nginx_ingress_serviceaccount {
            get {
                return ResourceManager.GetString("_41_nginx_ingress_serviceaccount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: rbac.authorization.k8s.io/v1
        ///kind: ClusterRole
        ///metadata:
        ///  name: nginx-ingress-clusterrole
        ///  labels:
        ///    app.kubernetes.io/name: ingress-nginx
        ///    app.kubernetes.io/part-of: ingress-nginx
        ///rules:
        ///  - apiGroups:
        ///      - &quot;&quot;
        ///    resources:
        ///      - configmaps
        ///      - endpoints
        ///      - nodes
        ///      - pods
        ///      - secrets
        ///    verbs:
        ///      - list
        ///      - watch
        ///      # Required to get industrial-iot/web-app secret
        ///      - get
        ///  # Required to see industrial-iot namespace
        ///  - apiGroups [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _42_nginx_ingress_clusterrole {
            get {
                return ResourceManager.GetString("_42_nginx_ingress_clusterrole", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: rbac.authorization.k8s.io/v1
        ///kind: Role
        ///metadata:
        ///  name: nginx-ingress-role
        ///  namespace: ingress-nginx
        ///  labels:
        ///    app.kubernetes.io/name: ingress-nginx
        ///    app.kubernetes.io/part-of: ingress-nginx
        ///rules:
        ///  - apiGroups:
        ///      - &quot;&quot;
        ///    resources:
        ///      - configmaps
        ///      - pods
        ///      - secrets
        ///      - namespaces
        ///    verbs:
        ///      - get
        ///  - apiGroups:
        ///      - &quot;&quot;
        ///    resources:
        ///      - configmaps
        ///    resourceNames:
        ///      # Defaults to &quot;&lt;election-id&gt;-&lt;ingress-class&gt;&quot;
        ///       [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _43_nginx_ingress_role {
            get {
                return ResourceManager.GetString("_43_nginx_ingress_role", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: rbac.authorization.k8s.io/v1
        ///kind: RoleBinding
        ///metadata:
        ///  name: nginx-ingress-role-nisa-binding
        ///  namespace: ingress-nginx
        ///  labels:
        ///    app.kubernetes.io/name: ingress-nginx
        ///    app.kubernetes.io/part-of: ingress-nginx
        ///roleRef:
        ///  apiGroup: rbac.authorization.k8s.io
        ///  kind: Role
        ///  name: nginx-ingress-role
        ///subjects:
        ///  - kind: ServiceAccount
        ///    name: nginx-ingress-serviceaccount
        ///    namespace: ingress-nginx
        ///.
        /// </summary>
        internal static string _44_nginx_ingress_role_nisa_binding {
            get {
                return ResourceManager.GetString("_44_nginx_ingress_role_nisa_binding", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: rbac.authorization.k8s.io/v1
        ///kind: ClusterRoleBinding
        ///metadata:
        ///  name: nginx-ingress-clusterrole-nisa-binding
        ///  labels:
        ///    app.kubernetes.io/name: ingress-nginx
        ///    app.kubernetes.io/part-of: ingress-nginx
        ///roleRef:
        ///  apiGroup: rbac.authorization.k8s.io
        ///  kind: ClusterRole
        ///  name: nginx-ingress-clusterrole
        ///subjects:
        ///  - kind: ServiceAccount
        ///    name: nginx-ingress-serviceaccount
        ///    namespace: ingress-nginx
        ///.
        /// </summary>
        internal static string _45_nginx_ingress_clusterrole_nisa_binding {
            get {
                return ResourceManager.GetString("_45_nginx_ingress_clusterrole_nisa_binding", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to apiVersion: apps/v1
        ///kind: Deployment
        ///metadata:
        ///  name: nginx-ingress-controller
        ///  namespace: ingress-nginx
        ///  labels:
        ///    app.kubernetes.io/name: ingress-nginx
        ///    app.kubernetes.io/part-of: ingress-nginx
        ///spec:
        ///  replicas: 2
        ///  selector:
        ///    matchLabels:
        ///      app.kubernetes.io/name: ingress-nginx
        ///      app.kubernetes.io/part-of: ingress-nginx
        ///  template:
        ///    metadata:
        ///      labels:
        ///        app.kubernetes.io/name: ingress-nginx
        ///        app.kubernetes.io/part-of: ingress-nginx
        ///      annotati [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string _51_nginx_ingress_controller_deployment {
            get {
                return ResourceManager.GetString("_51_nginx_ingress_controller_deployment", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to kind: Service
        ///apiVersion: v1
        ///metadata:
        ///  name: ingress-nginx
        ///  namespace: ingress-nginx
        ///  labels:
        ///    app.kubernetes.io/name: ingress-nginx
        ///    app.kubernetes.io/part-of: ingress-nginx
        ///spec:
        ///  externalTrafficPolicy: Local
        ///  type: LoadBalancer
        ///  selector:
        ///    app.kubernetes.io/name: ingress-nginx
        ///    app.kubernetes.io/part-of: ingress-nginx
        ///  ports:
        ///    # - name: http
        ///    #   port: 80
        ///    #   targetPort: http
        ///    - name: https
        ///      port: 443
        ///      targetPort: https
        ///.
        /// </summary>
        internal static string _52_ingress_nginx_service {
            get {
                return ResourceManager.GetString("_52_ingress_nginx_service", resourceCulture);
            }
        }
    }
}
