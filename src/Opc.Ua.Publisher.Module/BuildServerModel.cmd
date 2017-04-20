@echo off
setlocal

REM These lines will build the production line station model if the batch file is run from the Station directory

rem ensure Opc.Ua.ModelCompiler.exe is in the path
for %%X in (Opc.Ua.ModelCompiler.exe) do (set FOUND=%%~$PATH:X)
if not defined FOUND goto error

echo Building Station
Opc.Ua.ModelCompiler.exe -d2 ".\PublisherModel.xml" -cg ".\PublisherModel.csv" -o2 "."

exit /b 0

:error
echo Opc.Ua.ModelCompiler.exe not found
echo cannot compile the station models
exit /b 1