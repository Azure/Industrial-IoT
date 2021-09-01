{{/* vim: set filetype=mustache: */}}
{{/*
Expand the name of the chart.
*/}}
{{- define "azure-industrial-iot.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "azure-industrial-iot.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{/*
Create the name of the service account to use.
*/}}
{{- define "azure-industrial-iot.serviceAccountName" -}}
{{- if .Values.serviceAccount.create -}}
{{ default (include "azure-industrial-iot.fullname" .) .Values.serviceAccount.name }}
{{- else -}}
{{ default "default" .Values.serviceAccount.name }}
{{- end -}}
{{- end -}}

{{/*
Create the name of secret that will contain details of Azure resources.
*/}}
{{- define "azure-industrial-iot.env.fullname" -}}
{{- printf "%s-%s" .Release.Name "azure-industrial-iot-env" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create the names of Industrial-IoT microservices.
*/}}
{{- define "azure-industrial-iot.registry.fullname" -}}
{{- printf "%s-%s" .Release.Name "registry" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.twin.fullname" -}}
{{- printf "%s-%s" .Release.Name "twin" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.history.fullname" -}}
{{- printf "%s-%s" .Release.Name "history" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.onboarding.fullname" -}}
{{- printf "%s-%s" .Release.Name "onboarding" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.sync.fullname" -}}
{{- printf "%s-%s" .Release.Name "sync" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.publisher.fullname" -}}
{{- printf "%s-%s" .Release.Name "publisher" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.events.fullname" -}}
{{- printf "%s-%s" .Release.Name "events" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.events-processor.fullname" -}}
{{- printf "%s-%s" .Release.Name "events-processor" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.engineering-tool.fullname" -}}
{{- printf "%s-%s" .Release.Name "engineering-tool" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.edge-jobs.fullname" -}}
{{- printf "%s-%s" .Release.Name "edge-jobs" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.telemetry-processor.fullname" -}}
{{- printf "%s-%s" .Release.Name "telemetry-processor" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
app.kubernetes.io/component labels of Industrial-IoT microservices.

Those are used in Service selectors so they have to be unique for each microservices.
*/}}
{{- define "azure-industrial-iot.registry.component" -}}
{{- "opc-registry-service" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.twin.component" -}}
{{- "opc-twin-service" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.history.component" -}}
{{- "opc-history-service" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.onboarding.component" -}}
{{- "opc-onboarding-service" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.sync.component" -}}
{{- "opc-sync-service" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.publisher.component" -}}
{{- "opc-publisher-service" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.events.component" -}}
{{- "industrial-iot-events-service" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.events-processor.component" -}}
{{- "industrial-iot-events-processor" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.engineering-tool.component" -}}
{{- "industrial-iot-engineering-tool" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.edge-jobs.component" -}}
{{- "industrial-iot-jobs-orchestrator-service" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "azure-industrial-iot.telemetry-processor.component" -}}
{{- "industrial-iot-telemetry-processor" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create the names of Ingress resource for Industrial-IoT microservices.
*/}}
{{- define "azure-industrial-iot.ingress.fullname" -}}
{{- printf "%s-%s" .Release.Name "ingress" | trunc 63 | trimSuffix "-" -}}
{{- end -}}
