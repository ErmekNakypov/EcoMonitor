# sensor-mock

Tiny Python script that pretends to be an ESP32 air-quality sensor and pushes
randomised readings to the EcoMonitor `/api/v1/sensors/readings` endpoint.

Use it to demo the IoT pipeline without procuring real hardware.

## Requirements

- Python 3.9+
- A registered IoT device in EcoMonitor (Admin → IoT devices → Register
  device). Copy the JWT shown on the "Token issued" page — it is shown only
  once.

## Run

```bash
export ECOMONITOR_URL=http://localhost:5108     # base URL of the running app
export DEVICE_ID=ESP32-A7F3B2                    # device code shown in the admin UI
export DEVICE_TOKEN=eyJhbGciOi...                # JWT from the Token Issued page
export INTERVAL_SECONDS=30                       # optional; default 30s

python3 sensor_mock.py
```

The script POSTs every `INTERVAL_SECONDS` and prints `OK` / `FAIL` per request.

## Generated values

Readings are uniform-random within plausible Bishkek ranges:

| Field         | Range       |
| ------------- | ----------- |
| `pm25`        | 8 – 45 µg/m³ |
| `pm10`        | 15 – 80 µg/m³ |
| `temperature` | 12 – 22 °C |
| `humidity`    | 40 – 85 % |
| `pressure`    | 1005 – 1025 hPa |
| `measuredAt`  | now (UTC, ISO 8601) |

## Notes

- Tokens are long-lived (no expiry); revoke by setting the device status to
  Suspended or Decommissioned in the admin UI.
- Posts to a suspended/decommissioned device return HTTP 400 with
  `{"error":"Device not found or not active"}`.
- The same `(deviceId, measuredAt)` tuple is idempotent server-side — replays
  return success without duplicating the reading.
