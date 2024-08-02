## IksAdmin 2.0
**Support the author: [DonationAlerts](https://www.donationalerts.com/r/iks__)** <br>
**required: [MenuManager](https://csdevs.net/resources/menumanager.726/) <br>**
**Admin system with menu**<br><br>
![изображение](https://github.com/Iksix/Iks_Admin/assets/109164274/b5df9e4f-aeb5-4260-81ba-1916265898a4)

![изображение](https://github.com/Iksix/Iks_Admin/assets/109164274/f2e83b43-a40a-48ad-8093-5a7a1f991620) 

![изображение](https://github.com/Iksix/Iks_Admin/assets/109164274/8f6490c3-4f10-4c25-a792-2d91766d09c2)


## Features
- The ability to create modules
- Flexible configuration
- Customizable translations
- Admin chat. Starts with @
- Plugin support and development

## Commands
- `css_admin` | Flags access: `"admin"` Default: `bkmgsut`
- `css_adminadd <sid> <name> <flags/-(From group)> <immunity/-1(From group)> <group_id/-1(Group disabled)> <time> <server_id/ - (ALL SERVERS)>` | Flags access: `"adminManage"` Default: `z`
- `css_admindel <sid>` | Flags access: `"adminManage"` Default: `z`
- `css_reload_admins` | Flags access: `"adminManage"` Default: `z`
- `css_group_add <name> <flags> <immunity>` | FlagAccess: `"groupManage"`, default: `"z"`
- `css_group_del <name>` | Flag access: `"groupManage"`, default: `"z"`
- `css_group_list` | Flag access: `"groupManage"`, default: `"z"`
- `css_admin_reload_cfg` | `Flags Access: "reload_cfg", Default: "z"`
- `css_who <name>` | Flags access: `"who"` Default: `b`
- `css_ban <#uid/#sid/name> <duration> <reason> <name if needed>` | Flags access: `"ban"` Default: `b`
- `css_banip <$ip/#uid/#sid/name> <duration> <reason> <name if needed>` | FlagAccess: `"banip"`, default: `"b"`
- `css_gag <#uid/#sid/name> <duration> <reason> <name if needed>` | Flags access: `"gag"` Default: `g`
- `css_silence <#uid/#sid/name> <duration> <reason> <name if needed>` | `Flags Access: "silence", Default: "gm"`
- `css_unsilence <#uid/#sid/name>` | `Flags Access: "unsilence", Default: "gm"`
- `css_mute <#uid/#sid/name> <duration> <reason> <name if needed>` | Flags access: `"mute"` Default: `m`
- `css_unban <sid/ip>` | Flags access: `"unban"` Default: `u`
- `css_unmute/ungag <#uid/#sid/namep>` | Flags access: `"unmute"/"ungag"` Default: `m/g`
- `css_kick <#uid/#sid/name> <reason>` | Flags access: `"kick"` Default: `k`
- `css_slay <#uid/#sid/name>` | Flags access: `"slay"` Default: `s`
- `css_switchteam <#uid/#sid/name> <ct/t>` | Flags access: `"switchteam"` Default: `t`
- `css_changeteam <#uid/#sid/name> <ct/t/spec>` | Flags access: `"changeteam"` Default: `t`
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

> [!TIP]
> - You can write "1;2;3;4" in ServerID column for admin
> - At the moment, you can add groups only directly to the database.
> - Tested on [CS# v215](https://docs.cssharp.dev/index.html)

## API connect:
```csharp
private readonly PluginCapability<IIksAdminApi> _pluginCapability = new("iksadmin:core");
public static IIksAdminApi? AdminApi;
public override void OnAllPluginsLoaded(bool hotReload)
{
    AdminApi = _pluginCapability.Get();
}
```

## To do...
- [ ] Pre hooks
- [ ] Advanced Admin Commands module





