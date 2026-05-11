#!/bin/bash
cd "$(dirname "$0")"
if [ ! -f mock.pid ]; then
    echo "No mock.pid found. Mock not running?"
    exit 0
fi

PID=$(cat mock.pid)
if kill -0 "$PID" 2>/dev/null; then
    kill "$PID"
    echo "Sent SIGTERM to pid $PID"
    sleep 1
    if kill -0 "$PID" 2>/dev/null; then
        echo "Process still alive, sending SIGKILL"
        kill -9 "$PID"
    fi
fi
rm -f mock.pid
echo "Stopped"
