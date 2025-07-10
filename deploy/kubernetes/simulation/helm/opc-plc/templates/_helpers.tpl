{{/*
Expand the name of the chart.
*/}}
{{- define "simulation.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "simulation.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Name of ConfigMap for OPC PLC.
*/}}
{{- define "simulation.configmap" -}}
{{- printf "%s-%s" "opc-plc-config" $.Release.Name | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "simulation.labels" -}}
helm.sh/chart: {{ include "simulation.chart" . }}
{{ include "simulation.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "simulation.selectorLabels" -}}
app.kubernetes.io/name: {{ include "simulation.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}
