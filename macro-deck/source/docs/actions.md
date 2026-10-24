# Actions and example buttons

| Action | Settings | API command |
|---|---|---|
| Set Channel Volume | Channel, output fader, 0–100% | `channel.setVolume` |
| Volume Up | Channel, output fader, increment (default 5%) | `channel.adjustVolume` |
| Volume Down | Channel, output fader, increment (default 5%) | `channel.adjustVolume` |
| Toggle Channel Mute | Channel | `channel.toggleMute` |
| Set Channel Mute | Channel, On/Off | `channel.setMute` |
| Toggle Microphone Mute | None | `microphone.toggleMute`, or discovered microphone ID with `channel.toggleMute` |
| Reconnect to Mixalune | None | New authenticated session and synchronization |

Channels are discovered. Names are displayed, stable IDs are saved. An unavailable saved ID is shown as unavailable in the dropdown and execution fails clearly; it is never replaced automatically.

Volume percentage is divided by 100. Down sends a negative delta; Mixalune performs atomic adjustment and clamping. Game/stream is the default fader; monitor is independently selectable. Mute affects both outputs.

## Button examples

1. **Voice mute:** add Toggle Microphone Mute to the press flow. Adopt that action as the button's state provider. Muted is rose, unmuted teal, unavailable gray.
2. **Music +5:** add Volume Up, choose the discovered music/application channel, Game / stream, 5%.
3. **Monitor 25%:** add Set Channel Volume, select a channel, Monitor, 25%.
4. **Always muted:** add Set Channel Mute, choose the channel, On. Repeated presses stay muted.
5. **Reconnect:** add Reconnect to Mixalune and adopt its connected/disconnected state provider.

Macro Deck asks state providers for cached state about once per second while displayed. The plugin never performs network requests from a state or variable read. Choose a provider in the button editor; an action cannot force itself to own a button.

For numeric text, use the variable picker in a button label. Example:
`Volume: {{ vars.mixalune_channel_6d69632d31_volume }}%` refers to the discovered ID `mic-1` if that ID exists. This is an example, not a hardcoded channel selection.

## Variables

- `mixalune_connected`, `mixalune_status`, `mixalune_last_error`
- `mixalune_edition`, `mixalune_version`, `mixalune_api_version`, `mixalune_channel_count`
- `mixalune_microphone_muted`
- `mixalune_channel_<key>_volume`, `mixalune_channel_<key>_monitor_volume`, `mixalune_channel_<key>_muted`

Keys encode UTF-8 stable IDs as lowercase hexadecimal (long IDs use a stable SHA-256-derived key). Values become unavailable on disconnect. Removed channels leave the declarations; reads of stale IDs remain unavailable. Edition is “Not reported” when the API supplies no edition.

Icons are supplied in the plugin's Assets folder. The package uses Mixalune's existing application icon. State colors and variable labels use supported SDK mechanisms; this release does not implement a dynamic icon provider.

There are no soundboard, EQ, routing, profile, or Plus/Pro placeholder actions.

