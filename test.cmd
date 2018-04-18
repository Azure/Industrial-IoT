@echo off
shift
call scripts\setenv.cmd -g connected-factory-hmi2018
call scripts\make.cmd -v 1.0.1 -r marcschier -g connected-factory-hmi2018-l -p cf-hmi2018- -l westeurope -s IOT_GERMANY %*