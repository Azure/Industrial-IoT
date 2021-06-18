#!/usr/bin/env bash

#global variables
scriptFolder=$(dirname "$(readlink -f "$0")")
RED='\033[0;31m'
GREEN='\033[0;32m'
NO_COLOR='\033[0m'

echo "Test Substitution"

testEnvFile="${scriptFolder}/testEnvFile.env"
testFileToReplace="${scriptFolder}/testFileToReplace.txt"
testFileReplaced="${scriptFolder}/testFileReplaced.txt"
testExpectedFileReplaced="${scriptFolder}/testExpectedFileReplaced.txt"

echo "This is a test where some variables should be substituted
like variable \$TEST_ENV_1 with all supported characters
but not variable \$TEST_ENV_DO_NOT_REPLACE" > testFileToReplace.txt

echo "This is a test where some variables should be substituted
like variable {}!#$%()*+-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[^_abcdefghijklmnopqrstuvwxyz with all supported characters
but not variable \$TEST_ENV_DO_NOT_REPLACE" > testExpectedFileReplaced.txt

source $testEnvFile
if [[ -z $TEST_ENV_1 ]]; then
    echo "TEST_ENV_1 value is missing. Please verify your TestEnvFile.env file. Exiting."
    exit 1
fi
export TEST_ENV_1
source ${scriptFolder}/../replaceEnv.sh  $testFileToReplace $testFileReplaced 'TEST_ENV_1'

if cmp -s $testFileReplaced $testExpectedFileReplaced; then echo -e "${GREEN}OK${NO_COLOR}"; else echo -e "${RED}NOK${NO_COLOR}"; cat $testFileReplaced; cat $testExpectedFileReplaced; fi

unset TEST_ENV_1
rm $testFileToReplace $testExpectedFileReplaced $testFileReplaced