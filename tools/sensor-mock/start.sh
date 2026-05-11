#!/bin/bash
# Start sensor mock in background with logging.
set -e
cd "$(dirname "$0")"

if [ -f mock.pid ] && kill -0 "$(cat mock.pid)" 2>/dev/null; then
    echo "Mock already running, pid $(cat mock.pid). Stop with ./stop.sh first."
    exit 1
fi

if [ ! -f sensors.json ]; then
    echo "sensors.json not found. Copy sensors.json.example and fill in tokens."
    exit 1
fi

nohup python3 sensor_mock.py > mock.log 2>&1 &
echo $! > mock.pid
echo "Sensor mock started, pid $(cat mock.pid)"
echo "Logs: tail -f mock.log"
echo "Stop: ./stop.sh"
