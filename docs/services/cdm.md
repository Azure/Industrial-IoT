# Datalake and CDM Telemetry export

[Home](readme.md)

## Overview

The datalake exporter agent receives events from the secondary telemetry event hub and publishes these events to the configured Azure Datalake Storage resource.   It also ensures that the necessary Common Data Model Schema files are created so that consumers such as Power Apps and Power Bi can access the data and metadata.
