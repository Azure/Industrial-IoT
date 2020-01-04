# Registry Security Alerting Agent

[Home](../readme.md)

## Overview

The security Alerting agent listens for security relevant events on the messaging backplane and translates them to SIEM / Azure Security Center relevant notifications.   The following security events are currently generated:

- Endpoint with expired or self signed certificates
- Applications with expired or self signed certificates
- Endpoints exposed with Security mode "None".

The alerting agent will provide more alerts in the future.