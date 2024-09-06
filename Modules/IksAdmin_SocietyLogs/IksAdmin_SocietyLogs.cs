using CounterStrikeSharp.API;
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
using VkNet.Utils;
using Utilities = CounterStrikeSharp.API.Utilities;

namespace IksAdmin_SocietyLogs;

public class IksAdmin_SocietyLogs : BasePlugin, IPluginConfig<PluginConfig>
{
    public static PluginCapability<IIksAdminApi> AdminApiCapability = new("iksadmin:core");
    private IIksAdminApi? api;
    public static bool LogToVk;
    public static bool LogToDiscord;
    public static string DiscordWebHook;
    public static string DiscordAuthorName;
    public static string VkToken;
    public static long VkChatID;
    public static IStringLocalizer loc;

    
    public override string ModuleName => "IksAdmin_SocietyLogs";
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "iks";
    public override string ModuleDescription => "Logs for Iks_Admin =)";
    public PluginConfig Config { get; set; }

    private string PlayerIp(string steamId)
    {
        var players = Utilities.GetPlayers().Where(x => !x.IsBot && x.IsValid && x.AuthorizedSteamID != null);
        var player = players.FirstOrDefault(x => x.AuthorizedSteamID!.SteamId64.ToString() == steamId);
        if (player != null)
        {
            if (player.IpAddress != null)
                return player.IpAddress!;
        }
        return "Undefined";
    }

