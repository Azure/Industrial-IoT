@echo off
setlocal

pushd Boiler\Design
set design=BoilerDesign
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd HistoricalEvents\Design
set design=ModelDesign
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd MemoryBuffer\Design
set design=MemoryBufferDesign
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd SimpleEvents\Design
set design=ModelDesign
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd TestData\Design
set design=TestDataDesign
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd Views\Design
set design=ModelDesign
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd Views\Design
set design=OperationsDesign
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd Views\Design
set design=EngineeringDesign
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd Vehicles\Design
set design=ModelDesign1
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd Vehicles\Design
set design=ModelDesign2
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd

pushd Plc\Design
set design=ModelDesign
echo Building %design%
Opc.Ua.ModelCompiler.exe compile -version v104 -d2 "%design%.xml" -cg "%design%.csv" -o2 "."
popd
