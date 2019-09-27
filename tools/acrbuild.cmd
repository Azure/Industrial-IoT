@echo off
shift
powershell ./scripts/acr-matrix.ps1 -Build %*
