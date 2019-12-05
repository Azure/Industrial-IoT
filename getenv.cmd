@echo off
shift
powershell ./tools/scripts/get-env.ps1 %* > .env
goto :eof
