#!/usr/bin/env python3
"""
Multi-device mock sensor for EcoMonitor.
Reads sensors.json, runs one thread per configured device.
Use Ctrl+C to stop all devices.
"""
import json
import random
import sys
import threading
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

CONFIG_PATH = Path(__file__).parent / "sensors.json"


class DeviceRunner(threading.Thread):
    def __init__(self, base_url, interval, device_config, stop_event):
        super().__init__(daemon=True)
        self.base_url = base_url
        self.interval = interval
        self.config = device_config
        self.stop_event = stop_event
        self.device_id = device_config["deviceId"]
        self.name = device_config.get("name", self.device_id)
        self.token = device_config["token"]
        self.baseline_pm25 = device_config.get("baselinePm25", 20)
        self.baseline_temp = device_config.get("baselineTemperature", 18)
        self.baseline_humidity = device_config.get("baselineHumidity", 55)
        self.baseline_pressure = device_config.get("baselinePressure", 1015)

    def generate_reading(self):
        def jitter(baseline, pct=0.15):
            return round(baseline * random.uniform(1 - pct, 1 + pct), 1)

        return {
            "measuredAt": datetime.now(timezone.utc).isoformat(),
            "pm25": jitter(self.baseline_pm25),
            "pm10": jitter(self.baseline_pm25 * 1.6, 0.2),
            "temperature": round(self.baseline_temp + random.uniform(-2, 2), 1),
            "humidity": round(self.baseline_humidity + random.uniform(-5, 5), 0),
            "pressure": round(self.baseline_pressure + random.uniform(-3, 3), 0),
        }

    def post_reading(self, reading):
        url = f"{self.base_url}/api/v1/sensors/readings"
        data = json.dumps(reading).encode("utf-8")
        req = urllib.request.Request(
            url,
            data=data,
            headers={
                "Authorization": f"Bearer {self.token}",
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

    def log(self, msg):
        ts = datetime.now().strftime("%H:%M:%S")
        print(f"[{ts}] [{self.name}] {msg}", flush=True)

    def run(self):
        self.log(f"started, posting every {self.interval}s")
        while not self.stop_event.is_set():
            reading = self.generate_reading()
            status, body = self.post_reading(reading)
            if status == 200:
                self.log(f"OK pm25={reading['pm25']:.1f} temp={reading['temperature']:.1f}")
            else:
                self.log(f"FAIL status={status} body={body[:200]}")
            # Sleep but allow stop_event to interrupt
            self.stop_event.wait(self.interval)
        self.log("stopped")


def load_config():
    if not CONFIG_PATH.exists():
        print(f"ERROR: {CONFIG_PATH} not found.")
        print("Copy sensors.json.example to sensors.json and fill in real values.")
        sys.exit(1)
    with open(CONFIG_PATH) as f:
        config = json.load(f)
    if not config.get("devices"):
        print(f"ERROR: no devices configured in {CONFIG_PATH}")
        sys.exit(1)
    return config


def main():
    config = load_config()
    base_url = config.get("baseUrl", "http://localhost:5108")
    interval = config.get("intervalSeconds", 30)
    devices = config["devices"]

    print("EcoMonitor sensor mock")
    print(f"Target: {base_url}")
    print(f"Interval: {interval}s")
    print(f"Devices: {len(devices)}")
    print()

    stop_event = threading.Event()
    threads = [DeviceRunner(base_url, interval, dev, stop_event) for dev in devices]
    for t in threads:
        t.start()

    try:
        while any(t.is_alive() for t in threads):
            time.sleep(1)
    except KeyboardInterrupt:
        print()
        print("Stop requested, waiting for threads to finish current cycle...")
        stop_event.set()
        for t in threads:
            t.join(timeout=5)
        print("All devices stopped")


if __name__ == "__main__":
    main()
