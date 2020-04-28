// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {
    using System;
    using System.Collections.Generic;

    using k8s.Models;
    using Newtonsoft.Json;

    class Ingress {
        public Ingress() { }

        public Ingress(string classP = null) {
            Class = classP;
        }

        [JsonProperty(PropertyName = "class")]
        public string Class { get; set; }
    }

    class Http01Solver {
        public Http01Solver() { }

        public Http01Solver(Ingress ingress = null) {
            Ingress = ingress;
        }

        [JsonProperty(PropertyName = "ingress")]
        public Ingress Ingress { get; set; }
    }

    class Solver {
        public Solver() { }

        public Solver(Http01Solver http01 = null) {
            Http01 = http01;
        }

        [JsonProperty(PropertyName = "http01")]
        public Http01Solver Http01 { get; set; }
    }

    class PrivateKeySecretRef {
        public PrivateKeySecretRef() { }

        public PrivateKeySecretRef(string name = null) {
            Name = name;
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }

    class SpecAcme {
        public SpecAcme() { }

        public SpecAcme(
            string server = null,
            string email = null,
            PrivateKeySecretRef privateKeySecretRef = null,
            IList<Solver> solvers = null
        ) {
            Server = server;
            Email = email;
            PrivateKeySecretRef = privateKeySecretRef;
            Solvers = solvers;
        }

        [JsonProperty(PropertyName = "server")]
        public string Server { get; set; }
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
        [JsonProperty(PropertyName = "privateKeySecretRef")]
        public PrivateKeySecretRef PrivateKeySecretRef { get; set; }
        [JsonProperty(PropertyName = "solvers")]
        public IList<Solver> Solvers { get; set; }
    }

    enum IssuerConditionStatus {
        Unknown,
        True,
        False
    }

    class IssuerCondition {
        public IssuerCondition() { }

        public IssuerCondition(
            DateTime lastTransitionTime = default,
            string message = null,
            string reason = null,
            IssuerConditionStatus status = IssuerConditionStatus.Unknown,
            string type = null
        ) {
            LastTransitionTime = lastTransitionTime;
            Message = message;
            Reason = reason;
            Status = status;
            Type = type;
        }

        [JsonProperty(PropertyName = "lastTransitionTime")]
        public DateTime LastTransitionTime { get; set; }
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
        [JsonProperty(PropertyName = "reason")]
        public string Reason { get; set; }
        [JsonProperty(PropertyName = "status")]
        public IssuerConditionStatus Status { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }

    class IssuerSpec {
        public IssuerSpec() { }

        public IssuerSpec(SpecAcme acme = null) {
            Acme = acme;
        }

        [JsonProperty(PropertyName = "acme")]
        public SpecAcme Acme { get; set; }
    }

    class StatusAcme {
        public StatusAcme() { }

        public StatusAcme(
            string lastRegisteredEmail = null,
            string uri = null
        ) {
            LastRegisteredEmail = lastRegisteredEmail;
            Uri = uri;
        }

        [JsonProperty(PropertyName = "lastRegisteredEmail")]
        public string LastRegisteredEmail { get; set; }
        [JsonProperty(PropertyName = "uri")]
        public string Uri { get; set; }

    }

    class IssuerStatus {
        public IssuerStatus() { }

        public IssuerStatus(
            StatusAcme acme = null,
            IList<IssuerCondition> conditions = null
        ) {
            Acme = acme;
            Conditions = conditions;
        }

        [JsonProperty(PropertyName = "acme")]
        public StatusAcme Acme { get; set; }
        [JsonProperty(PropertyName = "conditions")]
        public IList<IssuerCondition> Conditions { get; set; }
    }

    /// <summary>
    /// This is a partial definition of v1alpha2 ClusterIssuer based on
    /// https://raw.githubusercontent.com/jetstack/cert-manager/release-0.13/deploy/manifests/00-crds.yaml
    /// </summary>
    class V1Alpha2ClusterIssuer {

        public const string KubeApiVersion = "v1alpha2";
        public const string KubeKind = "ClusterIssuer";
        public const string KubeGroup = "cert-manager.io";
        public const string KubeKindPlural = "clusterissuers";

        public V1Alpha2ClusterIssuer() { }

        public V1Alpha2ClusterIssuer(
            string apiVersion = null,
            string kind = null,
            V1ObjectMeta metadata = null,
            IssuerSpec spec = null,
            IssuerStatus status = null
        ) {
            ApiVersion = apiVersion;
            Kind = kind;
            Metadata = metadata;
            Spec = spec;
            Status = status;
        }

        [JsonProperty(PropertyName = "apiVersion")]
        public string ApiVersion { get; set; }
        [JsonProperty(PropertyName = "kind")]
        public string Kind { get; set; }
        [JsonProperty(PropertyName = "metadata")]
        public V1ObjectMeta Metadata { get; set; }
        [JsonProperty(PropertyName = "spec")]
        public IssuerSpec Spec { get; set; }
        [JsonProperty(PropertyName = "status")]
        public IssuerStatus Status { get; set; }
    }
}
