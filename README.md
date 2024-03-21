## IksAdmin EN
**Support the author: [DonationAlerts](https://www.donationalerts.com/r/iks__)** <br>
**Admin system with menu**<br><br>
![изображение](https://github.com/Iksix/Iks_Admin/assets/109164274/b5df9e4f-aeb5-4260-81ba-1916265898a4)

![изображение](https://github.com/Iksix/Iks_Admin/assets/109164274/f2e83b43-a40a-48ad-8093-5a7a1f991620) 



## Features
- Admin log to **Discord** and **VK**
- Flexible configuration
- Customizable translations
- Admin chat. Starts with @
- Plugin support and development

## Commands
- `css_admin` | Flags access: `"admin"` Default: `bkmgstu`
- `css_adminadd <sid> <name> <flags/-> <immunity> <group_id> <time> <server_id/ - (ALL SERVERS)>` | Flags access: `"adminManage"` Default: `z`
- `css_admindel <sid>` | Flags access: `"adminManage"` Default: `z`
- `css_reload_admins` | Flags access: `"adminManage"` Default: `z`
- `css_who <name>` | Flags access: `"who"` Default: `b`
- `css_ban <#uid/#sid/name> <duration> <reason> <name if needed>` | Flags access: `"ban"` Default: `b`
- `css_gag <#uid/#sid/name> <duration> <reason> <name if needed>` | Flags access: `"gag"` Default: `g`
- `css_mute <#uid/#sid/name> <duration> <reason> <name if needed>` | Flags access: `"mute"` Default: `m`
- `css_unmute/ungag/unban <#uid/#sid/name>` | Flags access: `"unmute"/"ungag"/"unban"` Default: `m/g/u`
- `css_kick <#uid/#sid/name> <reason>` | Flags access: `"kick"` Default: `k`
- `css_slay <#uid/#sid/name>` | Flags access: `"slay"` Default: `s`
- `css_switchteam <#uid/#sid/name> <ct/t>` | Flags access: `"switchteam"` Default: `s`
- `css_changeteam <#uid/#sid/name> <ct/t/spec>` | Flags access: `"changeteam"` Default: `s`
- `css_rename <#uid/#sid/name> <new name>` | Flags access: `"rename"` Default: `s`
- `css_hide` | Flags access: `"hide"` Default: `bkmg`
- `css_map <id> <(Workshop Map?) true/false>` | Flags access: `"map"` Default: `z`
### RCON only | Flags access: "rcon" Default: z
- `css_rcon <command>`
- `css_rban <sid> <ip/-(Auto)> <adminsid/CONSOLE> <duration> <reason> <BanType (0 - default / 1 - ip> <name>`
- `css_runban <sid> <adminsid/CONSOLE>`
- `css_rmute <sid> <adminsid/CONSOLE> <duration> <reason> <name>`
- `css_runmute <sid> <adminsid/CONSOLE>`
- `css_rgag <sid> <adminsid/CONSOLE> <duration> <reason> <name>`
- `css_rungag <sid> <adminsid/CONSOLE>`

## Where can I use flag access
For example:
- Let's limit css_hide to the z flag only
- Give the right to manage admins to a custom flag
- We can assign any of several flags to one right
```json
"Flags": {
    "hide": "z",
    "adminManage" : "q",
    "rename" : "bkmgus"
},
```

> [!IMPORTANT]
> - ServerID in config must be only 1 symbol

> [!TIP]
> - You can write "ABCD" in ServerID column for admin
> - At the moment, you can add groups only directly to the database.
> - Tested on [CS# v197](https://docs.cssharp.dev/index.html)

## To do...
- [ ] Pre hooks
- [ ] Advanced Admin Commands module