    public void OnConfigParsed(PluginConfig config)
    {
        config = ConfigManager.Load<PluginConfig>(ModuleName);
        loc = Localizer;
        LogToVk = config.LogToVk;
        LogToDiscord = config.LogToDiscord;
        DiscordWebHook = config.DiscordWebHook;
        VkToken = config.VkToken;
        VkChatID = config.VkChatID;
        DiscordAuthorName = config.DiscordAuthorName;

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
        try
        {
            api = AdminApiCapability.Get();
        }
        catch (Exception e)
        {
            Logger.LogError("IksAdminApi.dll nety :(");
        }
        foreach (var time in api!.Config.Times)
        {
            Console.WriteLine($"{time.Key} : {time.Value}");
        }

        api.OnAddAdmin += admin =>
        {
            var groupName = string.IsNullOrEmpty(admin.GroupName) ? Localizer["NONE"] : admin.GroupName;
            var groupId = admin.GroupId == -1 ? Localizer["NONE"] : admin.GroupId.ToString();
            var end = admin.End == 0 ? Localizer["NEVER"] : XHelper.GetDateStringFromUtc(admin.End);
            var vkmsg = Localizer["VK_OnAdminAdd"].Value
                .Replace("{name}", admin.Name)
                .Replace("{steamId}", admin.SteamId)
                .Replace("{groupName}", groupName)
                .Replace("{groupId}", groupId)
                .Replace("{immunity}", admin.Immunity.ToString())
                .Replace("{flags}", admin.Flags)
                .Replace("{end}", end)
                .Replace("{serverId}", admin.ServerId);
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("adminadd", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{name}", admin.Name)
                    .Replace("{steamId}", admin.SteamId)
                    .Replace("{groupName}", groupName)
                    .Replace("{groupId}", groupId)
                    .Replace("{immunity}", admin.Immunity.ToString())
                    .Replace("{flags}", admin.Flags)
                    .Replace("{end}", end)
                    .Replace("{serverId}", admin.ServerId);
                embed.Description = embed.Description
                    .Replace("{name}", admin.Name)
                    .Replace("{steamId}", admin.SteamId)
                    .Replace("{groupName}", groupName)
                    .Replace("{groupId}", groupId)
                    .Replace("{immunity}", admin.Immunity.ToString())
                    .Replace("{flags}", admin.Flags)
                    .Replace("{end}", end)
                    .Replace("{serverId}", admin.ServerId);
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{name}", admin.Name)
                            .Replace("{steamId}", admin.SteamId)
                            .Replace("{groupName}", groupName)
                            .Replace("{groupId}", groupId)
                            .Replace("{immunity}", admin.Immunity.ToString())
                            .Replace("{flags}", admin.Flags)
                            .Replace("{end}", end)
                            .Replace("{serverId}", admin.ServerId)
                        ;
                }
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
        api.OnDelAdmin += admin =>
        {
            var groupName = string.IsNullOrEmpty(admin.GroupName) ? Localizer["NONE"] : admin.GroupName;
            var groupId = admin.GroupId == -1 ? Localizer["NONE"] : admin.GroupId.ToString();
            var end = admin.End == 0 ? Localizer["NEVER"] : XHelper.GetDateStringFromUtc(admin.End);
            var vkmsg = Localizer["VK_OnAdminDel"].Value
                .Replace("{name}", admin.Name)
                .Replace("{steamId}", admin.SteamId)
                .Replace("{groupName}", groupName)
                .Replace("{groupId}", groupId)
                .Replace("{immunity}", admin.Immunity.ToString())
                .Replace("{flags}", admin.Flags)
                .Replace("{end}", end)
                .Replace("{serverId}", admin.ServerId);
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("admindel", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{name}", admin.Name)
                    .Replace("{steamId}", admin.SteamId)
                    .Replace("{groupName}", groupName)
                    .Replace("{groupId}", groupId)
                    .Replace("{immunity}", admin.Immunity.ToString())
                    .Replace("{flags}", admin.Flags)
                    .Replace("{end}", end)
                    .Replace("{serverId}", admin.ServerId);
                embed.Description = embed.Description
                    .Replace("{name}", admin.Name)
                    .Replace("{steamId}", admin.SteamId)
                    .Replace("{groupName}", groupName)
                    .Replace("{groupId}", groupId)
                    .Replace("{immunity}", admin.Immunity.ToString())
                    .Replace("{flags}", admin.Flags)
                    .Replace("{end}", end)
                    .Replace("{serverId}", admin.ServerId);
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{name}", admin.Name)
                            .Replace("{steamId}", admin.SteamId)
                            .Replace("{groupName}", groupName)
                            .Replace("{groupId}", groupId)
                            .Replace("{immunity}", admin.Immunity.ToString())
                            .Replace("{flags}", admin.Flags)
                            .Replace("{end}", end)
                            .Replace("{serverId}", admin.ServerId)
                        ;
                }
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
        api.OnAddBan += ban =>
        {
            var end = ban.Time == 0 ? Localizer["NEVER"] : XHelper.GetDateStringFromUtc(ban.End);
            var banType = ban.BanType == 1 ? "IP" : "SteamId";
            var vkmsg = Localizer["VK_OnAddBan"].Value
                .Replace("{name}", ban.Name)
                .Replace("{steamId}", ban.Sid)
                .Replace("{ip}", ban.Ip)
                .Replace("{adminSid}", ban.AdminSid)
                .Replace("{admin}", AdminName(ban.AdminSid))
                .Replace("{reason}", ban.Reason)
                .Replace("{banType}", banType)
                .Replace("{end}", end)
                .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                .Replace("{time}", GetTime(ban.Time));
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("ban", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{name}", ban.Name)
                    .Replace("{steamId}", ban.Sid)
                    .Replace("{ip}", ban.Ip)
                    .Replace("{adminSid}", ban.AdminSid)
                    .Replace("{admin}", AdminName(ban.AdminSid))
                    .Replace("{reason}", ban.Reason)
                    .Replace("{banType}", banType)
                    .Replace("{end}", end)
                    .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                    .Replace("{time}", GetTime(ban.Time));
                embed.Description = embed.Description
                    .Replace("{name}", ban.Name)
                    .Replace("{steamId}", ban.Sid)
                    .Replace("{ip}", ban.Ip)
                    .Replace("{adminSid}", ban.AdminSid)
                    .Replace("{admin}", AdminName(ban.AdminSid))
                    .Replace("{reason}", ban.Reason)
                    .Replace("{banType}", banType)
                    .Replace("{end}", end)
                    .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                    .Replace("{time}", GetTime(ban.Time));
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{name}", ban.Name)
                            .Replace("{steamId}", ban.Sid)
                            .Replace("{ip}", ban.Ip)
                            .Replace("{adminSid}", ban.AdminSid)
                            .Replace("{admin}", AdminName(ban.AdminSid))
                            .Replace("{reason}", ban.Reason)
                            .Replace("{banType}", banType)
                            .Replace("{end}", end)
                            .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                            .Replace("{time}", GetTime(ban.Time))
                        ;
                }
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
        api.OnUnBan += (ban) =>
        {
            var end = ban.Time == 0 ? Localizer["NEVER"] : XHelper.GetDateStringFromUtc(ban.End);
            var banType = ban.BanType == 1 ? "IP" : "SteamId";
            var vkmsg = Localizer["VK_OnUnBan"].Value
                .Replace("{name}", ban.Name)
                .Replace("{steamId}", ban.Sid)
                .Replace("{ip}", ban.Ip)
                .Replace("{banType}", banType)
                .Replace("{adminSid}", ban.UnbannedBy)
                .Replace("{admin}", AdminName(ban.UnbannedBy))
                .Replace("{reason}", ban.Reason)
                .Replace("{end}", end)
                .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                .Replace("{time}", GetTime(ban.Time));
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("unban", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{name}", ban.Name)
                    .Replace("{steamId}", ban.Sid)
                    .Replace("{ip}", ban.Ip)
                    .Replace("{banType}", banType)
                    .Replace("{adminSid}", ban.UnbannedBy)
                    .Replace("{admin}", AdminName(ban.UnbannedBy))
                    .Replace("{reason}", ban.Reason)
                    .Replace("{end}", end)
                    .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                    .Replace("{time}", GetTime(ban.Time));
                embed.Description = embed.Description
                    .Replace("{name}", ban.Name)
                    .Replace("{steamId}", ban.Sid)
                    .Replace("{ip}", ban.Ip)
                    .Replace("{banType}", banType)
                    .Replace("{adminSid}", ban.UnbannedBy)
                    .Replace("{admin}", AdminName(ban.UnbannedBy))
                    .Replace("{reason}", ban.Reason)
                    .Replace("{end}", end)
                    .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                    .Replace("{time}", GetTime(ban.Time));
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{name}", ban.Name)
                            .Replace("{steamId}", ban.Sid)
                            .Replace("{ip}", ban.Ip)
                            .Replace("{banType}", banType)
                            .Replace("{adminSid}", ban.UnbannedBy)
                            .Replace("{admin}", AdminName(ban.UnbannedBy))
                            .Replace("{reason}", ban.Reason)
                            .Replace("{end}", end)
                            .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                            .Replace("{time}", GetTime(ban.Time))
                        ;
                }
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
        api.OnAddGag += ban =>
        {
            Server.NextFrame(() =>
            {
                var end = ban.Time == 0 ? Localizer["NEVER"] : XHelper.GetDateStringFromUtc(ban.End);
                var vkmsg = Localizer["VK_OnAddGag"].Value
                    .Replace("{name}", ban.Name)
                    .Replace("{steamId}", ban.Sid)
                    .Replace("{adminSid}", ban.AdminSid)
                    .Replace("{ip}", PlayerIp(ban.Sid))
                    .Replace("{admin}", AdminName(ban.AdminSid))
                    .Replace("{reason}", ban.Reason)
                    .Replace("{end}", end)
                    .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                    .Replace("{time}", GetTime(ban.Time));
                Log(vkmsg);
                if (Config.DiscordMessages.TryGetValue("gag", out var emb))
                {
                    var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                    embed.Title = embed.Title
                        .Replace("{name}", ban.Name)
                        .Replace("{steamId}", ban.Sid)
                        .Replace("{adminSid}", ban.AdminSid)
                        .Replace("{ip}", PlayerIp(ban.Sid))
                        .Replace("{admin}", AdminName(ban.AdminSid))
                        .Replace("{reason}", ban.Reason)
                        .Replace("{end}", end)
                        .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                        .Replace("{time}", GetTime(ban.Time));
                    embed.Description = embed.Description
                        .Replace("{name}", ban.Name)
                        .Replace("{steamId}", ban.Sid)
                        .Replace("{adminSid}", ban.AdminSid)
                        .Replace("{ip}", PlayerIp(ban.Sid))
                        .Replace("{admin}", AdminName(ban.AdminSid))
                        .Replace("{reason}", ban.Reason)
                        .Replace("{end}", end)
                        .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                        .Replace("{time}", GetTime(ban.Time));
                    foreach (var field in embed.Fields)
                    {
                        field.Value = field.Value
                                .Replace("{name}", ban.Name)
                                .Replace("{steamId}", ban.Sid)
                                .Replace("{adminSid}", ban.AdminSid)
                                .Replace("{admin}", AdminName(ban.AdminSid))
                                .Replace("{ip}", PlayerIp(ban.Sid))
                                .Replace("{reason}", ban.Reason)
                                .Replace("{end}", end)
                                .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                                .Replace("{time}", GetTime(ban.Time))
                            ;
                    }
                    Task.Run(async () =>
                    {
                        if (!Config.LogToDiscord) return;
                        await SocietyLogger.Send(embed);
                    });
                }
            });
        };
        api.OnUnGag += (ban) =>
        {
            Server.NextFrame(() =>
            {
                var end = ban.Time == 0 ? Localizer["NEVER"] : XHelper.GetDateStringFromUtc(ban.End);
                var vkmsg = Localizer["VK_OnUnGag"].Value
                    .Replace("{name}", ban.Name)
                    .Replace("{steamId}", ban.Sid)
                    .Replace("{adminSid}", ban.AdminSid)
                    .Replace("{ip}", PlayerIp(ban.Sid))
                    .Replace("{admin}", AdminName(ban.AdminSid))
                    .Replace("{reason}", ban.Reason)
                    .Replace("{end}", end)
                    .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                    .Replace("{time}", GetTime(ban.Time));
                Log(vkmsg);
                if (Config.DiscordMessages.TryGetValue("ungag", out var emb))
                {
                    var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                    embed.Title = embed.Title
                        .Replace("{name}", ban.Name)
                        .Replace("{steamId}", ban.Sid)
                        .Replace("{adminSid}", ban.AdminSid)
                        .Replace("{ip}", PlayerIp(ban.Sid))
                        .Replace("{admin}", AdminName(ban.AdminSid))
                        .Replace("{reason}", ban.Reason)
                        .Replace("{end}", end)
                        .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                        .Replace("{time}", GetTime(ban.Time));
                    embed.Description = embed.Description
                        .Replace("{name}", ban.Name)
                        .Replace("{steamId}", ban.Sid)
                        .Replace("{adminSid}", ban.AdminSid)
                        .Replace("{ip}", PlayerIp(ban.Sid))
                        .Replace("{admin}", AdminName(ban.AdminSid))
                        .Replace("{reason}", ban.Reason)
                        .Replace("{end}", end)
                        .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                        .Replace("{time}", GetTime(ban.Time));
                    foreach (var field in embed.Fields)
                    {
                        field.Value = field.Value
                                .Replace("{name}", ban.Name)
                                .Replace("{steamId}", ban.Sid)
                                .Replace("{adminSid}", ban.UnbannedBy)
                                .Replace("{ip}", PlayerIp(ban.Sid))
                                .Replace("{admin}", AdminName(ban.UnbannedBy))
                                .Replace("{reason}", ban.Reason)
                                .Replace("{end}", end)
                                .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                                .Replace("{time}", GetTime(ban.Time))
                            ;
                    }
                    Task.Run(async () =>
                    {
                        if (!Config.LogToDiscord) return;
                        await SocietyLogger.Send(embed);
                    });
                }
            });
        };
        api.OnAddMute += ban =>
        {
            Server.NextFrame(() =>
            {
                var end = ban.Time == 0 ? Localizer["NEVER"] : XHelper.GetDateStringFromUtc(ban.End);

                var vkmsg = Localizer["VK_OnAddMute"].Value
                    .Replace("{name}", ban.Name)
                    .Replace("{steamId}", ban.Sid)
                    .Replace("{adminSid}", ban.AdminSid)
                    .Replace("{ip}", PlayerIp(ban.Sid))
                    .Replace("{admin}", AdminName(ban.AdminSid))
                    .Replace("{reason}", ban.Reason)
                    .Replace("{end}", end)
                    .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                    .Replace("{time}", GetTime(ban.Time));
                Log(vkmsg);
                if (Config.DiscordMessages.TryGetValue("mute", out var emb))
                {
                    var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                    embed.Title = embed.Title
                        .Replace("{name}", ban.Name)
                        .Replace("{steamId}", ban.Sid)
                        .Replace("{adminSid}", ban.AdminSid)
                        .Replace("{ip}", PlayerIp(ban.Sid))
                        .Replace("{admin}", AdminName(ban.AdminSid))
                        .Replace("{reason}", ban.Reason)
                        .Replace("{end}", end)
                        .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                        .Replace("{time}", GetTime(ban.Time));
                    embed.Description = embed.Description
                        .Replace("{name}", ban.Name)
                        .Replace("{steamId}", ban.Sid)
                        .Replace("{adminSid}", ban.AdminSid)
                        .Replace("{ip}", PlayerIp(ban.Sid))
                        .Replace("{admin}", AdminName(ban.AdminSid))
                        .Replace("{reason}", ban.Reason)
                        .Replace("{end}", end)
                        .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                        .Replace("{time}", GetTime(ban.Time));
                    foreach (var field in embed.Fields)
                    {
                        field.Value = field.Value
                                .Replace("{name}", ban.Name)
                                .Replace("{steamId}", ban.Sid)
                                .Replace("{adminSid}", ban.AdminSid)
                                .Replace("{ip}", PlayerIp(ban.Sid))
                                .Replace("{admin}", AdminName(ban.AdminSid))
                                .Replace("{reason}", ban.Reason)
                                .Replace("{end}", end)
                                .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                                .Replace("{time}", GetTime(ban.Time))
                            ;
                    }
                    Task.Run(async () =>
                    {
                        if (!Config.LogToDiscord) return;
                        await SocietyLogger.Send(embed);
                    });
                }
            });
        };
        api.OnUnMute += (ban) =>
        {
            Server.NextFrame(() =>
            {
                var end = ban.Time == 0 ? Localizer["NEVER"] : XHelper.GetDateStringFromUtc(ban.End);
                var vkmsg = Localizer["VK_OnUnMute"].Value
                    .Replace("{name}", ban.Name)
                    .Replace("{steamId}", ban.Sid)
                    .Replace("{ip}", PlayerIp(ban.Sid))
                    .Replace("{adminSid}", ban.UnbannedBy)
                    .Replace("{admin}", AdminName(ban.UnbannedBy))
                    .Replace("{reason}", ban.Reason)
                    .Replace("{end}", end)
                    .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                    .Replace("{time}", GetTime(ban.Time));
                Log(vkmsg);
                if (Config.DiscordMessages.TryGetValue("unmute", out var emb))
                {
                    var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                    embed.Title = embed.Title
                        .Replace("{name}", ban.Name)
                        .Replace("{steamId}", ban.Sid)
                        .Replace("{ip}", PlayerIp(ban.Sid))
                        .Replace("{adminSid}", ban.UnbannedBy)
                        .Replace("{admin}", AdminName(ban.UnbannedBy))
                        .Replace("{reason}", ban.Reason)
                        .Replace("{end}", end)
                        .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                        .Replace("{time}", GetTime(ban.Time));
                    embed.Description = embed.Description
                        .Replace("{name}", ban.Name)
                        .Replace("{steamId}", ban.Sid)
                        .Replace("{ip}", PlayerIp(ban.Sid))
                        .Replace("{adminSid}", ban.UnbannedBy)
                        .Replace("{admin}", AdminName(ban.UnbannedBy))
                        .Replace("{reason}", ban.Reason)
                        .Replace("{end}", end)
                        .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                        .Replace("{time}", GetTime(ban.Time));
                    foreach (var field in embed.Fields)
                    {
                        field.Value = field.Value
                                .Replace("{name}", ban.Name)
                                .Replace("{steamId}", ban.Sid)
                                .Replace("{ip}", PlayerIp(ban.Sid))
                                .Replace("{adminSid}", ban.UnbannedBy)
                                .Replace("{admin}", AdminName(ban.UnbannedBy))
                                .Replace("{reason}", ban.Reason)
                                .Replace("{end}", end)
                                .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                                .Replace("{time}", GetTime(ban.Time))
                            ;
                    }
                    Task.Run(async () =>
                    {
                        if (!Config.LogToDiscord) return;
                        await SocietyLogger.Send(embed);
                    });
                }
            });
        };
        api.OnSlay += (adminSid, playerInfo) =>
        {
            var vkmsg = Localizer["VK_OnSlay"].Value
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{name}", playerInfo.PlayerName)
                .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                .Replace("{ip}", playerInfo.IpAddress)
                .Replace("{adminSid}", adminSid);
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("slay", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{name}", playerInfo.PlayerName)
                    .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                    .Replace("{ip}", playerInfo.IpAddress)
                    .Replace("{adminSid}", adminSid);
                embed.Description = embed.Description
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{name}", playerInfo.PlayerName)
                    .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                    .Replace("{ip}", playerInfo.IpAddress)
                    .Replace("{adminSid}", adminSid);
                foreach (var field in embed.Fields)
                {
                    try
                    {
                        field.Value = field.Value.Replace("{admin}", AdminName(adminSid));
                        field.Value = field.Value.Replace("{name}", playerInfo.PlayerName);
                        field.Value = field.Value.Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString());
                        field.Value = field.Value.Replace("{ip}", playerInfo.IpAddress);
                        field.Value = field.Value.Replace("{adminSid}", adminSid);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }
                    
                }
                Console.WriteLine("Send To discord embed");
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
        api.OnKick += (adminSid, playerInfo, reason) =>
        {
            var vkmsg = Localizer["VK_OnKick"].Value
                .Replace("{name}", playerInfo.PlayerName)
                .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                .Replace("{ip}", playerInfo.IpAddress)
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
                .Replace("{reason}", reason);
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("kick", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{name}", playerInfo.PlayerName)
                    .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                    .Replace("{ip}", playerInfo.IpAddress)
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{reason}", reason);
                embed.Description = embed.Description
                    .Replace("{name}", playerInfo.PlayerName)
                    .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                    .Replace("{ip}", playerInfo.IpAddress)
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{reason}", reason);
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{name}", playerInfo.PlayerName)
                            .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                            .Replace("{ip}", playerInfo.IpAddress)
                            .Replace("{admin}", AdminName(adminSid))
                            .Replace("{adminSid}", adminSid)
                            .Replace("{reason}", reason)
                        ;
                }
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
        api.OnChangeTeam += (adminSid, playerInfo, oldTeam, newTeam) =>
        {
            var vkmsg = Localizer["VK_ChangeTeam"].Value
                .Replace("{name}", playerInfo.PlayerName)
                .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                .Replace("{ip}", playerInfo.IpAddress)
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
                .Replace("{oldTeam}", GetTeam(oldTeam))
                .Replace("{newTeam}", GetTeam(newTeam));
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("changeTeam", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{name}", playerInfo.PlayerName)
                    .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                    .Replace("{ip}", playerInfo.IpAddress)
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{oldTeam}", GetTeam(oldTeam))
                    .Replace("{newTeam}", GetTeam(newTeam));
                embed.Description = embed.Description
                    .Replace("{name}", playerInfo.PlayerName)
                    .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                    .Replace("{ip}", playerInfo.IpAddress)
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{oldTeam}", GetTeam(oldTeam))
                    .Replace("{newTeam}", GetTeam(newTeam));
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{name}", playerInfo.PlayerName)
                            .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                            .Replace("{ip}", playerInfo.IpAddress)
                            .Replace("{admin}", AdminName(adminSid))
                            .Replace("{adminSid}", adminSid)
                            .Replace("{oldTeam}", GetTeam(oldTeam))
                            .Replace("{newTeam}", GetTeam(newTeam))
                        ;
                }
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
        api.OnSwitchTeam += (adminSid, playerInfo, oldTeam, newTeam) =>
        {
            var vkmsg = Localizer["VK_SwitchTeam"].Value
                .Replace("{name}", playerInfo.PlayerName)
                .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                .Replace("{ip}", playerInfo.IpAddress)
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
                .Replace("{oldTeam}", GetTeam(oldTeam))
                .Replace("{newTeam}", GetTeam(newTeam));
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("switchTeam", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{name}", playerInfo.PlayerName)
                    .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                    .Replace("{ip}", playerInfo.IpAddress)
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{oldTeam}", GetTeam(oldTeam))
                    .Replace("{newTeam}", GetTeam(newTeam));
                embed.Description = embed.Description
                    .Replace("{name}", playerInfo.PlayerName)
                    .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                    .Replace("{ip}", playerInfo.IpAddress)
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{oldTeam}", GetTeam(oldTeam))
                    .Replace("{newTeam}", GetTeam(newTeam));
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{name}", playerInfo.PlayerName)
                            .Replace("{steamId}", playerInfo.SteamId.SteamId64.ToString())
                            .Replace("{ip}", playerInfo.IpAddress)
                            .Replace("{admin}", AdminName(adminSid))
                            .Replace("{adminSid}", adminSid)
                            .Replace("{oldTeam}", GetTeam(oldTeam))
                            .Replace("{newTeam}", GetTeam(newTeam))
                        ;
                }
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
        api.OnCommandUsed += (controller, info) =>
        {
            var adminSid = controller == null ? "CONSOLE" : controller.AuthorizedSteamID!.SteamId64.ToString();
            if (adminSid != "CONSOLE" && api.ThisServerAdmins.All(x => x.SteamId != adminSid))
                return;
            var vkmsg = Localizer["VK_Action"].Value
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
                .Replace("{cmd}", info.GetCommandString);
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("commandUsed", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{cmd}", info.GetCommandString);
                embed.Description = embed.Description
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{cmd}", info.GetCommandString);
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{admin}", AdminName(adminSid))
                            .Replace("{adminSid}", adminSid)
                            .Replace("{cmd}", info.GetCommandString)
                        ;
                }
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
        api.OnRename += (adminSid, target, oldName, newName) =>
        {
            var vkmsg = Localizer["VK_Rename"].Value
                .Replace("{admin}", AdminName(adminSid))
                .Replace("{adminSid}", adminSid)
                .Replace("{ip}", target.IpAddress)
                .Replace("{steamId}", target.SteamId.SteamId64.ToString())
                .Replace("{oldName}", oldName)
                .Replace("{newName}", newName);
            Log(vkmsg);
            if (Config.DiscordMessages.TryGetValue("rename", out var emb))
            {
                var embed = new EmbedModel(emb.GetTitle(), emb.GetDescription(), emb.Color, emb.GetFields());
                embed.Title = embed.Title
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{ip}", target.IpAddress)
                    .Replace("{steamId}", target.SteamId.SteamId64.ToString())
                    .Replace("{oldName}", oldName)
                    .Replace("{newName}", newName);
                embed.Description = embed.Description
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{ip}", target.IpAddress)
                    .Replace("{steamId}", target.SteamId.SteamId64.ToString())
                    .Replace("{oldName}", oldName)
                    .Replace("{newName}", newName);
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{admin}", AdminName(adminSid))
                            .Replace("{adminSid}", adminSid)
                            .Replace("{ip}", target.IpAddress)
                            .Replace("{steamId}", target.SteamId.SteamId64.ToString())
                            .Replace("{oldName}", oldName)
                            .Replace("{newName}", newName)
                        ;
                }
                Task.Run(async () =>
                {
                    if (!Config.LogToDiscord) return;
                    await SocietyLogger.Send(embed);
                });
            }
        };
    }

    private string GetTeam(CsTeam team)
    {
        string teamString = team switch
        {
            CsTeam.Spectator => "SPEC",
            CsTeam.Terrorist => "T",
            CsTeam.CounterTerrorist => "CT",
            _ => Localizer["NONE"]
        };
        return teamString;
    }
    private string ReplaceTeam(string message, string adminSid, PlayerInfo playerInfo, CsTeam oldTeam, CsTeam newTeam)
    {
        string sOldTeam = oldTeam switch
        {
            CsTeam.Spectator => "SPEC",
            CsTeam.Terrorist => "T",
            CsTeam.CounterTerrorist => "CT",
            _ => Localizer["NONE"]
        };
        string sNewTeam = newTeam switch
        {
            CsTeam.Spectator => "SPEC",
            CsTeam.Terrorist => "T",
            CsTeam.CounterTerrorist => "CT",
            _ => Localizer["NONE"]
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

    private void Log(string vkMessage)
    {
        // лог в вк
        if (LogToVk && vkMessage != "")
            SocietyLogger.SendToVk(vkMessage);
        
    }
}



public static class SocietyLogger
{
    public static void SendToDiscord(EmbedModel embed)
    {
        Task.Run(async () =>
        {
            await Send(embed);
        });
    }

    public async static Task Send(EmbedModel embed)
    {
        try
        {
            var webhookObject = new WebhookObject();
            webhookObject.AddEmbed(builder =>
            {
                builder.WithTitle(IksAdmin_SocietyLogs.loc["DISCORD_Title"])
                    .WithColor(new DColor(embed.Color.R, embed.Color.G, embed.Color.B))
                    .WithTitle(embed.Title)
                    .WithAuthor(IksAdmin_SocietyLogs.DiscordAuthorName)
                    .WithDescription(embed.Description);
                foreach (var field in embed.Fields)
                {
                    builder.AddField(field.Name, field.Value, field.InLine);
                }
            });
            await new Webhook(IksAdmin_SocietyLogs.DiscordWebHook).SendAsync(webhookObject);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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