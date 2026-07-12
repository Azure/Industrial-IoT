// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using System;
    using System.IO;

    /// <summary>
    /// Reads the Kubernetes signals supplied to an in-cluster workload.
    /// </summary>
    public static class KubernetesEnvironment
    {
        /// <summary>
        /// The Kubernetes API service host environment variable.
        /// </summary>
        public const string ServiceHostEnvironmentVariable = "KUBERNETES_SERVICE_HOST";

        /// <summary>
        /// The Kubernetes API service port environment variable.
        /// </summary>
        public const string ServicePortEnvironmentVariable = "KUBERNETES_SERVICE_PORT";

        /// <summary>
        /// The Kubernetes HTTPS API service port environment variable.
        /// </summary>
        public const string ServicePortHttpsEnvironmentVariable =
            "KUBERNETES_SERVICE_PORT_HTTPS";

        /// <summary>
        /// The mounted service account directory.
        /// </summary>
        public const string ServiceAccountDirectory =
            "/var/run/secrets/kubernetes.io/serviceaccount";

        /// <summary>
        /// Whether the current process runs in a Kubernetes cluster.
        /// </summary>
        public static bool IsInCluster()
        {
            return !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(ServiceHostEnvironmentVariable)) ||
                File.Exists(Path.Combine(ServiceAccountDirectory, "token"));
        }

        /// <summary>
        /// Gets the current namespace, when available.
        /// </summary>
        public static string Namespace
        {
            get
            {
                try
                {
                    var path = Path.Combine(ServiceAccountDirectory, "namespace");
                    if (File.Exists(path))
                    {
                        var value = File.ReadAllText(path).Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            return value;
                        }
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
                return "default";
            }
        }

        /// <summary>
        /// Gets the Kubernetes API host, when the service environment is present.
        /// </summary>
        public static string Host
        {
            get
            {
                var host = Environment.GetEnvironmentVariable(ServiceHostEnvironmentVariable);
                if (string.IsNullOrWhiteSpace(host))
                {
                    return "https://kubernetes.default.svc";
                }

                var port = Environment.GetEnvironmentVariable(
                    ServicePortHttpsEnvironmentVariable) ??
                    Environment.GetEnvironmentVariable(ServicePortEnvironmentVariable) ??
                    "443";
                if (host.Contains(':', StringComparison.Ordinal) &&
                    !host.StartsWith('['))
                {
                    host = $"[{host}]";
                }
                return $"https://{host}:{port}";
            }
        }
    }
}
