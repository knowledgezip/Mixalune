# Configuration

In Macro Deck, configure the Mixalune Control integration. It allows one connection configuration.

| Field | Value |
|---|---|
| Host | Fixed `127.0.0.1` |
| Port | Default 18765; 1024–65535 |
| Authentication token | Copy from Mixalune's API Settings |
| Connection timeout | 1–60 seconds, default 5 |
| Request timeout | 1–60 seconds, default 5 |

Submitting setup performs an authenticated connection test, verifies API version, and retrieves capabilities before completing. The token uses a secret field and `ConfigFlowValue.Secret`; only `GetSecretAsync` reads it later. The plugin writes no token file and emits no token diagnostics.

The setup description shows the current cached connection status, application version, required API version, reported edition, channel count, and last safe error. Reopen configuration to refresh this summary; it is not a continuously rendered status dashboard. Live status is also exposed through variables.

Use the host integration enable/disable control to start/stop the plugin's lifecycle. **Reconnect to Mixalune** restarts the existing configured connection and returns “reconnecting,” not an invented connection success. A successful connection sets `mixalune_connected` true only after synchronization.

Mixalune need not start first after configuration is saved. Retries use delays of approximately 1, 2, 4, 8, 16, then 30 seconds with jitter. The plugin does not launch Mixalune. Closing the host cancels network tasks and pending requests.

The protocol authenticates within five seconds after socket upgrade, regardless of a longer configured connection timeout. There is no token in the URL, no browser Origin, and no remote-host option.

