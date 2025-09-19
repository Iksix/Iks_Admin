using System.Net;
using CoreRCON;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using IksAdmin.Commands;
using IksAdmin.Functions;
using IksAdmin.Menu;
using IksAdminApi;
using Microsoft.Extensions.Localization;
using MySqlConnector;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using CoreConfig = IksAdminApi.CoreConfig;

namespace IksAdmin;

public class AdminApi : IIksAdminApi
{


    public IksAdminApi.CoreConfig Config { get; set; } 
    public BasePlugin Plugin { get; set; } 
    public IStringLocalizer Localizer { get; set; }
    public Dictionary<string, SortMenu[]> SortMenus { get; set; } = new();
    public string ModuleDirectory { get; set; }

    public Dictionary<ulong, Admin> ServerAdmins {get; set;} = new();

    public List<Admin> AllAdmins { get; set; } = new();
    public List<ServerModel> AllServers { get; set; } = new();
    public ServerModel ThisServer { get; set; } = null!;
    public Dictionary<string, Dictionary<string, string>> RegistredPermissions {get; set;} = new();
    public List<Group> Groups { get; set; } = new();
    public List<GroupLimitation> GroupLimitations {get; set;} = new();
    public Admin ConsoleAdmin { get; set; } = null!;
    public string DbConnectionString {get; set;}
    public Dictionary<CCSPlayerController, Action<string>> NextPlayerMessage {get; set;} = new();
    public List<AdminModule> LoadedModules {get; set;} = new();
    public async Task<DBResult> CreateAdmin(Admin actioneer, Admin admin, int? serverId)
    {
        try
        {
            var admins = await DBAdmins.GetAllAdmins(serverId, false);

            var existingAdmin = admins.FirstOrDefault(x =>
                x != null && x.SteamId == admin.SteamId && (serverId == null ? true : x.Servers.Contains(serverId)));

            var eventData = new EventData("admin_create_pre");
            eventData.Insert<Admin>("actioneer", actioneer);
            eventData.Insert<Admin>("new_admin", admin);

            if (eventData.Invoke() != HookResult.Continue)
            {
                return new DBResult(null, 2, "stopped by event handler");
            }

            actioneer = eventData.Get<Admin>("actioneer");
            admin = eventData.Get<Admin>("new_admin");

            if (
                // Проверка существует ли админ с таким же serverId как у добавляемого
                existingAdmin != null
            )
            {
                // Если да то обновляем админа в базе

                
                admin.Id = existingAdmin.Id;
                admin.DeletedAt = null;
                await UpdateAdmin(actioneer, admin);

                // Но нужна доп. проверка если serverId null
                if (serverId == null && !existingAdmin.Servers.Contains(serverId))
                {
                    await RemoveServerIdsFromAdmin(existingAdmin.Id);
                    await AddServerIdToAdmin(existingAdmin.Id, null);
                }
                await ReloadDataFromDb();
                return new DBResult(admin.Id, 1, "admin has been updated");
            }

            // Если нет то добавляем админа и севрер айди к нему
            var newAdmin = await DBAdmins.AddAdminToBase(admin);
            await AddServerIdToAdmin(newAdmin.Id, serverId);
            await ReloadDataFromDb();
            eventData.Invoke("admin_create_post");
            return new DBResult(newAdmin.Id, 0, "Admin has been added");
        }
        catch (Exception e)
        {
            return new DBResult(null, -1, e.ToString());
        }
    }

    public async Task AddServerIdToAdmin(int adminId, int? serverId)
    {
        await DBAdmins.AddServerIdToAdmin(adminId, serverId);
    }

    public async Task RemoveServerIdFromAdmin(int adminId, int serverId)
    {
        await DBAdmins.RemoveServerIdFromAdmin(adminId, serverId);
    }

    public async Task RemoveServerIdsFromAdmin(int adminId)
    {
        await DBAdmins.RemoveServerIdsFromAdmin(adminId);
    }

    public async Task<DBResult> DeleteAdmin(Admin actioneer, Admin admin, bool announce = true)
    {
        try
        {
            AdminUtils.LogDebug(actioneer.Name);
            var eventData = new EventData("admin_delete_pre");
            eventData.Insert<Admin>("actioneer", actioneer);
            eventData.Insert<Admin>("new_admin", admin);
            eventData.Insert<bool>("announce", announce);
            if (eventData.Invoke() != HookResult.Continue)
            {
                return new DBResult(null, 2, "DeleteAdmin stopped by event handler");
            }
            actioneer = eventData.Get<Admin>("actioneer");
            admin = eventData.Get<Admin>("new_admin");
            announce = eventData.Get<bool>("announce");
            await DBAdmins.DeleteAdmin(admin.Id);
            await ReloadDataFromDb();
            if (announce)
            {
                Server.NextWorldUpdate(() => {
                    MsgAnnounces.AdminDeleted(actioneer, admin);
                });
            }
            eventData.Invoke("admin_delete_post");
            return new DBResult(null, 0);
        }
        catch (System.Exception e )
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
        
    }

    public async Task<DBResult> UpdateAdmin(Admin actioneer, Admin admin)
    {
        await DBAdmins.UpdateAdminInBase(admin);
        await ReloadDataFromDb();
        return new DBResult(admin.Id, 0, "Admin has been updated");
    }
    public async Task<DBResult> UpdateAdmin(Admin actioneer, Admin admin, bool updateOnServers)
    {
        await DBAdmins.UpdateAdminInBase(admin);
        if (updateOnServers)
            await ReloadDataFromDb();
        return new DBResult(admin.Id, 0, "Admin has been updated");
    }
    
    
    public async Task<List<Admin>> GetAdminsBySteamId(string steamId, bool ignoreDeleted = true)
    {
        return (await DBAdmins.GetAllAdminsBySteamId(steamId, ignoreDeleted));
    }

    private string _commandInitializer = "core";

    public Dictionary<string, List<CommandModel>> RegistredCommands {get; set;} = new Dictionary<string, List<CommandModel>>();
    public List<PlayerComm> Comms {get; set; } = new();
    public List<Warn> Warns {get; set;} = new();

    // CONFIGS ===
    private IksAdminApi.CoreConfig _adminConfig {get; set;} = new ();

    public List<PlayerInfo> DisconnectedPlayers {get; set;} = new();
    public List<AdminToServer> AdminsToServer {get; set;} = new();

    public List<Admin> HidenAdmins { get; set; } = new();
  

    public AdminApi(BasePlugin plugin, IStringLocalizer localizer, string moduleDirectory)
    {
        Plugin = plugin;
        ReloadConfigs();
        var builder = new MySqlConnectionStringBuilder();
        builder.Password = Config!.Password;
        builder.Server = Config.Host;
        builder.Database = Config.Database;
        builder.UserID = Config.User;
        builder.Port = uint.Parse(Config.Port);
        builder.ConnectionTimeout = 5;
        DB.ConnectionString = builder.ConnectionString;
        Localizer = localizer;
        ModuleDirectory = moduleDirectory;
        DbConnectionString = builder.ConnectionString;
        Task.Run(async () => {
            await ReloadDataFromDb();
        });
    }

    public void ReloadConfigs()
    {
        new KicksConfig().Set();
        _adminConfig.Set();
        Config = CoreConfig.Config;
        new BansConfig().Set();
        new MutesConfig().Set();
        new GagsConfig().Set();
        new SilenceConfig().Set();
    }

