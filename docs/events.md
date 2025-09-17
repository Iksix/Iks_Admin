## Список ивентов DynamicEvents:
Ивенты с _pre так же имеют _post версию
### "c_team_player_pre" - смена команды с убийством
- `"player" <CCSPlayerController>`
- `"admin" <Admin>`
- `"announce" <bool>`
- `"team" <int>`
### "s_team_player_pre" - смена команды без убийства
- `"player" <CCSPlayerController>`
- `"admin" <Admin>`
- `"announce" <bool>`
- `"team" <int>`
### "respawn_player_pre" - респавн игрока
- `"player" <CCSPlayerController>`
- `"admin" <Admin>`
- `"announce" <bool>`
### "kick_player_pre" - кик игрока
- `"player" <CCSPlayerController>`
- `"admin" <Admin>`
- `"announce" <bool>`
- `"reason" <string>`
### "slay_player_pre" - убийство игрока
- `"player" <CCSPlayerController>`
- `"admin" <Admin>`
- `"announce" <bool>`
### "rename_player_pre" - переименовать игрока
- `"admin" <Admin>`
- `"player" <CCSPlayerController>`
- `"name" <string>`
- `"announce" <bool>`
### `async` "update_warn" - обновление варна (в бд)
- `"warn" <Warn>`
### `async` "create_warn_pre" - выдача варна
- `"warn" <Warn>`
- `"announce" <bool>`
### `async` "admin_create_pre" - создание админа
- `"actioneer" <Admin>`
- `"new_admin" <Admin>`
### `async` "admin_delete_pre" - удаление админа
- `"actioneer" <Admin>`
- `"new_admin" <Admin>`
### `"disconnect_player_pre"` - Вызывается при использование функции DisconnectPlayers из AdminApi
- `"player" <CCSPlayerController>` 
- `"reason" <string>` 
- `"instantly" <bool>` 
- `"custom_message_template" <string?>` 
- `"admin" <Admin?>` 
- `"custom_by_admin_template" <string?>` 
- `"disconnection_reason" <NetworkDisconnectionReason?>` 
- `"disconnected_by" <string>` 
