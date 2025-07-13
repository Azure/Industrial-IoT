#!/bin/bash

#
# Start ssh server to accept incoming debugger connections on port 22.
#
echo "Starting SSH server..."
/usr/sbin/sshd

#
# Workaround for the debugged container not sharing the /tmp directory with
# the debugger which VS Debugger requires to set up the ipc to the process
# being debugged. See https://github.com/microsoft/MIEngine/issues/1488 and
# https://github.com/dotnet/runtime/issues/37444 for more information.
#
if [ -n "$1" ]; then
    PROCESS="$1"
elif [ -n "$__DEBUG_TARGET" ]; then
    PROCESS="$__DEBUG_TARGET"
else
    PROCESS="dotnet"
fi
CUR=
while true; do
    # Check if the process is running
    PID=$(pgrep -f $PROCESS)
    if [ -z "$PID" ]; then
        if [ -n "$CUR" ]; then
            echo "$PROCESS process $CUR stopped."
            CUR=
        fi
        sleep 2s
    elif [ "$PID" != "$CUR" ]; then
        if [ -z "$CUR" ]; then
            echo "$PROCESS process $PID started."
        else
            echo "$PROCESS process $CUR restarted as $PID."
        fi
        CUR=$PID
        # Update the TMPDIR environment variable for all users in bashrc
        echo "Setting TMPDIR to /proc/$CUR/root/tmp for the current user..."
        if grep -q "export TMPDIR=" ~/.bashrc; then
            sed -i "s|^export TMPDIR=.*|export TMPDIR=/proc/$CUR/root/tmp|" ~/.bashrc
        else
            echo "export TMPDIR=/proc/$CUR/root/tmp" >> ~/.bashrc
        fi
        ARGS=$(ps -o args= -p $CUR)
        ARGS=$(set $ARGS && shift && echo $1)
        echo "$PROCESS started with PID $CUR and arguments '$ARGS'"
        echo "Waiting..."
        sleep 3s
        # point to the dotnet runtime in the debugged process
        echo "Linking /usr/share/dotnet to /proc/$CUR/root/usr/share/dotnet"
        ln -sf /proc/$CUR/root/usr/share/dotnet /usr/share/dotnet
        # Try to make the dll/pdb files available to the debugger process...
        ARG_PATH=$(dirname "${ARGS}")
        mkdir -p $ARG_PATH
        find $ARG_PATH/ -type l -delete
        echo "Linking files from /proc/$CUR/root$ARG_PATH/ to $ARG_PATH/"
        FILES=$(find /proc/$CUR/root$ARG_PATH/ \
            -regextype posix-extended -regex '.*\.(pdb|dll)$' \
            -exec realpath --relative-to=/proc/$CUR/root/ {} \;)
        for i in $FILES; do
            echo "Linking /proc/$CUR/root/$i to /$i"
            ln -sf /proc/$CUR/root/$i /$i
        done
    else
        sleep 10s
    fi
done
