using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Utils;
using Discord.Webhook;
using IksAdminApi;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;

namespace IksAdmin_SocietyLogs;

public class IksAdmin_SocietyLogs : BasePlugin, IPluginConfig<PluginConfig>
{
    public static PluginCapability<IIksAdminApi> AdminApiCapability = new("iksadmin:core");

    public static bool LogToVk;
    public static bool LogToDiscord;
    public static string DiscordWebHook;
    public static string VkToken;
    public static long VkChatID;
    public static IStringLocalizer loc;

    private IIksAdminApi? api;
    public override string ModuleName => "IksAdmin_SocietyLogs";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "iks";
    public override string ModuleDescription => "Logs for Iks_Admin =)";
    public PluginConfig Config { get; set; }

    public void OnConfigParsed(PluginConfig config)
    {
        config = ConfigManager.Load<PluginConfig>(ModuleName);
        loc = Localizer;
        LogToVk = config.LogToVk;
        LogToDiscord = config.LogToDiscord;
        DiscordWebHook = config.DiscordWebHook;
        VkToken = config.VkToken;
        VkChatID = config.VkChatID;

        Config = config;
    }

    private bool SkipKey(string key)
    {
        if (Localizer[key].Value.Trim() == "") return false;
        return true;
    }

    private string AdminName(string adminSid)
    {
        var admin = api!.ThisServerAdmins.FirstOrDefault(x => x.SteamId == adminSid);
        string adminName = adminSid.ToLower() == "console" ? "CONSOLE" :
            admin == null ? "~Deleted Admin~" : admin.Name;
        return adminName;
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        api = AdminApiCapability.Get();
        if (api == null) Logger.LogError("api not finded :(");

        foreach (var time in api!.Config.Times)
        {
            Console.WriteLine($"{time.Key} : {time.Value}");
        }

        api.OnAddAdmin += admin =>
        {
            Log(ReplaceAdmin(Localizer["DISCORD_OnAdminAdd"], admin), ReplaceAdmin(Localizer["VK_OnAdminAdd"], admin), "OnAdminAdd");
        };
        api.OnDelAdmin += admin =>
        {
            Log(ReplaceAdmin(Localizer["DISCORD_OnAdminDel"], admin), ReplaceAdmin(Localizer["VK_OnAdminDel"], admin), "OnAdminDel");
        };
        api.OnAddBan += ban =>
        {
            Log(ReplaceBan(Localizer["DISCORD_OnAddBan"], ban), ReplaceBan(Localizer["VK_OnAddBan"], ban), "OnAddBan");
        };
        api.OnUnBan += (ban, unbannedBy) =>
        {
            Log(ReplaceBan(Localizer["DISCORD_OnUnBan"], ban, unbannedBy), ReplaceBan(Localizer["VK_OnUnBan"], ban, unbannedBy), "OnUnBan");
        };
        api.OnAddGag += ban =>
        {
            Log(ReplaceComm(Localizer["DISCORD_OnAddGag"], ban), ReplaceComm(Localizer["VK_OnAddGag"], ban), "OnAddGag");
        };
        api.OnUnGag += (ban, unbannedBy) =>
        {
            Log(ReplaceComm(Localizer["DISCORD_OnUnGag"], ban, unbannedBy), ReplaceComm(Localizer["VK_OnUnGag"], ban, unbannedBy), "OnUnGag");
        };
        api.OnAddMute += ban =>
        {
            Log(ReplaceComm(Localizer["DISCORD_OnAddMute"], ban), ReplaceComm(Localizer["VK_OnAddMute"], ban), "OnAddMute");
        };
        api.OnUnMute += (ban, unbannedBy) =>
        {
            Log(ReplaceComm(Localizer["DISCORD_OnUnMute"], ban, unbannedBy), ReplaceComm(Localizer["VK_OnUnMute"], ban, unbannedBy), "OnUnMute");
        };
        api.OnSlay += (adminSid, playerInfo) =>
        {
            Log(ReplaceSlay(Localizer["DISCORD_OnSlay"], adminSid, playerInfo), ReplaceSlay(Localizer["VK_OnSlay"], adminSid, playerInfo), "OnSlay");
        };
        api.OnKick += (adminSid, playerInfo, reason) =>
        {
            Log(ReplaceKick(Localizer["DISCORD_OnKick"], adminSid, playerInfo, reason), ReplaceKick(Localizer["VK_OnKick"], adminSid, playerInfo, reason), "OnKick");
        };
        api.OnChangeTeam += (adminSid, playerInfo, oldTeam, newTeam) =>
        {
            Log(ReplaceTeam(Localizer["DISCORD_ChangeTeam"], adminSid, playerInfo, oldTeam, newTeam), ReplaceTeam(Localizer["VK_ChangeTeam"], adminSid, playerInfo, oldTeam, newTeam), "ChangeTeam");
        };
        api.OnSwitchTeam += (adminSid, playerInfo, oldTeam, newTeam) =>
        {
            Log(ReplaceTeam(Localizer["DISCORD_SwitchTeam"], adminSid, playerInfo, oldTeam, newTeam), ReplaceTeam(Localizer["VK_SwitchTeam"], adminSid, playerInfo, oldTeam, newTeam), "SwitchTeam");
        };
        api.OnCommandUsed += (controller, info) =>
        {
            var adminSid = controller == null ? "CONSOLE" : controller.AuthorizedSteamID!.SteamId64.ToString();
            string dMessage = Localizer["DISCORD_Action"].Value
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{cmd}", info.GetCommandString)
                ;
            string vkMessage = Localizer["VK_Action"].Value
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{cmd}", info.GetCommandString)
                ;
            Log(dMessage, vkMessage, "Action");
        };
        api.OnRename += (adminSid, target, oldName, newName) =>
        {
            string dMessage = Localizer["DISCORD_Rename"].Value
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{sid}", target.SteamId.SteamId64.ToString())
                    .Replace("{oldName}", oldName)
                    .Replace("{newName}", newName)
                ;
            string vkMessage = Localizer["VK_Rename"].Value
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{sid}", target.SteamId.SteamId64.ToString())
                    .Replace("{oldName}", oldName)
                    .Replace("{newName}", newName)
                ;
            Log(dMessage, vkMessage, "Rename");
        };
    }
    private string ReplaceTeam(string message, string adminSid, PlayerInfo playerInfo, CsTeam oldTeam, CsTeam newTeam)
    {
        string sOldTeam = oldTeam switch
        {
            CsTeam.Spectator => "SPEC",
            CsTeam.Terrorist => "T",
            CsTeam.CounterTerrorist => "CT",
            _ => "NONE"
        };
        string sNewTeam = newTeam switch
        {
            CsTeam.Spectator => "SPEC",
            CsTeam.Terrorist => "T",
            CsTeam.CounterTerrorist => "CT",
            _ => "NONE"
        };
        return message
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
                .Replace("{name}", playerInfo.PlayerName)
                .Replace("{sid}", playerInfo.SteamId.SteamId64.ToString())
                .Replace("{oldTeam}", sOldTeam)
                .Replace("{newTeam}", sNewTeam)
            ;
    }
    private string ReplaceKick(string message, string adminSid, PlayerInfo playerInfo, string reason)
    {
        return message
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
                .Replace("{name}", playerInfo.PlayerName)
                .Replace("{sid}", playerInfo.SteamId.SteamId64.ToString())
                .Replace("{reason}", reason)
            ;
    }
    private string ReplaceSlay(string message, string adminSid, PlayerInfo playerInfo)
    {
        return message
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
                .Replace("{name}", playerInfo.PlayerName)
                .Replace("{sid}", playerInfo.SteamId.SteamId64.ToString())
            ;
    }

