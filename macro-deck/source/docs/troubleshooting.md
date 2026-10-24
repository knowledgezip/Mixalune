# Troubleshooting

- **Cannot connect:** start an API-enabled Mixalune build. Open API Settings, enable the local API and apply. Match the plugin port. Older installed builds without this panel cannot be controlled by this plugin.
- **Authentication failed:** copy the current token from Mixalune and reconfigure. Regenerating a token invalidates the previous one. Do not paste tokens into issue reports.
- **HTTP connection failure:** use the fixed IPv4 loopback endpoint. No firewall exception, remote IP, URL token, or administrator rights should be needed.
- **Not configured:** finish the plugin setup and its connection test. Reconnect cannot create missing credentials.
- **Unavailable channel:** reopen the action, preserve or explicitly replace its saved ID. No fallback to a similarly named channel occurs.
- **Unavailable function:** the plugin checks capabilities and respects API denial. Updating a button cannot unlock licensing.
- **Wrong volume impression:** plugin percentages are a fraction of fader travel. Mixalune's native gain percentages can exceed 100%; the API retains that boost behavior.
- **Muted button does not change:** adopt the mute action as the button's state provider. Allow about one second for the host to read the event-fed cache.
- **Timeout after pressing mute:** the command may already have executed. The plugin disconnects and resynchronizes, never replays the toggle. Check the confirmed state before pressing again.
- **Edition unknown:** expected for the inspected source, which returns `edition: null`.
- **After sleep:** WebSocket keepalive and heartbeat detect a broken connection, then backoff/reconnect restores state. Physical sleep/resume remains an acceptance test.

For bug reports include plugin, Macro Deck and Mixalune versions, port, safe last-error text, and steps. Never include API tokens or credential stores.


