{{/*
Expand the name of the chart.
*/}}
{{- define "opcuabroker.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "opcuabroker.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Name of ConfigMap for OPC PLC.
*/}}
{{- define "opcuabroker.configmap" -}}
{{- printf "%s-%s" "opcplc-config" $.Release.Name | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "opcuabroker.labels" -}}
helm.sh/chart: {{ include "opcuabroker.chart" . }}
{{ include "opcuabroker.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "opcuabroker.selectorLabels" -}}
app.kubernetes.io/name: {{ include "opcuabroker.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}