    private string ReplaceAdmin(string message, Admin admin)
    {
        return message
                .Replace("{name}", admin.Name)
                .Replace("{sid}", admin.SteamId)
                .Replace("{flags}", admin.Flags)
                .Replace("{serverId}", admin.ServerId)
                .Replace("{groupId}", admin.GroupId.ToString())
                .Replace("{groupName}", admin.GroupName)
                .Replace("{immunity}", admin.Immunity.ToString())
                .Replace("{end}", XHelper.GetDateStringFromUtc(admin.End))
            ;
    }
    
    private string GetTime(int time)
    {
        var times = api!.Config.Times;
        time = time / 60;
        if (!times.ContainsValue(time))
            return $"{time}{api.Localizer["HELPER_Min"]}";
        return times.First(x => x.Value == time).Key;
    }
    
    private string ReplaceBan(string message, PlayerBan ban, string unbannedBy = "")
    {
        return message
            .Replace("{name}", ban.Name)
            .Replace("{reason}", ban.Reason)
            .Replace("{unbannedBy}", AdminName(unbannedBy))
            .Replace("{unbannedBySid}", unbannedBy)
            .Replace("{duration}", GetTime(ban.Time))
            .Replace("{admin}", AdminName(ban.AdminSid))
            .Replace("{adminSid}", ban.AdminSid)
            .Replace("{sid}", ban.Sid)
            .Replace("{ip}", ban.Ip)
            .Replace("{serverId}", ban.ServerId)
            .Replace("{banType}", ban.BanType switch{ 0 => "Normal", 1 => "Ip", _ => "Normal" })
            .Replace("{end}", XHelper.GetDateStringFromUtc(ban.End))
            .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created));
    }
    
    private string ReplaceComm(string message, PlayerComm comm, string unbannedBy = "")
    {
        return message
            .Replace("{name}", comm.Name)
            .Replace("{reason}", comm.Reason)
            .Replace("{duration}", GetTime(comm.Time))
            .Replace("{admin}", AdminName(comm.AdminSid))
            .Replace("{adminSid}", comm.AdminSid)
            .Replace("{sid}", comm.Sid)
            .Replace("{unbannedBy}", AdminName(unbannedBy))
            .Replace("{unbannedBySid}", unbannedBy)
            .Replace("{serverId}", comm.ServerId)
            .Replace("{end}", XHelper.GetDateStringFromUtc(comm.End))
            .Replace("{created}", XHelper.GetDateStringFromUtc(comm.Created));
    }

    private void Log(string discordMessage, string vkMessage, string key)
    {
        // лог в дс
        if (LogToDiscord && discordMessage != "")
            SocietyLogger.SendToDiscord(discordMessage);
        
        // лог в вк
        if (LogToVk && vkMessage != "")
            SocietyLogger.SendToVk(vkMessage);
        
    }
}



public static class SocietyLogger
{
    public static void SendToDiscord(string message, DColor? color = null)
    {
        color = color == null ? new DColor(255, 255, 255) : color;
        Task.Run(async () =>
        {
            var webhookObject = new WebhookObject();

            webhookObject.AddEmbed(builder =>
            {
                builder.WithTitle(IksAdmin_SocietyLogs.loc["DISCORD_Title"])
                    .WithColor(color)
                    .WithDescription(message);
            });
            await new Webhook(IksAdmin_SocietyLogs.DiscordWebHook).SendAsync(webhookObject);
        });
    }

    public static void SendToVk(string message)
    {
        Task.Run(async () =>
        {
            var apiAuthParams = new ApiAuthParams
            {
                AccessToken = IksAdmin_SocietyLogs.VkToken,
                Settings = Settings.Messages
            };
            var api = new VkApi();
            await api.AuthorizeAsync(apiAuthParams);

            try
            {
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    RandomId = new Random().Next(),
                    PeerId = IksAdmin_SocietyLogs.VkChatID,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Iks_Admin_SocietyLogs] Vk Message error: {ex.Message}");
            }
        });
    }
}