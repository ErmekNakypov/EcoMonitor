#!/usr/bin/env python3
"""
Mock ESP32 sensor for EcoMonitor.
Reads device id and JWT token from environment, sends fake readings on an interval.

Usage:
    export ECOMONITOR_URL=http://localhost:5108
    export DEVICE_ID=ESP32-A7F3B2
    export DEVICE_TOKEN=eyJhbG...
    python sensor_mock.py
"""
import json
import os
import random
import sys
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone

BASE_URL = os.environ.get("ECOMONITOR_URL", "http://localhost:5108")
DEVICE_ID = os.environ.get("DEVICE_ID")
DEVICE_TOKEN = os.environ.get("DEVICE_TOKEN")
INTERVAL_SECONDS = int(os.environ.get("INTERVAL_SECONDS", "30"))

if not DEVICE_ID or not DEVICE_TOKEN:
    print("ERROR: DEVICE_ID and DEVICE_TOKEN environment variables required")
    sys.exit(1)

print(f"Mock sensor {DEVICE_ID} starting")
print(f"Posting to {BASE_URL}/api/v1/sensors/readings every {INTERVAL_SECONDS}s")
print("Press Ctrl+C to stop")
print()


def generate_reading():
    return {
        "measuredAt": datetime.now(timezone.utc).isoformat(),
        "pm25": round(random.uniform(8, 45), 1),
        "pm10": round(random.uniform(15, 80), 1),
        "temperature": round(random.uniform(12, 22), 1),
        "humidity": round(random.uniform(40, 85), 0),
        "pressure": round(random.uniform(1005, 1025), 0),
    }


def post_reading(reading):
    url = f"{BASE_URL}/api/v1/sensors/readings"
    data = json.dumps(reading).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=data,
        headers={
            "Authorization": f"Bearer {DEVICE_TOKEN}",
            "Content-Type": "application/json",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            return resp.status, resp.read().decode("utf-8")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8")
    except Exception as e:
        return None, str(e)


try:
    while True:
        reading = generate_reading()
        status, body = post_reading(reading)
        ts = datetime.now().strftime("%H:%M:%S")
        if status == 200:
            print(f"[{ts}] OK pm25={reading['pm25']:.1f} temp={reading['temperature']:.1f}")
        else:
            print(f"[{ts}] FAIL status={status} body={body[:200]}")
        time.sleep(INTERVAL_SECONDS)
except KeyboardInterrupt:
    print("\nStopped")
