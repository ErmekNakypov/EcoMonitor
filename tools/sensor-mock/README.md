# EcoMonitor Sensor Mock

Simulates ESP32 air-quality sensors posting to the EcoMonitor API.
Multi-device support: one config file, one process, all devices run concurrently.

## Setup

1. Register devices in the admin panel at `/Admin/Devices` and copy each
   device token (shown once after creation).
2. Copy the config template:

   ```bash
   cp sensors.json.example sensors.json
   ```

3. Edit `sensors.json`: paste each device's id and token, adjust baseline
   values if you want different AQI levels per device.

## Run (foreground, see live output)

```bash
python3 sensor_mock.py
```

Press Ctrl+C to stop. All devices stop together.

## Run (background)

The first time, make the wrappers executable:

```bash
chmod +x start.sh stop.sh
```

Then:

```bash
./start.sh        # starts in background, logs to mock.log, pid in mock.pid
tail -f mock.log  # follow logs
./stop.sh         # stop
```

## Config

Edit `sensors.json`. Each device entry has:

- `deviceId` — matches the admin-registered device id (`ESP32-XXXXXX`)
- `token` — JWT shown on the device's "Token issued" page
- `name` — display label in logs (any string)
- `baselinePm25`, `baselineTemperature`, `baselineHumidity`,
  `baselinePressure` — readings jitter randomly around these baselines
  (±15% on PM, ±2°C on temperature, ±5% on humidity, ±3 hPa on pressure)

Baseline values let you simulate different AQI conditions per device.
Higher `baselinePm25` produces a redder marker on the map.

Top-level config keys:

- `baseUrl` — EcoMonitor base URL (defaults to `http://localhost:5108`)
- `intervalSeconds` — how often each device posts (defaults to `30`)

## Notes

- Tokens are long-lived (no expiry); revoke by setting the device status to
  Suspended or Decommissioned in the admin UI.
- Posts to a suspended/decommissioned device return HTTP 400 with
  `{"error":"Device not found or not active"}`.
- The same `(deviceId, measuredAt)` tuple is idempotent server-side — replays
  return success without duplicating the reading.

## Security

`sensors.json` contains real device tokens and is gitignored.
Never commit it.
