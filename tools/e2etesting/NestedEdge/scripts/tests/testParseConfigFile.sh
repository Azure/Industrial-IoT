#!/usr/bin/env bash

are_arrays_equal() {
  local -n _array_one=$1
  local -n _array_two=$2
#  printf '1: %q\n' "${_array_one[@]}"
#  printf '2: %q\n' "${_array_two[@]}"
  if [ "${_array_one[*]}" == "${_array_two[*]}" ]; then
    true
  else
    false
  fi
}

RED='\033[0;31m'
GREEN='\033[0;32m'
NO_COLOR='\033[0m'

echo "1. Tests Array Comparison"
echo -n "Test 1.1 "
array1=("test1" "test2")
array2=("test1" "test2")
if are_arrays_equal array1 array2; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 1.2 "
array1=("test" "test2")
array2=("test" "test3")
if ! are_arrays_equal array1 array2; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi

echo "2. Tests Configuration File Parsing"

echo "Test 2.1"
configTest21="./testConfig21.txt"
source ../parseConfigFile.sh $configTest21
expectedIotEdgeDevices=("L5-edge" "L4-edge" "L3-edge")
expectedIotEdgeDevicesSubnets=("7-L5-IT-EnterpriseNetwork" "6-L4-IT-SiteLogistics" "4-L3-OT-SiteOperations")
expectedIotEdgeParentDevices=("IoTHub" "L5-edge" "L4-edge")
expectedIiotAssets=()
expectedIiotAssetsSubnets=()
expectedTAcrEnvFilePath="./ACR.env"
expectedTopLayerBaseDeploymentTemplateFilePath="./edgeDeployments/topLayerBaseDeploymentItProxy.template.json"
expectedMiddleLayerBaseDeploymentFilePath="./edgeDeployments/middleLayerBaseDeployment.json"
expectedBottomLayerBaseDeploymentFilePath="./edgeDeployments/bottomLayerBaseDeploymentOtProxy.json"
expectedRootCA="https://raw.githubusercontent.com/Azure/Industrial-IoT/feature/nested_edge/tools/e2etesting/NestedEdge/scripts/assets/test-certs.tar.bz2"
echo -n "Test 2.1.1 "
if are_arrays_equal iotEdgeDevices expectedIotEdgeDevices; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.2 "
if are_arrays_equal iotEdgeDevicesSubnets expectedIotEdgeDevicesSubnets; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.3 "
if are_arrays_equal iotEdgeParentDevices expectedIotEdgeParentDevices; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.4 "
if are_arrays_equal iiotAssets expectedIiotAssets; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.5 "
if are_arrays_equal iiotAssetsSubnets expectedIiotAssetsSubnets; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.6 "
if [ $acrEnvFilePath=$expectedAcrEnvFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.7 "
if [ $topLayerBaseDeploymentFilePath=$expectedTopLayerBaseDeploymentTemplateFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.8 "
if [ $middleLayerBaseDeploymentFilePath=$expectedMiddleLayerBaseDeploymentFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.9 "
if [ $bottomLayerBaseDeploymentFilePath=$expectedBottomLayerBaseDeploymentFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.10 "
if [ $rootCA=$expectedRootCA ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi

echo "Test 2.2"
configTest22="./testConfig22.txt"
source ../parseConfigFile.sh $configTest22
expectedIotEdgeDevices=("L5-edge" "L4-edge-1" "L4-edge-2" "L3-edge-1" "L3-edge-2")
expectedIotEdgeDevicesSubnets=("7-L5-IT-EnterpriseNetwork" "6-L4-IT-SiteLogistics" "6-L4-IT-SiteLogistics" "4-L3-OT-SiteOperations" "4-L3-OT-SiteOperations")
expectedIotEdgeParentDevices=("IoTHub" "L5-edge" "L5-edge" "L4-edge-1" "L4-edge-1")
expectedIiotAssets=()
expectedIiotAssetsSubnets=()
expectedTAcrEnvFilePath="./ACR.env"
expectedTopLayerBaseDeploymentTemplateFilePath="./edgeDeployments/topLayerBaseDeploymentItProxy.template.jon"
expectedMiddleLayerBaseDeploymentFilePath="./edgeDeployments/middleLayerBaseDeployment.json"
expectedBottomLayerBaseDeploymentFilePath="./edgeDeployments/bottomLayerBaseDeploymentOtProxy.json"
expectedRootCA="https://raw.githubusercontent.com/Azure/Industrial-IoT/feature/nested_edge/tools/e2etesting/NestedEdge/scripts/assets/test-certs.tar.bz2"
echo -n "Test 2.2.1 "
if are_arrays_equal iotEdgeDevices expectedIotEdgeDevices; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.2.2 "
if are_arrays_equal iotEdgeDevicesSubnets expectedIotEdgeDevicesSubnets; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.2.3 "
if are_arrays_equal iotEdgeParentDevices expectedIotEdgeParentDevices; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.2.4 "
if are_arrays_equal iiotAssets expectedIiotAssets; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.2.5 "
if are_arrays_equal iiotAssetsSubnets expectedIiotAssetsSubnets; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.6 "
if [ $acrEnvFilePath=$expectedAcrEnvFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.2.7 "
if [ $topLayerBaseDeploymentFilePath=$expectedTopLayerBaseDeploymentTemplateFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.2.8 "
if [ $middleLayerBaseDeploymentFilePath=$expectedMiddleLayerBaseDeploymentFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.2.9 "
if [ $bottomLayerBaseDeploymentFilePath=$expectedBottomLayerBaseDeploymentFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.2.10 "
if [ $rootCA=$expectedRootCA ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi

echo "Test 2.3"
configTest23="./testConfig23.txt"
source ../parseConfigFile.sh $configTest23
expectedIotEdgeDevices=("L5-edge" "L4-edge" "L3-edge")
expectedIotEdgeDevicesSubnets=("7-L5-IT-EnterpriseNetwork" "6-L4-IT-SiteLogistics" "4-L3-OT-SiteOperations")
expectedIotEdgeParentDevices=("IoTHub" "L5-edge" "L4-edge")
expectedIiotAssets=("L2-OPC-UA-server-1" "L2-OPC-UA-server-2" "L2-OPC-UA-server-3" "L2-OPC-UA-server-4")
expectedIiotAssetsSubnets=("3-L2-OT-AreaSupervisoryControl" "3-L2-OT-AreaSupervisoryControl" "3-L2-OT-AreaSupervisoryControl" "3-L2-OT-AreaSupervisoryControl")
expectedTAcrEnvFilePath="./ACR.env"
expectedTopLayerBaseDeploymentTemplateFilePath="./edgeDeployments/topLayerBaseDeploymentItProxy.template.jon"
expectedMiddleLayerBaseDeploymentFilePath="./edgeDeployments/middleLayerBaseDeployment.json"
expectedBottomLayerBaseDeploymentFilePath="./edgeDeployments/bottomLayerBaseDeploymentOtProxy.json"
expectedRootCA="https://raw.githubusercontent.com/Azure/Industrial-IoT/feature/nested_edge/tools/e2etesting/NestedEdge/scripts/assets/test-certs.tar.bz2"
echo -n "Test 2.3.1 "
if are_arrays_equal iotEdgeDevices expectedIotEdgeDevices; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.3.2 "
if are_arrays_equal iotEdgeDevicesSubnets expectedIotEdgeDevicesSubnets; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.3.3 "
if are_arrays_equal iotEdgeParentDevices expectedIotEdgeParentDevices; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.3.4 "
if are_arrays_equal iiotAssets expectedIiotAssets; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.3.5 "
if are_arrays_equal iiotAssetsSubnets expectedIiotAssetsSubnets; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.1.6 "
if [ $acrEnvFilePath=$expectedAcrEnvFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.3.7 "
if [ $topLayerBaseDeploymentFilePath=$expectedTopLayerBaseDeploymentTemplateFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.3.8 "
if [ $middleLayerBaseDeploymentFilePath=$expectedMiddleLayerBaseDeploymentFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.3.9 "
if [ $bottomLayerBaseDeploymentFilePath=$expectedBottomLayerBaseDeploymentFilePath ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi
echo -n "Test 2.3.10 "
if [ $rootCA=$expectedRootCA ]; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; fi


# # Debugging utility...
# echo "Edge devices: ${iotEdgeDevices[@]}"
# echo "Expected Edge devices: ${expectedIotEdgeDevices[@]}"
# echo "Edge devices subnets: ${iotEdgeDevicesSubnets[@]}"
# echo "Expected Edge devices subnets: ${expectedIotEdgeDevicesSubnets[@]}"
# echo "Parent edge devices: ${iotEdgeParentDevices[@]}"
# echo "Expected Parent edge devices: ${expectedIotEdgeParentDevices[@]}"
# echo "IIOT assets: ${iiotAssets[@]}"
# echo "Expected IIOT assets: ${expectedIiotAssets[@]}"
# echo "IIOT assets subnets: ${iiotAssetsSubnets[@]}"
# echo "Expected IIOT assets subnets: ${expectedIiotAssetsSubnets[@]}"
# echo "acrEnvFilePath: ${acrEnvFilePath}"
# echo "topLayerBaseDeploymentTemplateFilePath: ${topLayerBaseDeploymentTemplateFilePath}"
# echo "middleLayerBaseDeploymentFilePath: ${middleLayerBaseDeploymentFilePath}"
# echo "bottomLayerBaseDeploymentFilePath: ${bottomLayerBaseDeploymentFilePath}"
# echo "rootCA: ${rootCA}"