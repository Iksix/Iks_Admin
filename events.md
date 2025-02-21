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
### `async` "update_warn" - обновление варна (в бд)
- `"warn" <Warn>`
### `async` "create_warn_pre" - выдача варна
- `"warn" <Warn>`
- `"announce" <bool>`
### `async` "admin_create_pre" - выдача варна
- `"actioneer" <Admin>`
- `"new_admin" <Admin>`
### `async` "admin_delete_post" - выдача варна
- `"actioneer" <Admin>`
- `"new_admin" <Admin>`