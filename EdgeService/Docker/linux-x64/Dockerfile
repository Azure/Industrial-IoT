FROM microsoft/dotnet:2.0.0-runtime-stretch

ARG EXE_DIR=.

WORKDIR /app

COPY $EXE_DIR/ ./

CMD ["dotnet", "EdgeService.dll"]