## Iks_Admin EN
**Support the author: [DonationAlerts](https://www.donationalerts.com/r/iks__)** <br>
**Admin system with menu**<br><br>
![image](https://github.com/Iksix/Iks_Admin/assets/109164274/66dd6026-66c2-4031-9130-7f03563c8ce6)

## Features
- Admin log to **Discord** and **VK**
- Flexible configuration
- Customizable translations
- The ability to add a custom item to the menu
- Admin chat. Starts with @
- Plugin support and development

## Commands
- `css_admin` -> Open admin menu
### Flag `z`
- ```css_adminadd <sid> <name> <flags/-> <immunity> <group_id> <time> <server_id/ - (ALL SERVERS)>``` -> Add Admin
- Example: ```css_adminadd 76561199124384855 iks bkmgus 50 -1 0 -```
- ```css_admindel <sid>``` -> Delete Admin
- Example: ```css_admindel 76561199124384855```
- `css_reload_admins_cfg` -> Reload admins and plugin config
- `css_reload_admins` -> Reload admins
### Flag `b`
- ```css_ban <#uid/#sid/name> <duration> <reason> <name if needed>``` -> Ban player
- Example: ```css_ban #76561199124384855 0 "Use cheats" iks```
- ```css_banip <#uid/#sid/name/#ip(if offline)> <duration> <reason> <name if needed>``` -> Ban player by ip
- Example: ```css_banip #127.0.0.1 0 "Use cheats" iks```
### Flag `u`
- ```css_unban sid``` -> Unban player
- Example: ```css_unban 76561199124384855```
### Flag `m`
- ```css_mute <#uid/#sid/name> <duration> <reason> <name if needed>``` -> Mute in voice chat
- Example: ```css_mute iks 0 14+ iks```
- ```css_unmute <#uid/#sid/name>``` -> Unmute player
- Example: ```css_unmute #76561199124384855```
### Flag `g`
- ```css_gag <#uid/#sid/name> <duration> <reason> <name if needed>``` -> Mute in chat
- Example: ```css_gag iks 0 Spam iks```
- ```css_gag <#uid/#sid/name>``` -> Ungag player
- Example: ```css_ungag #76561199124384855```
### Flag `k`
- ```css_kick <#uid/#sid/name> <reason>``` -> Kick the player
- Example: ```css_kick iks Some reason```
### Flag `s`
- ```css_hide``` -> Hide you from Scoreboard
- ```css_slay <#uid/#sid/name>``` -> Kill the player
- `css_rename <#uid/#sid/name> <new name>` -> Rename the player
- Example: ```css_slay iks```
- ```css_switchteam <#uid/#sid/name> <ct/t>``` -> Switch player team without killing him
- Example: ```css_switchteam iks t```
- ```css_changeteam <#uid/#sid/name> <ct/t/spec>``` -> Change player team with killing him
- Example: ```css_changeteam iks spec```
- `css_hsay <color> <text>` -> Print message to center on 5sec.
- Example: ```css_hsay red Pls send me discord```
- `css_isay "<img link>"` -> Print image to center. 100% support: `.png ; .jpg ; .gif`
- Example: ```css_isay "Some image link.png"```
### Flags `d`
- `css_respawn <#uid/#sid/name/>` -> Respawn the player
- `css_noclip <#uid/#sid/name/>` -> On/Off noclip to the player
### Server(RCON) only
- `css_rban <sid> <ip/-(Auto)> <adminsid/CONSOLE> <duration> <reason> <BanType (0 - default / 1 - ip> <name if needed>`
- `css_runban <sid> <adminsid/CONSOLE>`
- `css_rmute <sid> <adminsid/CONSOLE> <duration> <reason> <name if needed>`
- `css_runmute <sid> <adminsid/CONSOLE>`
- `css_rgag <sid> <adminsid/CONSOLE> <duration> <reason> <name if needed>`
- `css_rungag <sid> <adminsid/CONSOLE>`

> [!IMPORTANT]
> - ServerID in config must be only 1 symbol

> [!TIP]
> - You can write "ABCD" in ServerID column for admin
> - At the moment, you can add groups only directly to the database.
> - Tested on [CS# v172](https://docs.cssharp.dev/index.html)

## To do...
- [x] Add `css_rename`
- [ ] Add `css_hp`
- [ ] Add `css_map`
- [ ] Add `css_speed`
- [ ] Add `css_psay`
- [x] Add `css_respawn`
- [ ] Add `css_give`
- [ ] Add `css_rcon`
- [x] Add `css_noclip`
- [x] Add `css_hide`
- [ ] Add `css_who`
- [ ] Add possibility to change to workshop map from menu



