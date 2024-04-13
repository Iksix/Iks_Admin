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
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "iks";
    public override string ModuleDescription => "Logs for Iks_Admin =)";
    public PluginConfig Config { get; set; }

    private string PlayerIp(string steamId)
    {
        var players = Utilities.GetPlayers().Where(x => !x.IsBot && x.IsValid);
        var player = players.FirstOrDefault(x => x.AuthorizedSteamID!.SteamId64.ToString() == steamId);
        if (player != null)
            return player.IpAddress!;
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
            if (Config.DiscordMessages.TryGetValue("adminadd", out var emb))
            {
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
                foreach (var field in embed.Fields)
                {
                    var groupName = string.IsNullOrEmpty(admin.GroupName) ? "NONE" : admin.GroupName;
                    var groupId = admin.GroupId == -1 ? "NONE" : admin.GroupId.ToString();
                    var end = admin.End == 0 ? "NEVER" : XHelper.GetDateStringFromUtc(admin.End);
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
                    await SocietyLogger.Send(embed);
                });
            }
            Log(ReplaceAdmin(Localizer["VK_OnAdminAdd"], admin));
        };
        api.OnDelAdmin += admin =>
        {
            if (Config.DiscordMessages.TryGetValue("admindel", out var emb))
            {
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
                foreach (var field in embed.Fields)
                {
                    var groupName = string.IsNullOrEmpty(admin.GroupName) ? "NONE" : admin.GroupName;
                    var groupId = admin.GroupId == -1 ? "NONE" : admin.GroupId.ToString();
                    var end = admin.End == 0 ? "NEVER" : XHelper.GetDateStringFromUtc(admin.End);
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
                    await SocietyLogger.Send(embed);
                });
            }
            Log(ReplaceAdmin(Localizer["VK_OnAdminDel"], admin));
        };
        api.OnAddBan += ban =>
        {
            if (Config.DiscordMessages.TryGetValue("ban", out var emb))
            {
                var end = ban.Time == 0 ? "NEVER" : XHelper.GetDateStringFromUtc(ban.End);
                var banType = ban.BanType == 1 ? "IP" : "SteamId";
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
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
                    await SocietyLogger.Send(embed);
                });
            }
            Log(ReplaceBan(Localizer["VK_OnAddBan"], ban));
        };
        api.OnUnBan += (ban, unbannedBy) =>
        {
            if (Config.DiscordMessages.TryGetValue("unban", out var emb))
            {
                var end = ban.Time == 0 ? "NEVER" : XHelper.GetDateStringFromUtc(ban.End);
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
                var banType = ban.BanType == 1 ? "IP" : "SteamId";
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{name}", ban.Name)
                            .Replace("{steamId}", ban.Sid)
                            .Replace("{ip}", ban.Ip)
                            .Replace("{banType}", banType)
                            .Replace("{adminSid}", unbannedBy)
                            .Replace("{admin}", AdminName(unbannedBy))
                            .Replace("{reason}", ban.Reason)
                            .Replace("{end}", end)
                            .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                            .Replace("{time}", GetTime(ban.Time))
                        ;
                }
                Task.Run(async () =>
                {
                    await SocietyLogger.Send(embed);
                });
            }
            Log(ReplaceBan(Localizer["VK_OnUnBan"], ban, unbannedBy));
        };
        api.OnAddGag += ban =>
        {
            Server.NextFrame(() =>
            {
                if (Config.DiscordMessages.TryGetValue("gag", out var emb))
                {
                    var end = ban.Time == 0 ? "NEVER" : XHelper.GetDateStringFromUtc(ban.End);
                    var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
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
                        await SocietyLogger.Send(embed);
                    });
                }
                Log(ReplaceComm(Localizer["VK_OnAddGag"], ban));
            });
        };
        api.OnUnGag += (ban, unbannedBy) =>
        {
            Server.NextFrame(() =>
            {
                if (Config.DiscordMessages.TryGetValue("ungag", out var emb))
                {
                    var end = ban.Time == 0 ? "NEVER" : XHelper.GetDateStringFromUtc(ban.End);
                    var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
                    foreach (var field in embed.Fields)
                    {
                        field.Value = field.Value
                                .Replace("{name}", ban.Name)
                                .Replace("{steamId}", ban.Sid)
                                .Replace("{adminSid}", unbannedBy)
                                .Replace("{ip}", PlayerIp(ban.Sid))
                                .Replace("{admin}", AdminName(unbannedBy))
                                .Replace("{reason}", ban.Reason)
                                .Replace("{end}", end)
                                .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                                .Replace("{time}", GetTime(ban.Time))
                            ;
                    }
                    Task.Run(async () =>
                    {
                        await SocietyLogger.Send(embed);
                    });
                }
                Log(ReplaceComm(Localizer["VK_OnUnGag"], ban, unbannedBy));
            });
        };
        api.OnAddMute += ban =>
        {
            Server.NextFrame(() =>
            {
                if (Config.DiscordMessages.TryGetValue("mute", out var emb))
                {
                    var end = ban.Time == 0 ? "NEVER" : XHelper.GetDateStringFromUtc(ban.End);
                    var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
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
                        await SocietyLogger.Send(embed);
                    });
                }
                Log(ReplaceComm(Localizer["VK_OnAddMute"], ban));
            });
        };
        api.OnUnMute += (ban, unbannedBy) =>
        {
            Server.NextFrame(() =>
            {
                if (Config.DiscordMessages.TryGetValue("unmute", out var emb))
                {
                    var end = ban.Time == 0 ? "NEVER" : XHelper.GetDateStringFromUtc(ban.End);
                    var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
                    foreach (var field in embed.Fields)
                    {
                        field.Value = field.Value
                                .Replace("{name}", ban.Name)
                                .Replace("{steamId}", ban.Sid)
                                .Replace("{ip}", PlayerIp(ban.Sid))
                                .Replace("{adminSid}", unbannedBy)
                                .Replace("{admin}", AdminName(unbannedBy))
                                .Replace("{reason}", ban.Reason)
                                .Replace("{end}", end)
                                .Replace("{created}", XHelper.GetDateStringFromUtc(ban.Created))
                                .Replace("{time}", GetTime(ban.Time))
                            ;
                    }
                    Task.Run(async () =>
                    {
                        await SocietyLogger.Send(embed);
                    });
                }
                Log(ReplaceComm(Localizer["VK_OnUnMute"], ban, unbannedBy));
            });
        };
        api.OnSlay += (adminSid, playerInfo) =>
        {
            if (Config.DiscordMessages.TryGetValue("slay", out var emb))
            {
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
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
                    await SocietyLogger.Send(embed);
                });
            }
            Log(ReplaceSlay(Localizer["VK_OnSlay"], adminSid, playerInfo));
        };
        api.OnKick += (adminSid, playerInfo, reason) =>
        {
            if (Config.DiscordMessages.TryGetValue("kick", out var emb))
            {
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
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
                    await SocietyLogger.Send(embed);
                });
            }
            Log(ReplaceKick(Localizer["VK_OnKick"], adminSid, playerInfo, reason));
        };
        api.OnChangeTeam += (adminSid, playerInfo, oldTeam, newTeam) =>
        {
            if (Config.DiscordMessages.TryGetValue("changeTeam", out var emb))
            {
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
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
                    await SocietyLogger.Send(embed);
                });
            }
            Log(ReplaceTeam(Localizer["VK_ChangeTeam"], adminSid, playerInfo, oldTeam, newTeam));
        };
        api.OnSwitchTeam += (adminSid, playerInfo, oldTeam, newTeam) =>
        {
            if (Config.DiscordMessages.TryGetValue("switchTeam", out var emb))
            {
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
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
                    await SocietyLogger.Send(embed);
                });
            }
            Log(ReplaceTeam(Localizer["VK_SwitchTeam"], adminSid, playerInfo, oldTeam, newTeam));
        };
        api.OnCommandUsed += (controller, info) =>
        {
            Console.WriteLine("1111");
            var adminSid = controller == null ? "CONSOLE" : controller.AuthorizedSteamID!.SteamId64.ToString();
            if (adminSid != "CONSOLE" && api.ThisServerAdmins.All(x => x.SteamId != adminSid))
                return;
            Console.WriteLine("2222");
            if (Config.DiscordMessages.TryGetValue("commandUsed", out var emb))
            {
                Console.WriteLine("3333");
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
                foreach (var field in embed.Fields)
                {
                    Console.WriteLine("5555");
                    field.Value = field.Value
                            .Replace("{admin}", AdminName(adminSid))
                            .Replace("{adminSid}", adminSid)
                            .Replace("{cmd}", info.GetCommandString)
                        ;
                    Console.WriteLine("4444");
                }
                Task.Run(async () =>
                {
                    await SocietyLogger.Send(embed);
                });
                Console.WriteLine("6666");
            }
            string vkMessage = Localizer["VK_Action"].Value
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{cmd}", info.GetCommandString)
                ;
            Log(vkMessage);
        };
        api.OnRename += (adminSid, target, oldName, newName) =>
        {
            Console.WriteLine(AdminName(adminSid));
            Console.WriteLine(adminSid);
            Console.WriteLine(target.IpAddress);
            Console.WriteLine(target.SteamId.SteamId64.ToString());
            Console.WriteLine(oldName);
            Console.WriteLine(newName);
            if (Config.DiscordMessages.TryGetValue("rename", out var emb))
            {
                var embed = new EmbedModel(emb.Title, emb.Description, emb.Color, emb.GetFields());
                foreach (var field in embed.Fields)
                {
                    field.Value = field.Value
                            .Replace("{admin}", AdminName(adminSid))
                            .Replace("{adminSid}", adminSid)
                            .Replace("{ip}", target.IpAddress)
                            .Replace("{sid}", target.SteamId.SteamId64.ToString())
                            .Replace("{oldName}", oldName)
                            .Replace("{newName}", newName)
                        ;
                }
                Task.Run(async () =>
                {
                    await SocietyLogger.Send(embed);
                });
            }
            string vkMessage = Localizer["VK_Rename"].Value
                    .Replace("{admin}", AdminName(adminSid))
                    .Replace("{adminSid}", adminSid)
                    .Replace("{ip}", target.IpAddress)
                    .Replace("{sid}", target.SteamId.SteamId64.ToString())
                    .Replace("{oldName}", oldName)
                    .Replace("{newName}", newName)
                ;
            Log(vkMessage);
        };
    }

    private string GetTeam(CsTeam team)
    {
        string teamString = team switch
        {
            CsTeam.Spectator => "SPEC",
            CsTeam.Terrorist => "T",
            CsTeam.CounterTerrorist => "CT",
            _ => "NONE"
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
        Console.WriteLine("d12222");
        Task.Run(async () =>
        {
            await Send(embed);
        });
    }

    public async static Task Send(EmbedModel embed)
    {
        try
        {
            Console.WriteLine("d1");
            var webhookObject = new WebhookObject();
            Console.WriteLine("d2");
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
            Console.WriteLine("d3");
            await new Webhook(IksAdmin_SocietyLogs.DiscordWebHook).SendAsync(webhookObject);
            Console.WriteLine("d4");
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