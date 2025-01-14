using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin;

public static class MsgAnnounces
{
    private static AdminApi _api = Main.AdminApi;
    private static IStringLocalizer _localizer = _api.Localizer;
    public static void BanAdded(PlayerBan ban)
    {
        var str = ban.BanType == 0 ? _localizer["Announce.BanAdded"] : _localizer["Announce.BanAddedIp"];
        AdminUtils.PrintToServer(str.Value
            .Replace("{admin}", ban.Admin!.CurrentName)
            .Replace("{name}", ban.NameString)
            .Replace("{reason}", ban.Reason)
            .Replace("{ip}", ban.IpString)
            .Replace("{duration}", AdminUtils.GetDurationString(ban.Duration)), tag: _localizer["Tag"]
        );
    }
    public static void Unbanned(PlayerBan ban)
    {
        AdminUtils.PrintToServer(_localizer["Announce.Unbanned"].Value
            .Replace("{admin}", ban.UnbannedByAdmin!.CurrentName)
            .Replace("{name}", ban.NameString)
            .Replace("{reason}", ban.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(ban.Duration)), tag: _localizer["Tag"]
        );
    }

    public static void GagAdded(PlayerComm gag)
    {
        AdminUtils.PrintToServer(_localizer["Announce.GagAdded"].Value
            .Replace("{admin}", gag.Admin!.CurrentName)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.Reason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: _localizer["Tag"]
        );
    }
    public static void UnGagged(PlayerComm gag)
    {
        AdminUtils.PrintToServer(_localizer["Announce.UnGagged"].Value
            .Replace("{admin}", gag.UnbannedByAdmin!.CurrentName)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: _localizer["Tag"]
        );
    }

    public static void MuteAdded(PlayerComm mute)
    {
        AdminUtils.PrintToServer(_localizer["Announce.MuteAdded"].Value
            .Replace("{admin}", mute.Admin!.CurrentName)
            .Replace("{name}", mute.Name)
            .Replace("{reason}", mute.Reason)
            .Replace("{duration}", AdminUtils.GetDurationString(mute.Duration)), tag: _localizer["Tag"]
        );
    }
     public static void UnMuted(PlayerComm gag)
    {
        AdminUtils.PrintToServer(_localizer["Announce.UnMuted"].Value
            .Replace("{admin}", gag.UnbannedByAdmin!.CurrentName)
            .Replace("{name}", gag.Name)
            .Replace("{reason}", gag.UnbanReason)
            .Replace("{duration}", AdminUtils.GetDurationString(gag.Duration)), tag: _localizer["Tag"]
        );
    }
    public static void SilenceAdded(PlayerComm comm)
    {
        AdminUtils.PrintToServer(_localizer["Announce.SilenceAdded"].Value
                .Replace("{admin}", comm.Admin!.CurrentName)
                .Replace("{name}", comm.Name)
                .Replace("{reason}", comm.Reason)
                .Replace("{duration}", AdminUtils.GetDurationString(comm.Duration)), tag: _localizer["Tag"]
        );
    }
    public static void UnSilenced(PlayerComm comm)
    {
        AdminUtils.PrintToServer(_localizer["Announce.UnSilenced"].Value
                .Replace("{admin}", comm.UnbannedByAdmin!.CurrentName)
                .Replace("{name}", comm.Name)
                .Replace("{reason}", comm.UnbanReason)
                .Replace("{duration}", AdminUtils.GetDurationString(comm.Duration)), tag: _localizer["Tag"]
        );
    }

   
    public static void Kick(Admin admin, CCSPlayerController player, string reason)
    {
        AdminUtils.PrintToServer(_localizer["Announce.Kick"].Value
                .Replace("{admin}", admin!.CurrentName)
                .Replace("{name}", player.PlayerName)
                .Replace("{reason}", reason)
        );
    }

    public static void Slay(Admin admin, CCSPlayerController player)
    {
        AdminUtils.PrintToServer(_localizer["Announce.Slay"].Value
                .Replace("{admin}", admin!.CurrentName)
                .Replace("{name}", player.PlayerName)
        );
    }

    public static void Respawn(Admin admin, CCSPlayerController player)
    {
        AdminUtils.PrintToServer(_localizer["Announce.Respawn"].Value
                .Replace("{admin}", admin!.CurrentName)
                .Replace("{name}", player.PlayerName)
        );
    }
    private static string TeamString(int team) 
    {
        return team switch {
            1 => _localizer["Other.Team.Spec"],
            2 => _localizer["Other.Team.T"],
            3 => _localizer["Other.Team.CT"],
            _ => "NONE"
        };
    }
    public static void ChangeTeam(Admin admin, CCSPlayerController player, int team)
    {
        AdminUtils.PrintToServer(_localizer["Announce.ChangeTeam"].Value
                .Replace("{admin}", admin!.CurrentName)
                .Replace("{name}", player.PlayerName)
                .Replace("{team}", TeamString(team))
        );
    }
    public static void SwitchTeam(Admin admin, CCSPlayerController player, int team)
    {
        AdminUtils.PrintToServer(_localizer["Announce.SwitchTeam"].Value
                .Replace("{admin}", admin!.CurrentName)
                .Replace("{name}", player.PlayerName)
                .Replace("{team}", TeamString(team))
        );
    }
    public static void Warn(Warn warn)
    {
        AdminUtils.PrintToServer(_localizer["Announce.Warn"].Value
                .Replace("{admin}", AdminUtils.Admin(warn.AdminId)!.CurrentName)
                .Replace("{name}", AdminUtils.Admin(warn.TargetId)!.CurrentName)
                .Replace("{reason}", warn.Reason)
                .Replace("{now}", AdminUtils.Admin(warn.TargetId)!.Warns.Count.ToString())
                .Replace("{max}", _api.Config.MaxWarns.ToString())
                .Replace("{duration}", (AdminUtils.GetDurationString(warn.Duration)).ToString()), tag: _localizer["Tag"]
        );
    }

    public static void WarnDelete(Warn warn)
    {
        AdminUtils.PrintToServer(_localizer["Announce.WarnDelete"].Value
                .Replace("{admin}", AdminUtils.Admin(warn.AdminId)!.CurrentName)
                .Replace("{name}", AdminUtils.Admin(warn.TargetId)!.CurrentName)
                .Replace("{reason}", warn.Reason)
                .Replace("{now}", AdminUtils.Admin(warn.TargetId)!.Warns.Count.ToString())
                .Replace("{max}", _api.Config.MaxWarns.ToString())
                .Replace("{id}", warn.Id.ToString()), tag: _localizer["Tag"]
        );
    }
}