    public async Task ReloadDataFromDb(bool onAllServers = true)
    {
        
        var serverModel = new ServerModel(
            Config.ServerId,
            Config.ServerIp,
            Config.ServerName,
            Config.RconPassword
        );
        var oldAdmins = ServerAdmins.ToArray();
        try
        {
            AdminUtils.LogDebug("Init Database");
            await DB.Init();
            AdminUtils.LogDebug("Refresh Servers");
            await DBServers.Add(serverModel);
            AllServers = await DBServers.GetAll();
            ThisServer = AllServers.First(x => x.Id == serverModel.Id);
            AdminUtils.LogDebug("Refresh Admins");
            await RefreshAdmins();
            Warns = await DBWarns.GetAllActive();
            if (onAllServers)
            {
                _ = SendRconToAllServers("css_am_reload", true);
            }
            Server.NextWorldUpdate(() => {
                List<int> adminsSlots = new();
                foreach (var admin in oldAdmins)
                {
                    if (admin.Value.Controller == null) return;
                    adminsSlots.Add(admin.Value.Controller.Slot);
                }
                foreach (var slot in adminsSlots)
                {
                    var adminWithSameSlot = ServerAdmins.FirstOrDefault(x => x.Value.Controller != null && x.Value.Controller.Slot == slot).Value;
                    if (adminWithSameSlot == null || adminWithSameSlot.IsDisabled)
                    {
                        var player = Utilities.GetPlayerFromSlot(slot);
                        if (player != null)
                        {
                            CloseMenu(player);
                        }
                    }
                }
            });
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public void CloseMenu(CCSPlayerController player)
    {
        if (Main.MenuApi != null)
        {
            Main.MenuApi.CloseMenu(player);
        }

        if (Config.MenuType == 4)
        {
            CS2ScreenMenuAPI.MenuAPI.CloseActiveMenu(player);
        }

        CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
    }

    public void ApplyCommForPlayer(PlayerComm comm)
    {
        switch (comm.MuteType)
        {
            case 0:
                MutePlayerInGame(comm);
                break;
            case 1:
                GagPlayerInGame(comm);
                break;
            case 2:
                SilencePlayerInGame(comm);
                break;
        }
    }

    private void SilencePlayerInGame(PlayerComm comm)
    {
        var player = PlayersUtils.GetControllerBySteamIdUnsafe(comm.SteamId);
        if (player == null) return;
        player.VoiceFlags = VoiceFlags.Muted;
        Comms.Add(comm);
    }

    public void RemoveCommFromPlayer(PlayerComm comm)
    {

        switch (comm.MuteType)
        {
            case 0:
                UnmutePlayerInGame(comm);
                break;
            case 1:
                UnGagPlayerInGame(comm);
                break;
            case 2:
                UnSilencePlayerInGame(comm);
                break;
        }
    }

    public IDynamicMenu CreateMenu(string id, string title, MenuType? type = null, MenuColors titleColor = MenuColors.Default, PostSelectAction postSelectAction = PostSelectAction.Nothing, Action<CCSPlayerController>? backAction = null, IDynamicMenu? backMenu = null)
    {
        if (type == null) type = (MenuType)Config.MenuType;
        return new DynamicMenu(id, title, (MenuType)type, titleColor, postSelectAction, backAction, backMenu);
    }
    public IDynamicMenuOption CreateMenuOption(string id, string title, Action<CCSPlayerController, IDynamicMenuOption> onExecute, MenuColors? color = null, bool disabled = false, string viewFlags = "*")
    {
        return new DynamicMenuOption(id, title, onExecute, color, disabled, viewFlags);
    }

    public void RegisterPermission(string key, string defaultFlags)
    {
        // example key = "admin_manage.add"
        var firstKey = key.Split(".")[0]; // admin_manage
        var lastKey = string.Join(".", key.Split(".").Skip(1)); // add
        if (RegistredPermissions.ContainsKey(firstKey))
        {
            var perms = RegistredPermissions[firstKey];
            if (!perms.ContainsKey(lastKey))
            {
                perms.Add(lastKey, defaultFlags);
            }
        } else {
            RegistredPermissions.Add(firstKey, new Dictionary<string, string> { { lastKey, defaultFlags } });
        }
    }
    public string GetCurrentPermissionFlags(string key)
    {
        return AdminUtils.GetCurrentPermissionFlags(key);
    }
    public string GetCurrentPermissionFlags(string[] keys)
    {
        string result = "";
        foreach(string key in keys)
        {
            result += GetCurrentPermissionFlags(key);
        }
        return result;
    }

    // EVENTS ===
 
    public event IIksAdminApi.MenuOpenHandler? MenuOpenPre;
    public bool OnMenuOpenPre(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu)
    {
        var result = MenuOpenPre?.Invoke(player, menu, gameMenu) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            AdminUtils.LogDebug("Some event handler stopped menu opening | Id: " + menu.Id);
            return false;
        }
        return true;
    }
    public event IIksAdminApi.MenuOpenHandler? MenuOpenPost;
    public bool OnMenuOpenPost(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu)
    {
        var result = MenuOpenPost?.Invoke(player, menu, gameMenu) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            return false;
        }
        return true;
    }
    public event IIksAdminApi.OptionRenderHandler? OptionRenderPre;
    public bool OnOptionRenderPre(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option)
    {
        var result = OptionRenderPre?.Invoke(player, menu, gameMenu, option) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            AdminUtils.LogDebug("Some event handler skipped option render | Id: " + option.Id);
            return false;
        }
        return true;
    }
    public event IIksAdminApi.OptionRenderHandler? OptionRenderPost;
    public bool OnOptionRenderPost(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option)
    {
        var result = OptionRenderPost?.Invoke(player, menu, gameMenu, option) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            return false;
        }
        return true;
    }
    public event IIksAdminApi.OptionExecuted? OptionExecutedPre;
    public bool OnOptionExecutedPre(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option)
    {
        var result = OptionExecutedPre?.Invoke(player, menu, gameMenu, option) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            AdminUtils.LogDebug("Some event handler stopped option executed | Id: " + option.Id);
            return false;
        }
        return true;
    }
    public event IIksAdminApi.OptionExecuted? OptionExecutedPost;
    public event IIksAdminApi.DynamicEvent? OnDynamicEvent;
    public HookResult InvokeDynamicEvent(EventData data)
    {
        return OnDynamicEvent?.Invoke(data) ?? HookResult.Continue;
    }

    public event IIksAdminApi.BanHandler? OnBanPre;
    public event IIksAdminApi.BanHandler? OnBanPost;
    public event IIksAdminApi.UnBanHandler? OnUnBanPre;
    public event IIksAdminApi.UnBanHandler? OnUnBanPost;
    public event IIksAdminApi.UnBanHandler? OnUnBanIpPre;
    public event IIksAdminApi.UnBanHandler? OnUnBanIpPost;
    public event IIksAdminApi.CommHandler? OnCommPre;
    public event IIksAdminApi.CommHandler? OnCommPost;
    public event IIksAdminApi.UnCommHandler? OnUnCommPre;
    public event IIksAdminApi.UnCommHandler? OnUnCommPost;
    public event Action? OnReady;
    public event Action<AdminModule>? OnModuleUnload;
    public event Action<AdminModule>? OnModuleLoaded;
    public event Action<string, string>? OnFullConnect;
    public event IIksAdminApi.OnCommandUsed? OnCommandUsedPre;
    public event IIksAdminApi.OnCommandUsed? OnCommandUsedPost;
    public event Action<Admin, PlayerBan> SuccessUnban;
    public event Action<Admin, PlayerComm> SuccessUnComm;

    public void OnFullConnectInvoke(string steamId, string ip)
    {
        OnFullConnect?.Invoke(steamId, ip);
    }

    public bool OnOptionExecutedPost(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu, IDynamicMenuOption option)
    {
        var result = OptionExecutedPost?.Invoke(player, menu, gameMenu, option) ?? HookResult.Continue;
        if (result is HookResult.Stop or HookResult.Handled) {
            return false;
        }
        return true;
    }

    public async Task RefreshAdmins()
    {
        await DBAdmins.RefreshAdmins();
    }
    public async Task RefreshAdminsOnAllServers()
    {
        await Main.AdminApi.SendRconToAllServers("css_am_reload_admins");
    }
    
    public void HookNextPlayerMessage(CCSPlayerController player, Action<string> action)
    {
        AdminUtils.LogDebug("Log next player message: " + player.PlayerName);
        NextPlayerMessage[player] = action;
    }

    public void RemoveNextPlayerMessageHook(CCSPlayerController player)
    {
        AdminUtils.LogDebug("Remove next player message hook: " + player.PlayerName);
        NextPlayerMessage.Remove(player);
    }

    public async Task SendRconToAllServers(string command, bool ignoreSelf = false)
    {
        foreach (var server in AllServers)
        {
            if (ignoreSelf && server.Ip == ThisServer.Ip) continue;
            try
            {
                await SendRconToServer(server, command);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    public async Task<string> SendRconToServer(ServerModel server, string command)
    {
        var ip = server.Ip.Split(":")[0];
        var port = server.Ip.Split(":")[1];
        AdminUtils.LogDebug($"Sending rcon command [{command}] to server ({server.Name})[{server.Ip}] ...");
        using var rcon = new RCON(new IPEndPoint(IPAddress.Parse(ip), int.Parse(port)), server.Rcon ?? "");
        await rcon.ConnectAsync();
        var result = await rcon.SendCommandAsync(command);
        AdminUtils.LogDebug($"Success ✔");
        AdminUtils.LogDebug($"Response from {server.Name} [{server.Ip}]: {result}");
        return result;
    }

    public ServerModel? GetServerById(int id)
    {
        return AllServers.FirstOrDefault(x => x.Id == id);
    }

    public ServerModel? GetServerByIp(string ip)
    {
        return AllServers.FirstOrDefault(x => x.Ip == ip);
    }
    
    public void AddNewCommand(
        string command,
        string description,
        string permission,
        string usage,
        Action<CCSPlayerController?, List<string>, CommandInfo> onExecute,
        CommandUsage whoCanExecute = CommandUsage.CLIENT_AND_SERVER,
        string? tag = null,
        string? notEnoughPermissionsMessage = null,
        int minArgs = 0)
    {
        if (Config.IgnoreCommandsRegistering.Contains(command))
        {
            AdminUtils.LogDebug($"Adding new command [{command}] was skipped from config");
            return;
        }
        if (Config.CommandReplacement.TryGetValue("css_"+command, out var newCommand)) 
        {
            AdminUtils.LogDebug($"Replace {command} to {newCommand}");
            command = newCommand.Remove(0, 4);
        }
        var tagString = tag == null ? Localizer["Tag"] : tag;
        CommandInfo.CommandCallback callback = (p, info) => {
            if (whoCanExecute == CommandUsage.CLIENT_ONLY && p == null)
            {
                info.Reply("It's client only command ✖", tagString);
                return;
            }
            if (whoCanExecute == CommandUsage.SERVER_ONLY && p != null)
            {
                info.Reply(Localizer["Error.OnlyServerCommand"], tagString);
                return;
            }
            var perms = permission.Split(",");
            bool forAll = false;
            foreach (var perm in perms)
            {
                if (AdminUtils.GetCurrentPermissionFlags(perm) == "*")
                    forAll = true;
                if (!p.HasPermissions(perm))
                {
                    info.Reply(notEnoughPermissionsMessage == null ? Localizer["Error.NotEnoughPermissions"] : notEnoughPermissionsMessage, tagString);
                    return;
                }
            }
            if (p != null && p.Admin() != null && p.Admin()!.IsDisabledByWarns && !forAll)
            {
                info.Reply(Localizer["ActionError.DisabledByWarns"]);
                return;
            }
            if (p != null && p.Admin() != null && p.Admin()!.IsDisabledByEnd && !forAll)
            {
                info.Reply(Localizer["ActionError.DisabledByEnd"]);
                return;
            }
            
            var args = AdminUtils.GetArgsFromCommandLine(info.GetCommandString);
            if (args.Count < minArgs)
            {
                info.Reply(Localizer["Error.DifferentNumberOfArgs"].Value.Replace("{usage}", usage), tagString);
                return;
            }
            try
            {
                var onCommandUsedPre = OnCommandUsedPre?.Invoke(p, args, info) ?? HookResult.Continue;
                if (onCommandUsedPre != HookResult.Continue)
                {
                    AdminUtils.LogDebug("Command execute stop by event handler");
                    return;
                }
                onExecute.Invoke(p, args, info);
                OnCommandUsedPost?.Invoke(p, args, info);
            }
            catch (ArgumentException)
            {
                info.Reply(Localizer["Error.ArgumentException"].Value.Replace("{usage}", usage), tagString);
                throw;
            }
            catch (Exception e)
            {
                info.Reply(Localizer["Error.OtherCommandError"].Value.Replace("{usage}", usage), tagString);
                AdminUtils.LogError(e.Message);
            }

            
        };
        var definition = new CommandDefinition("css_" + command, description, callback);
        Plugin.CommandManager.RegisterCommand(definition);
        RegistredCommands[_commandInitializer].Add(new CommandModel { 
            Command = "css_" + command, 
            Definition = definition,
            CommandUsage = whoCanExecute,
            Description = description,
            Pemission = permission,
            Usage = usage,
            Tag = tag
        });
    }

    public void SetCommandInititalizer(string moduleName)
    {
        _commandInitializer = moduleName;
        RegistredCommands.TryAdd(_commandInitializer, new List<CommandModel>());
    }
    public void ClearCommandInitializer()
    {
        _commandInitializer = "unsorted";
    }

    public void EOnModuleLoaded(AdminModule module)
    {
        OnModuleLoaded?.Invoke(module);
    }

    public void EOnModuleUnload(AdminModule module)
    {
        OnModuleUnload?.Invoke(module);
    }

    public async Task<DBResult> AddBan(PlayerBan ban, bool announce = true)
    {
        try
        {
            AdminUtils.LogDebug($"Baning player...");
            var reservedReason = BansConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == ban.Reason.ToLower());
            if (reservedReason != null)
            {
                AdminUtils.LogDebug($"Do reservedReason transformations...");
                if (reservedReason.BanOnAllServers)
                    ban.ServerId = null;
                ban.Reason = reservedReason.Text;
            }
            var admin = ban.Admin!;
            var group = admin.Group;
            AdminUtils.LogDebug("Ban f2 1 ");
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = limitations.FirstOrDefault(x => x.LimitationKey == "max_ban_time")?.LimitationValue;
                var minTime = limitations.FirstOrDefault(x => x.LimitationKey == "min_ban_time")?.LimitationValue;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = limitations.FirstOrDefault(x => x.LimitationKey == "max_bans_in_day")?.LimitationValue;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (ban.Duration > maxTimeInt || minTimeInt > ban.Duration)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.CoreApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return new DBResult(null, 3, "limitations limit reached");
                }
                if (maxByDay != null)
                {
                    var lastPunishments = await DBBans.GetLastAdminBans(admin, 60 * 60 * 24);
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.CoreApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", Utils.GetDateString(lastPunishments[0].CreatedAt + 60 * 60 * 24))
                        );
                        return new DBResult(null, 3, "limitations limit reached");
                    }
                }
            }
            AdminUtils.LogDebug("Ban f2 2 ");
            
            // Проверка на существование бана
            PlayerBan? existingBan = null;
            if (ban.BanType is 1 or 2 && ban.Ip != null)
                existingBan = await GetActiveBanIp(ban.Ip);
            else if (ban.BanType is 0 or 2 && ban.SteamId != null) existingBan = await GetActiveBan(ban.SteamId);
            if (existingBan != null)
                return new DBResult(null, 1, "ban exists");
            // ====
            
            var onBanPre = OnBanPre?.Invoke(ban, ref announce) ?? HookResult.Continue;
            if (onBanPre != HookResult.Continue)
            {
                return new DBResult(null, -2, "stopped by event PRE");
            }
            AdminUtils.LogDebug("Ban f2 3 ");
            var result = await DBBans.Add(ban);
            AdminUtils.LogDebug("Ban f2 4 ");
            switch (result.QueryStatus)
            {
                case 0:
                    Server.NextWorldUpdate(() =>
                    {
                        AdminUtils.LogDebug("Ban f2 5 ");
                        if (announce)
                            MsgAnnounces.BanAdded(ban);
                        CCSPlayerController? player = null;
                        if (ban.BanType == 0)
                        {
                            player = PlayersUtils.GetControllerBySteamIdUnsafe(ban.SteamId!);
                        }
                        else
                            player = PlayersUtils.GetControllerByIp(ban.Ip!);
                        if (player != null)
                        {
                            DisconnectPlayer(player, ban.Reason, customMessageTemplate: Localizer["HTML.AdvancedBanMessage"], admin: admin,
                                disconnectionReason: NetworkDisconnectionReason.NETWORK_DISCONNECT_STEAM_BANNED, disconnectedBy: "ban");
                        }
                    });
                    break;
                case 1:
                    AdminUtils.LogDebug("Ban already exists!");
                    break;
                case -1:
                    AdminUtils.LogDebug("Some error while ban");
                    break;
            }
            var onBanPost = OnBanPost?.Invoke(ban, ref announce) ?? HookResult.Continue;
            if (onBanPost != HookResult.Continue)
            {
                return new DBResult(null, -2, "stopped by event POST");
            }
            return result;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
        
    }

    public async Task<DBResult> Unban(Admin admin, string steamId, string? reason, bool announce = true)
    {
        var ban = await GetActiveBan(steamId);
        if (ban == null)
        {
            AdminUtils.LogDebug("Ban not finded ✖!");
            return new DBResult(null, 1, "Ban not finded ✖!");
        }
        if (!DBBans.CanUnban(admin, ban)) return new DBResult(null, 2, "admin can't unban");
        
        var onUnBanPre = OnUnBanPre?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnBanPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "stopped by event PRE");
        }
        ban.UpdatedAt = AdminUtils.CurrentTimestamp();
        var result = await DBBans.Unban(admin, ban, reason);
        switch (result.QueryStatus)
        {
            case 0:
                ban.UnbannedBy = admin.Id;
                ban.UnbanReason = reason;
                SuccessUnban?.Invoke(admin, ban);
                Server.NextWorldUpdate(() => {
                    if (announce)
                        MsgAnnounces.Unbanned(ban);
                });
                break;
            case -1:
                AdminUtils.LogDebug("Some error while unban");
                break;
        }
        
        var onUnBanPost = OnUnBanPost?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnBanPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "stopped by event POST");
        }
        
        return result;
    }

    public async Task<DBResult> UnbanIp(Admin admin, string ip, string? reason, bool announce = true)
    {
        var ban = await GetActiveBanIp(ip);
        if (ban == null)
        {
            AdminUtils.LogDebug("Ban not finded ✖!");
            return new DBResult(null, 1, "Ban not finded ✖!");
        }
        
        if (!DBBans.CanUnban(admin, ban)) return new DBResult(null, 2, "admin can't unban");
        
        var onUnBanIpPre = OnUnBanPre?.Invoke(admin, ref ip, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnBanIpPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "stopped by event PRE");
        }
        
        var result = await DBBans.Unban(admin, ban, reason);
        switch (result.QueryStatus)
        {
            case 0:
                ban.UnbannedBy = admin.Id;
                ban.UnbanReason = reason;
                SuccessUnban?.Invoke(admin, ban);
                Server.NextWorldUpdate(() => {
                    if (announce)
                        MsgAnnounces.Unbanned(ban);
                });
                break;
            case -1:
                AdminUtils.LogDebug("Some error while unban");
                break;
        }
        
        var onUnBanIpPost = OnUnBanPost?.Invoke(admin, ref ip, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnBanIpPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "stopped by event POST");
        }

        return result;
    }

    public async Task<PlayerBan?> GetActiveBan(string steamId)
    {
        var ban = await DBBans.GetActiveBan(steamId);
        return ban;
    }

    public async Task<List<PlayerBan>> GetAllBans(string steamId)
    {
        var ban = await DBBans.GetAllBans(steamId);
        return ban;
    }

    public async Task<PlayerBan?> GetActiveBanIp(string ip)
    {
        var ban = await DBBans.GetActiveBanIp(ip);
        return ban;
    }

    public async Task<List<PlayerBan>> GetAllIpBans(string ip)
    {
        var ban = await DBBans.GetAllIpBans(ip);
        return ban;
    }

    public async Task<List<PlayerBan>> GetLastBans(int time)
    {
        return await DBBans.GetLastBans(time);
    }

    public bool CanDoActionWithPlayer(string callerId, string targetId)
    {
        var callerAdmin = AdminUtils.ServerAdmin(callerId);
        if (callerId.ToLower() == "console") return true;
        var targetAdmin = AdminUtils.ServerAdmin(targetId);

        if (targetAdmin == null || targetAdmin.IsDisabled) return true;

        if (targetAdmin != null)
        {
            if (callerAdmin == null) return false;
            if (callerAdmin.HasPermissions("other.equals_immunity_action"))
            {
                if (callerAdmin.CurrentImmunity >= targetAdmin.CurrentImmunity) return true;
            } else {
                if (callerAdmin.CurrentImmunity > targetAdmin.CurrentImmunity) return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// This method depricated, use <see cref="DisconnectPlayer(CounterStrikeSharp.API.Core.CCSPlayerController,string,bool,string?,IksAdminApi.Admin?,string?,CounterStrikeSharp.API.ValveConstants.Protobuf.NetworkDisconnectionReason?, string)"/> instead
    /// </summary>
    [Obsolete]
    public void DisconnectPlayer(
        CCSPlayerController player, 
        string reason, bool instantly = false, 
        string? customMessageTemplate = null, 
        Admin? admin = null, 
        string? customByAdminTemplate = null,
        NetworkDisconnectionReason? disconnectionReason = null)
    {
        var messageTemplate = customMessageTemplate ?? Localizer["HTML.AdvancedKickMessage"];
        bool advanced = Config.AdvancedKick;
        disconnectionReason = disconnectionReason ?? NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED;
        if (!advanced || instantly) 
        {
            player.Disconnect((NetworkDisconnectionReason)disconnectionReason);
            return;
        }
        Main.BlockTeamChange.Add(player);
        var byAdminMessageTemplate = customByAdminTemplate ?? Localizer["HTML.ByAdminTemplate"];
        var byAdminMessage = admin != null ? byAdminMessageTemplate.Replace("{admin}", admin.Name) : "";
        for (int i = 0; i < Config.AdvancedKickTime; i++)
        {
            var sec = i;
            Plugin.AddTimer(sec, () =>
            {
                if (player == null! || !player.IsValid) return;
                player.HtmlMessage(messageTemplate
                        .Replace("{reason}", reason)
                        .Replace("{time}", (Config.AdvancedKickTime - sec).ToString())
                        .Replace("{byAdmin}", byAdminMessage)
                    , Config.AdvancedKickTime);
            });
        }
        Plugin.AddTimer(Config.AdvancedKickTime, () => {
            if (player != null!)
            {
                player.ClearHtmlMessage();

                player.Disconnect((NetworkDisconnectionReason)disconnectionReason);
            }
        });
    }
    
    public void DisconnectPlayer(
        CCSPlayerController player, 
        string reason, 
        bool instantly = false, 
        string? customMessageTemplate = null, 
        Admin? admin = null, 
        string? customByAdminTemplate = null,
        NetworkDisconnectionReason? disconnectionReason = null,
        string disconnectedBy = "plugin"
        )

    {

        var edata = new EventData("disconnect_player_pre");
        edata.Insert("player", player);
        edata.Insert("reason", reason);
        edata.Insert("instantly", instantly);
        edata.Insert("custom_message_template", customMessageTemplate);
        edata.Insert("admin", admin);
        edata.Insert("custom_by_admin_template", customByAdminTemplate);
        edata.Insert("disconnection_reason", disconnectionReason);
        edata.Insert("disconnected_by", disconnectedBy);

        if (edata.Invoke() != HookResult.Continue)
        {
            AdminUtils.LogDebug("DisconnectPlayer(): Stopped by PRE event");
            return;
        }
        
        player = edata.Get<CCSPlayerController>("player");
        reason = edata.Get<string>("reason");
        instantly = edata.Get<bool>("instantly");
        customMessageTemplate = edata.Get<string?>("custom_message_template");
        admin = edata.Get<Admin?>("admin");
        customByAdminTemplate = edata.Get<string?>("custom_by_admin_template");
        disconnectionReason = edata.Get<NetworkDisconnectionReason?>("disconnection_reason");
        disconnectedBy = edata.Get<string>("disconnected_by");
        
        var messageTemplate = customMessageTemplate ?? Localizer["HTML.AdvancedKickMessage"];
        bool advanced = Config.AdvancedKick;
        disconnectionReason = disconnectionReason ?? NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED;
        if (!advanced || instantly) 
        {
            player.Disconnect((NetworkDisconnectionReason)disconnectionReason);
            return;
        }
        Main.BlockTeamChange.Add(player);
        var byAdminMessageTemplate = customByAdminTemplate ?? Localizer["HTML.ByAdminTemplate"];
        var byAdminMessage = admin != null ? byAdminMessageTemplate.Replace("{admin}", admin.Name) : "";
        for (int i = 0; i < Config.AdvancedKickTime; i++)
        {
            var sec = i;
            Plugin.AddTimer(sec, () =>
            {
                if (player == null! || !player.IsValid) return;
                player.HtmlMessage(messageTemplate
                        .Replace("{reason}", reason)
                        .Replace("{time}", (Config.AdvancedKickTime - sec).ToString())
                        .Replace("{byAdmin}", byAdminMessage)
                    , Config.AdvancedKickTime);
            });
        }
        Plugin.AddTimer(Config.AdvancedKickTime, () => {
            if (player != null!)
            {
                player.ClearHtmlMessage();

                player.Disconnect((NetworkDisconnectionReason)disconnectionReason);

                edata.Invoke("disconnect_player_post");
            }
        });
    }

    public void DoActionWithIdentity(CCSPlayerController? actioneer, string identity, Action<CCSPlayerController?, IdentityType> action, string[]? blockedArgs = null, bool acceptNullSteamIdPlayer = false)
    {
        if (blockedArgs != null && blockedArgs.Contains(identity))
        {
            actioneer.Print("This identity is blocked for this action!");
            return;
        }
        if (identity == "@me" && actioneer == null)
        {
            actioneer.Print("This identity is blocked for NULL PLAYERS!");
            return;
        }
        List<CCSPlayerController> targets = new();
        var identityType = IdentityType.Name;
        switch (identity)
        {
            case "@all":
                targets = Utilities.GetPlayers().Where(x => x.IsValid).ToList();
                identityType = IdentityType.All;
                break;
            case "@me":
                targets = new List<CCSPlayerController>() { actioneer! };
                identityType = IdentityType.Me;
                break;
            case "@ct":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && x.TeamNum == 3).ToList();
                identityType = IdentityType.Ct;
                break;
            case "@t":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && x.TeamNum == 2).ToList();
                identityType = IdentityType.T;
                break;
            case "@spec":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && x.TeamNum == 1).ToList();
                identityType = IdentityType.Spec;
                break;
            case "@bots":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && x.IsBot).ToList();
                identityType = IdentityType.Bots;
                break;
            case "@players":
                targets = Utilities.GetPlayers().Where(x => x.IsValid && !x.IsBot).ToList();
                identityType = IdentityType.Players;
                break;
        }
        if (targets.Count > 0)
        {
            foreach (var target1 in targets)
            {
                action.Invoke(target1, identityType);
            }
            return;
        }
        if (identity.StartsWith("#"))
        {
            var target = PlayersUtils.GetControllerBySteamId(identity.Remove(0, 1));
            if ((target != null) || (identity.Length == 18 && acceptNullSteamIdPlayer))
            {
                identityType = IdentityType.SteamId;
                action.Invoke(target, identityType);
                return;
            }
            else {
                if (uint.TryParse(identity.Remove(0, 1), out uint uid))
                    target = PlayersUtils.GetControllerByUid(uid);
                if (target != null)
                {
                    identityType = IdentityType.UserId;
                    action.Invoke(target, identityType);
                    return;
                }
            }
            return;
        }
        var targetName = PlayersUtils.GetControllerByName(identity);
        if (targetName != null)
        {
            action.Invoke(targetName, identityType);
            return;
        }
        
        Helper.Print(actioneer, Localizer["ActionError.TargetNotFound"]);
    }
    /// <summary>
    /// Нужен SteamWebApiKey установленный в кфг
    /// </summary>
    public async Task<PlayerSummaries?> GetPlayerSummaries(ulong steamId)
    {
        if (Main.AdminApi.Config.WebApiKey == "") return null;
        var webInterfaceFactory = new SteamWebInterfaceFactory(Main.AdminApi.Config.WebApiKey);
        var steamInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(new HttpClient());
        var playerSummaryResponse = await steamInterface.GetPlayerSummaryAsync(steamId);
        var data = playerSummaryResponse.Data;
        var summaries = new PlayerSummaries(
            data.SteamId,
            data.Nickname,
            data.ProfileUrl,
            data.AvatarUrl,
            data.AvatarFullUrl,
            data.AvatarUrl
        );
        return summaries;
    }
    /// <summary>
    /// Перезагрузка/проверка и выдача/снятие наказаний игрока
    /// </summary>
    public async Task ReloadInfractions(string steamId, string? ip = null, bool instantlyKick = false)
    {
        // Проверяем наличие бана и кикаем если есть =)
        AdminUtils.LogDebug("Reload infractions for: " + steamId);
        var ban = await GetActiveBan(steamId);
        AdminUtils.LogDebug("Has ban: " + (ban != null).ToString());
        if (ban != null)
        {
            Server.NextWorldUpdate(() =>
            {
                var player = PlayersUtils.GetControllerBySteamId(steamId);
                if (player == null)
                {
                    Main.KickOnFullConnect.Add(steamId, instantlyKick);
                    Main.KickOnFullConnectReason.Add(steamId, ban.Reason);
                    return;
                }
                
                DisconnectPlayer(
                    player,
                    ban.Reason,
                    instantlyKick,
                    customMessageTemplate: Localizer["HTML.AdvancedBanMessage"], disconnectionReason: NetworkDisconnectionReason.NETWORK_DISCONNECT_STEAM_BANNED,
                    admin: ban.Admin, 
                    disconnectedBy: "ban"
                );
            });
            return;
        }
        if (ip != null && !Config.MirrorsIp.Contains(ip))
        {
            
            AdminUtils.LogDebug("Check ban for: " + ip);
            ban = await GetActiveBanIp(ip);
            AdminUtils.LogDebug("Has ip ban: " + (ban != null).ToString());
            if (ban != null)
            {
                Server.NextWorldUpdate(() =>
                {
                    var player = PlayersUtils.GetControllerBySteamId(steamId);
                    if (player == null)
                    {
                        Main.KickOnFullConnect.Add(steamId, instantlyKick);
                        Main.KickOnFullConnectReason.Add(steamId, ban.Reason);
                        return;
                    }
                
                    DisconnectPlayer(
                        player,
                        ban.Reason,
                        instantlyKick,
                        customMessageTemplate: Localizer["HTML.AdvancedBanMessage"], disconnectionReason: NetworkDisconnectionReason.NETWORK_DISCONNECT_STEAM_BANNED,
                        admin: ban.Admin,
                        disconnectedBy: "ban"
                    );
                });
                return;
            }
        }
        var comms = await GetActiveComms(steamId);
        Server.NextWorldUpdate(() => {
            foreach (var comm in comms)
            {
                ApplyCommForPlayer(comm);
            }
        });
        AdminUtils.LogDebug("Has gag: " + comms.HasGag());
        AdminUtils.LogDebug("Has mute: " + comms.HasMute());
        AdminUtils.LogDebug("Has silence: " + comms.HasSilence());
        
        if (!ServerAdmins.TryGetValue(ulong.Parse(steamId), out var admin))
        {
            AdminUtils.LogDebug("PLAYER NOT ADMIN \u2716");
            return;
        }

        AdminUtils.LogDebug("Reload warns...");
        AdminUtils.LogDebug("Admin id: " + admin.Id);
        var warns = await GetAllWarnsForAdmin(admin);
        foreach (var warn in warns.ToList())
        {
            var exWarn = Warns.FirstOrDefault(x => x.Id == warn.Id);
            if (exWarn != null)
            {
                exWarn = warn;
            }
            else
            {
                Warns.Add(warn);
            }
        }
        var warnsForDelete = Warns.Where(x => warns.All(warn => warn.Id != x.Id)).ToList();
        foreach (var warn in warnsForDelete)
        {
            AdminUtils.LogDebug("Delete invalid warn: " + warn.Id);
            Warns.Remove(warn);
        }
        AdminUtils.LogDebug("Warns count: " + admin.Warns.Count);
    }

    public void MutePlayerInGame(PlayerComm mute)
    {
        var players = Utilities.GetPlayers();
        foreach (var p in players)
        {
            if (p.AuthorizedSteamID == null) continue;
            AdminUtils.LogDebug(p.PlayerName);
            AdminUtils.LogDebug(p.AuthorizedSteamID.SteamId64.ToString());
        }
        var player = PlayersUtils.GetControllerBySteamIdUnsafe(mute.SteamId);
        if (player != null)
        {
            AdminUtils.LogDebug($"Mute player: {mute.Name} | {mute.SteamId} in game!");
            Comms.Add(mute);
            player.VoiceFlags = VoiceFlags.Muted;
        } else {
            // Main.InstantComm.Add(mute.SteamId, mute); // Нужна была как заглушка, потому что раньше я не мог получить контроллер игрока сразу после авторизации
        }
    }
    public void UnmutePlayerInGame(PlayerComm mute)
    {
        var player = PlayersUtils.GetControllerBySteamId(mute.SteamId);
        if (player != null)
        {
            Helper.Print(player, Localizer["Message.WhenMuteEnd"]);
            player.VoiceFlags = VoiceFlags.Normal;
        }
        var exComm = Comms.GetMute();
        Main.InstantComm.Remove(mute.SteamId);
        Comms.Remove(exComm!);
    }
    
    public void GagPlayerInGame(PlayerComm gag)
    {
        var player = PlayersUtils.GetControllerBySteamIdUnsafe(gag.SteamId);
        AdminUtils.LogDebug($"Gag player: {gag.Name} | {gag.SteamId} in game!");
        Comms.Add(gag);
    }
    public void UnGagPlayerInGame(PlayerComm gag)
    {
        var player = PlayersUtils.GetControllerBySteamId(gag.SteamId);
        if (player != null)
        {
            Helper.Print(player, Localizer["Message.WhenGagEnd"]);
        }
        var exGag = Comms.GetGag();
        Comms.Remove(exGag!);
    }
    private void UnSilencePlayerInGame(PlayerComm comm)
    {
        var player = PlayersUtils.GetControllerBySteamId(comm.SteamId);
        if (player != null)
        {
            Helper.Print(player, Localizer["Message.WhenSilenceEnd"]);
            player.VoiceFlags = VoiceFlags.Normal;
        }
        var exComm = Comms.GetSilence();
        Comms.Remove(exComm!);
    }


    public async Task<DBResult> UnComm(Admin admin, PlayerComm comm, bool announce = true)
    {
        DBResult result = new DBResult(null, -1, "ERROR!");
        comm.UpdatedAt = AdminUtils.CurrentTimestamp();
        switch (comm.MuteType)
        {
            case 0:
                result = await UnMute(admin, comm.SteamId, comm.UnbanReason, announce);
                break;
            case 1:
                result = await UnGag(admin, comm.SteamId, comm.UnbanReason, announce);
                break;
            case 2:
                result = await UnSilence(admin, comm.SteamId, comm.UnbanReason, announce);
                break;
        }

        return result;
    }

    private async Task<DBResult> UnSilence(Admin admin, string steamId, string? reason, bool announce)
    {
        AdminUtils.LogDebug($"Ungag player {steamId}!");
        var comms = await GetActiveComms(steamId);
        if (!comms.HasSilence())
        {
            AdminUtils.LogDebug("Silence not finded ✖!");
            return new DBResult(0, 1, "Silence not finded ✖!");
        }
        
        var onUnCommPre = OnUnCommPre?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        
        var silence = comms.GetSilence()!;
        var result = await DBComms.UnComm(admin, silence, reason);
        switch (result.QueryStatus)
        {
            case 0:
                silence.UnbannedBy = admin.Id;
                silence.UnbanReason = reason;
                SuccessUnComm?.Invoke(admin, silence);
                Server.NextWorldUpdate(() => {
                    UnSilencePlayerInGame(silence);
                    if (announce)
                        MsgAnnounces.UnSilenced(silence);
                });
                break;
            case 2:
                AdminUtils.LogDebug("Not enough permissions for unSilence this player");
                break;
            case -1:
                AdminUtils.LogDebug("Some error while unSilence");
                break;
        }
        var onUnCommPost = OnUnCommPost?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        return result;
    }

    

    public async Task<List<PlayerComm>> GetActiveComms(string steamId)
    {
        return await DBComms.GetActiveComms(steamId);
    }

    public async Task<List<PlayerComm>> GetAllComms(string steamId)
    {
        return await DBComms.GetAllComms(steamId);
    }

    public async Task<List<PlayerComm>> GetLastComms(int time)
    {
        return await DBComms.GetLastComms(time);
    }

    public async Task<DBResult> AddComm(PlayerComm comm, bool announce = true)
    {
        DBResult result = new DBResult(-1, -1, "ERROR!");
        try
        {
            switch (comm.MuteType)
            {
                case 0:
                    result = await AddMute(comm, announce);
                    break;
                case 1:
                    result = await AddGag(comm, announce);
                    break;
                case 2:
                    result = await AddSilence(comm, announce);
                    break;
            }
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
        return result;
    }

    private async Task<DBResult> AddSilence(PlayerComm comm, bool announce)
    {
        try
        {
            comm.MuteType = 2;
            AdminUtils.LogDebug($"Silence player {comm.SteamId}!");
            var reservedReason = SilenceConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == comm.Reason.ToLower());
            if (reservedReason != null)
            {
                AdminUtils.LogDebug("Do reservedReason transformations...");
                if (reservedReason.BanOnAllServers)
                    comm.ServerId = null;
                comm.Reason = reservedReason.Text;
            }

            var admin = comm.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = limitations.FirstOrDefault(x => x.LimitationKey == "max_silence_time")?.LimitationValue;
                var minTime = limitations.FirstOrDefault(x => x.LimitationKey == "min_silence_time")?.LimitationValue;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = limitations.FirstOrDefault(x => x.LimitationKey == "max_silences_in_day")?.LimitationValue;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (comm.Duration/60 > maxTimeInt || minTimeInt > comm.Duration/60)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.CoreApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return new DBResult(null, 3, "limitations limit reached");;
                }
                if (maxByDay != null)
                {
                    var lastPunishments = (await DBComms.GetLastAdminComms(admin, 60 * 60 * 24)).Where(x => x.MuteType == 1).ToList();
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.CoreApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", Utils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return new DBResult(null, 3, "limitations limit reached");;
                    }
                }
            }
            
            var existingComm = await GetActiveComms(comm.SteamId);
            if (existingComm != null && existingComm.Any(x => x.MuteType == comm.MuteType || x.MuteType == 2))
                return new DBResult(null, 1, "Already banned");
            
            var onCommPre = OnCommPre?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPre != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event PRE");
            }

            var result = await DBComms.Add(comm);
            switch (result.QueryStatus)
            {
                case 0:
                    comm.Id = result.ElementId ?? 0;
                    Server.NextWorldUpdate(() => {
                        if (announce)
                            MsgAnnounces.SilenceAdded(comm);
                        Server.NextWorldUpdate(() => {
                            SilencePlayerInGame(comm);
                        });
                    });
                    break;
                case 1:
                    AdminUtils.LogDebug("Silence already exists!");
                    break;
                case -1:
                    AdminUtils.LogDebug("Some error while silence");
                    break;
            }
            
            var onCommPost = OnCommPost?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPost != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event POST");
            }
            
            return result;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public async Task<DBResult> AddGag(PlayerComm comm, bool announce = true)
    {
        try
        {
            comm.MuteType = 1;
            AdminUtils.LogDebug($"Gaging player {comm.SteamId}!");
            var reservedReason = GagsConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == comm.Reason.ToLower());
            if (reservedReason != null)
            {
                AdminUtils.LogDebug($"Do reservedReason transformations...");
                if (reservedReason.BanOnAllServers)
                    comm.ServerId = null;
                comm.Reason = reservedReason.Text;
            }

            var admin = comm.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = limitations.FirstOrDefault(x => x.LimitationKey == "max_gag_time")?.LimitationValue;
                var minTime = limitations.FirstOrDefault(x => x.LimitationKey == "min_gag_time")?.LimitationValue;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = limitations.FirstOrDefault(x => x.LimitationKey == "max_gags_in_day")?.LimitationValue;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (comm.Duration/60 > maxTimeInt || minTimeInt > comm.Duration/60)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.CoreApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return new DBResult(null, 3, "limitations limit reached");
                }
                if (maxByDay != null)
                {
                    var lastPunishments = (await DBComms.GetLastAdminComms(admin, 60 * 60 * 24)).Where(x => x.MuteType == 1).ToList();
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.CoreApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", Utils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return new DBResult(null, 3, "limitations limit reached");
                    }
                }
            }
            
            var existingComm = await GetActiveComms(comm.SteamId);
            if (existingComm != null && existingComm.Any(x => x.MuteType == comm.MuteType || x.MuteType == 2))
                return new DBResult(null, 1, "Already banned");
            
            var onCommPre = OnCommPre?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPre != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event PRE");
            }
            var result = await DBComms.Add(comm);
            switch (result.QueryStatus)
            {
                case 0:
                    comm.Id = result.ElementId ?? 0;
                    Server.NextWorldUpdate(() => {
                        if (announce)
                            MsgAnnounces.GagAdded(comm);
                        Server.NextWorldUpdate(() => {
                            GagPlayerInGame(comm);
                        });
                    });
                    break;
                case 1:
                    AdminUtils.LogDebug("Gag already exists!");
                    break;
                case -1:
                    AdminUtils.LogDebug("Some error while gag");
                    break;
            }
            
            var onCommPost = OnCommPost?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPost != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event POST");
            }
            
            return result;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public async Task<DBResult> UnGag(Admin admin, string steamId, string? reason, bool announce = true)
    {
        AdminUtils.LogDebug($"Ungag player {steamId}!");
        var comms = await GetActiveComms(steamId);
        if (!comms.HasGag())
        {
            AdminUtils.LogDebug("Gag not finded ✖!");
            return new DBResult(0, 1, "Gag not finded ✖!");
        }
        
        var onUnCommPre = OnUnCommPre?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }

        var gag = comms.GetGag()!;
        var result = await DBComms.UnComm(admin, comms.GetGag()!, reason);
        switch (result.QueryStatus)
        {
            case 0:
                gag.UnbannedBy = admin.Id;
                gag.UnbanReason = reason;
                SuccessUnComm?.Invoke(admin, gag);
                Server.NextWorldUpdate(() => {
                    UnGagPlayerInGame(gag);
                    if (announce)
                        MsgAnnounces.UnGagged(gag);
                });
                break;
            case 2:
                AdminUtils.LogDebug("Not enough permissions for ungag this player");
                break;
            case -1:
                AdminUtils.LogDebug("Some error while ungag");
                break;
        }
        
        var onUnCommPost = OnUnCommPost?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        
        return result;
    }

    public async Task<DBResult> AddMute(PlayerComm comm, bool announce = true)
    {
        try
        {
            comm.MuteType = 0;
            AdminUtils.LogDebug($"Muting player {comm.SteamId}!");
            var reservedReason = MutesConfig.Config.Reasons.FirstOrDefault(x => x.Title.ToLower() == comm.Reason.ToLower());
            if (reservedReason != null)
            {
                AdminUtils.LogDebug($"Do reservedReason transformations..." );
                if (reservedReason.BanOnAllServers)
                    comm.ServerId = null;
                comm.Reason = reservedReason.Text;
            }

            var admin = comm.Admin!;
            var group = admin.Group;
            if (group != null)
            {
                var limitations = group.Limitations;
                var maxTime = limitations.FirstOrDefault(x => x.LimitationKey == "max_mute_time")?.LimitationValue;
                var minTime = limitations.FirstOrDefault(x => x.LimitationKey == "min_mute_time")?.LimitationValue;
                var minTimeInt = minTime == null ? 0 : int.Parse(minTime);
                var maxTimeInt = maxTime == null ? int.MaxValue : int.Parse(maxTime);
                var maxByDay = limitations.FirstOrDefault(x => x.LimitationKey == "max_mutes_in_day")?.LimitationValue;
                var maxByDayInt = maxByDay == null ? int.MaxValue : int.Parse(maxByDay);
                if (comm.Duration/60 > maxTimeInt || minTimeInt > comm.Duration/60)
                {
                    Helper.PrintToSteamId(admin.SteamId, AdminUtils.CoreApi.Localizer["Limitations.TimeLimit"].Value
                        .Replace("{min}", minTimeInt.ToString())
                        .Replace("{max}", maxTimeInt.ToString())
                    );
                    return new DBResult(null, 3, "limitations limit reached");
                }
                if (maxByDay != null)
                {
                    var lastPunishments = (await DBComms.GetLastAdminComms(admin, 60 * 60 * 24)).Where(x => x.MuteType == 0).ToList();
                    if (lastPunishments.Count > maxByDayInt)
                    {
                        Helper.PrintToSteamId(admin.SteamId, AdminUtils.CoreApi.Localizer["Limitations.MaxByDayLimit"].Value
                            .Replace("{date}", Utils.GetDateString(lastPunishments[0].CreatedAt + 60*60*24))
                        );
                        return new DBResult(null, 3, "limitations limit reached");
                    }
                }
            }
            
            var existingComm = await GetActiveComms(comm.SteamId);
            if (existingComm != null && existingComm.Any(x => x.MuteType == comm.MuteType || x.MuteType == 2))
                return new DBResult(null, 1, "Already banned");
            
            var onCommPre = OnCommPre?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPre != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event PRE");
            }
            
            var result = await DBComms.Add(comm);
            switch (result.QueryStatus)
            {
                case 0:
                    comm.Id = result.ElementId ?? 0;
                    Server.NextWorldUpdate(() => {
                        if (announce)
                            MsgAnnounces.MuteAdded(comm);
                        Server.NextWorldUpdate(() => {
                            MutePlayerInGame(comm);
                        });
                    });
                    break;
                case 1:
                    AdminUtils.LogDebug("Mute already exists!");
                    break;
                case -1:
                    AdminUtils.LogDebug("Some error while mute");
                    break;
            }
            
            var onCommPost = OnCommPost?.Invoke(comm, ref announce) ?? HookResult.Continue;
            if (onCommPost != HookResult.Continue)
            {
                return new DBResult(null, -2, "Stopped by event POST");
            }
            
            return result;
        }
        catch (Exception e)
        {
            AdminUtils.LogError(e.ToString());
            throw;
        }
    }

    public async Task<DBResult> UnMute(Admin admin, string steamId, string? reason, bool announce = true)
    {
        AdminUtils.LogDebug($"Unmute player {steamId}!");
        var comms = await DBComms.GetActiveComms(steamId);
        if (!comms.HasMute())
        {
            AdminUtils.LogDebug("Mute not finded ✖!");
            return new DBResult(0, 1, "Mute not finded ✖!");
        }
        
        var onUnCommPre = OnUnCommPre?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPre != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        
        var mute = comms.GetMute()!;
        var result = await DBComms.UnComm(admin, mute, reason);
        switch (result.QueryStatus)
        {
            case 0:
                mute.UnbannedBy = admin.Id;
                mute.UnbanReason = reason;
                SuccessUnComm?.Invoke(admin, mute);
                Server.NextWorldUpdate(() => {
                    UnmutePlayerInGame(mute);
                    if (announce)
                        MsgAnnounces.UnMuted(mute);
                });
                break;
            case 2:
                AdminUtils.LogDebug("Not enough permissions for unmute this player");
                break;
            case -1:
                AdminUtils.LogDebug("Some error while unmute");
                break;
        }
        
        var onUnCommPost = OnUnCommPost?.Invoke(admin, ref steamId, ref reason, ref announce) ?? HookResult.Continue;
        if (onUnCommPost != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event PRE");
        }
        
        return result;
    }


    public void Hide(Admin admin)
    {
        var caller = admin.Controller;

        if (caller == null)
            return;
        
        if (HidenAdmins.Contains(admin))
        {
            HidenAdmins.Remove(admin);
            caller.Print(Localizer["Message.Hide_off"]);
            caller!.ChangeTeam(CsTeam.Spectator);
            return;
        }
        HidenAdmins.Remove(admin);
        HidenAdmins.Add(admin);
        CmdBase.FirstMessage.Add(caller);
        Server.ExecuteCommand("sv_disable_teamselect_menu 1");
        if (caller.PlayerPawn.Value != null && caller.PawnIsAlive)
            caller.PlayerPawn.Value.CommitSuicide(true, false);
        Plugin.AddTimer(1.0f, () => { Server.NextWorldUpdate(() => caller.ChangeTeam(CsTeam.Spectator)); HidenAdmins.Add(admin); CmdBase.FirstMessage.Add(caller); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        Plugin.AddTimer(1.4f, () => { Server.NextWorldUpdate(() => caller.ChangeTeam(CsTeam.None)); caller.Print(Localizer["Message.Hide_on"]); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        Plugin.AddTimer(2.0f, () => { Server.NextWorldUpdate(() => Server.ExecuteCommand("sv_disable_teamselect_menu 0")); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
    }

    public async Task<DBResult> CreateGroup(Group group)
    {
        var result = await DBGroups.AddGroup(group);
        await ReloadDataFromDb();
        return result;
    }

    public async Task<DBResult> UpdateGroup(Group group)
    {
        var result = await DBGroups.UpdateGroupInBase(group);
        await ReloadDataFromDb();
        return result;
    }

    public async Task<DBResult> DeleteGroup(Group group)
    {
        var result = await DBGroups.DeleteGroup(group);
        await ReloadDataFromDb();
        return result;
    }

    public async Task<List<Group>> GetAllGroups()
    {
        return await DBGroups.GetAllGroups();
    }

    public async Task<DBResult> CreateWarn(Warn warn, bool announce = true)
    {
        var eData = new EventData("create_warn_pre");
        eData.Insert("warn", warn);
        eData.Insert("announce", announce);
        if (eData.Invoke() != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event WARN");
        }
        warn = eData.Get<Warn>("warn");
        var result = await warn.InsertToBase();
        if (result.ElementId != null) 
            Warns.Add(warn);
        await ReloadDataFromDb();
        if (eData.Invoke("create_warn_post") != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event WARN");
        }
        warn = eData.Get<Warn>("warn");
        announce = eData.Get<bool>("announce");
        Server.NextWorldUpdate(() =>
        {
            if (announce)
                MsgAnnounces.Warn(warn);
        });
        return result;
    }

    public async Task<DBResult> UpdateWarn(Warn warn)
    {
        var eData = new EventData("update_warn");
        eData.Insert("warn", warn);
        if (eData.Invoke() != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event WARN");
        }
        warn = eData.Get<Warn>("warn");
        var exWarn = Warns.FirstOrDefault(x => x.Id == warn.Id);
        if (exWarn != null)
            exWarn = warn;
        var result = await warn.UpdateInBase();
        await ReloadDataFromDb();
        return result;
    }

    public async Task<DBResult> DeleteWarn(Admin admin, Warn warn, bool announce = true)
    {
        warn.DeletedBy = admin.Id;
        warn.DeletedAt = AdminUtils.CurrentTimestamp();
        var eData = new EventData("delete_warn");
        eData.Insert("warn", warn);
        if (eData.Invoke() != HookResult.Continue)
        {
            return new DBResult(null, -2, "Stopped by event WARN");
        }
        warn = eData.Get<Warn>("warn");
        var result = await warn.UpdateInBase();
        await ReloadDataFromDb();
        Server.NextWorldUpdate(() =>
        {
            if (announce)
                MsgAnnounces.WarnDelete(warn);
        });
        return result;
    }

    public async Task<List<Warn>> GetAllWarns()
    {
        return await DBWarns.GetAll();
    }

    public async Task<List<Warn>> GetAllWarnsByAdmin(Admin admin)
    {
        return await DBWarns.GetAllActiveByAdmin(admin.Id);
    }
    public async Task<List<Warn>> GetAllWarnsForAdmin(Admin admin)
    {
        return await DBWarns.GetAllActiveForAdmin(admin.Id);
    }

    public void Slay(Admin admin, CCSPlayerController player, bool announce = true)
    {
        AdminUtils.LogDebug($"Slaying player {player.PlayerName}...");
        var eventData = new EventData("slay_player_pre");
        eventData.Insert("admin", admin);
        eventData.Insert("player", player);
        eventData.Insert("announce", announce);
        if (eventData.Invoke() != HookResult.Continue)
        {
            AdminUtils.LogDebug("Stopped by event PRE");
            return;
        }
        admin = eventData.Get<Admin>("admin");
        player = eventData.Get<CCSPlayerController>("player");
        announce = eventData.Get<bool>("announce");

        player.CommitSuicide(false, false);
        if (announce)
            MsgAnnounces.Slay(admin, player);

        eventData.Invoke("slay_player_post");
    }
    public void Kick(Admin admin, CCSPlayerController player, string reason, bool announce = true)
    {
        AdminUtils.LogDebug($"Kicking player {player.PlayerName}...");
        var eventData = new EventData("kick_player_pre");
        eventData.Insert("admin", admin);
        eventData.Insert("player", player);
        eventData.Insert("reason", reason);
        eventData.Insert("announce", announce);

        if (eventData.Invoke() != HookResult.Continue)
        {
            AdminUtils.LogDebug("Stopped by event PRE");
            return;
        }
        admin = eventData.Get<Admin>("admin");
        player = eventData.Get<CCSPlayerController>("player");
        reason = eventData.Get<string>("reason");
        announce = eventData.Get<bool>("announce");

        DisconnectPlayer(player, reason, admin: admin, disconnectedBy: "kick");
        if (announce)
            MsgAnnounces.Kick(admin, player, reason);

        eventData.Invoke("kick_player_post");
    }

    public void Respawn(Admin admin, CCSPlayerController player, bool announce = true)
    {
        AdminUtils.LogDebug($"Respawning player {player.PlayerName}...");
        var eventData = new EventData("respawn_player_pre");
        eventData.Insert("admin", admin);
        eventData.Insert("player", player);
        eventData.Insert("announce", announce);
        if (eventData.Invoke() != HookResult.Continue)
        {
            AdminUtils.LogDebug("Stopped by event PRE");
            return;
        }
        admin = eventData.Get<Admin>("admin");
        player = eventData.Get<CCSPlayerController>("player");
        announce = eventData.Get<bool>("announce");

        player.Respawn();
        if (announce)
            MsgAnnounces.Respawn(admin, player);

        eventData.Invoke("respawn_player_post");
    }
    public void ChangeTeam(Admin admin, CCSPlayerController player, int team, bool announce = true)
    {
        AdminUtils.LogDebug($"Change team for player {player.PlayerName}...");
        var eventData = new EventData("c_team_player_pre");
        eventData.Insert("admin", admin);
        eventData.Insert("player", player);
        eventData.Insert("announce", announce);
        eventData.Insert("team", team);
        if (eventData.Invoke() != HookResult.Continue)
        {
            AdminUtils.LogDebug("Stopped by event PRE");
            return;
        }
        admin = eventData.Get<Admin>("admin");
        player = eventData.Get<CCSPlayerController>("player");
        announce = eventData.Get<bool>("announce");
        team = eventData.Get<int>("team");

        player.ChangeTeam((CsTeam)team);
        if (announce)
            MsgAnnounces.ChangeTeam(admin, player, team);

        eventData.Invoke("c_team_player_post");
    }
    public void SwitchTeam(Admin admin, CCSPlayerController player, int team, bool announce = true)
    {
        AdminUtils.LogDebug($"Switch team for player {player.PlayerName}...");
        var eventData = new EventData("s_team_player_pre");
        eventData.Insert("admin", admin);
        eventData.Insert("player", player);
        eventData.Insert("announce", announce);
        eventData.Insert("team", team);
        if (eventData.Invoke() != HookResult.Continue)
        {
            AdminUtils.LogDebug("Stopped by event PRE");
            return;
        }
        admin = eventData.Get<Admin>("admin");
        player = eventData.Get<CCSPlayerController>("player");
        announce = eventData.Get<bool>("announce");
        team = eventData.Get<int>("team");
        if (team == 1)
        {
            player.ChangeTeam((CsTeam)team);
        } else
            player.SwitchTeam((CsTeam)team);
        if (announce)
            MsgAnnounces.SwitchTeam(admin, player, team);

        eventData.Invoke("s_team_player_post");
    }

    public void Rename(Admin admin, CCSPlayerController player, string name, bool announce = true)
    {
        AdminUtils.LogDebug($"Switch team for player {player.PlayerName}...");
        var eventData = new EventData("rename_player_pre");
        eventData.Insert("admin", admin);
        eventData.Insert("player", player);
        eventData.Insert("name", name);
        eventData.Insert("announce", announce);
        if (eventData.Invoke() != HookResult.Continue)
        {
            AdminUtils.LogDebug("Stopped by event PRE");
            return;
        }

        admin = eventData.Get<Admin>("admin");
        player = eventData.Get<CCSPlayerController>("player");
        name = eventData.Get<string>("name");
        announce = eventData.Get<bool>("announce");
        
        if (announce)
            MsgAnnounces.Rename(admin, player, name);
        player.PlayerName = name;
        Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
        
        

        eventData.Invoke("rename_player_post");
    }
}