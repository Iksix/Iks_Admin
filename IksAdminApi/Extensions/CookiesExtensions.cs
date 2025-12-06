using System.Text.Json;
using CounterStrikeSharp.API.Core;

namespace IksAdminApi.Extensions;

public static class CookiesExtensions
{
    private static readonly IIksAdminApi Api = AdminUtils.CoreApi;
    
    public static void SetCookie(this CCSPlayerController player, string key, string value, bool toAllServers = false)
    {
        Api.SetCookie(player.AuthorizedSteamID!.SteamId64, key, value, toAllServers);
    }
    
    public static string? GetCookie(this CCSPlayerController player, string key)
    {
        return Api.GetCookie(player.AuthorizedSteamID!.SteamId64, key)?.Value;
    }
    
    public static void SetCookie<T>(this CCSPlayerController player, string key, T obj, bool toAllServers = false)
    {
        SetCookie(player.AuthorizedSteamID!.SteamId64, key, obj, toAllServers);
    }
    
    public static T? GetCookie<T>(this CCSPlayerController player, string key)
    {
        return GetCookie<T>(player.AuthorizedSteamID!.SteamId64, key);
    }
    
    public static void SetCookie<T>(ulong steamId, string key, T obj, bool toAllServers = false)
    {
        var json = JsonSerializer.Serialize(obj);
        
        Api.SetCookie(steamId, key, json, toAllServers);
    }
    
    public static T? GetCookie<T>(ulong steamId, string key)
    {
        var cookie = Api.GetCookie(steamId, key);

        if (cookie == null)
            return default;

        return JsonSerializer.Deserialize<T>(cookie.Value)!;
    }
}