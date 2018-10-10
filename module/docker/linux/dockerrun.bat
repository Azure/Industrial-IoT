rem start docker with mapped logs
rem push image: docker push mregen/edgegds:latest
docker run -it -p 58850-58852:58850-58852 -e 58850-58852 -h edgegds -v "/c/GDS:/root/.local/share/Microsoft/GDS" edgegds:latest --g http://opcvault.azurewebsites.net/"
