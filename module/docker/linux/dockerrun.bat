rem start docker with mapped logs
rem push image: docker push mregen/edgeopcvault:latest
docker run -it -p 58850-58852:58850-58852 -e 58850-58852 -h %COMPUTERNAME% -v "/c/GDS:/root/.local/share/Microsoft/GDS" edgeopcvault:latest --vault="https://vault012-service.azurewebsites.net" --resource="46f44d87-87a2-4d91-ad34-f0ed5d6031ed" --clientid="f5a38dd7-4282-49eb-b1f3-d50b72462588" --secret="ydZ0rxTzsDrik09c4sFRKmF0jgNO0yAB+93vcdRLCs4=" --tenantid="660722d6-c658-431c-8b2e-a157f3134da5"

