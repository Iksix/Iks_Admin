using CounterStrikeSharp.API.Core;
using IksAdmin.Api.Entities.Admins;
using XUtils;

namespace IksAdmin.Api.Application.Extensions;

/// <summary>
/// Extensions for <see cref="Admin"/> class
/// </summary>
public static class AdminExtensions
{
    /// <summary>
    /// Checks if current admins is on server
    /// <br/>
    /// For <c>CONSOLE</c> always false!
    /// </summary>
    public static bool IsOnline(this Admin admin)
    {
        var steamId = admin.SteamId;

        if (steamId == 0) return false;

        return admin.GetController() != null;
    }
    
    
    /// <summary>
    /// Gets player controller if Admin is online
    /// <br/>
    /// For <c>CONSOLE</c> always false!
    /// </summary>
    public static bool TryGetController(this Admin admin, out CCSPlayerController? controller)
    {
        controller = admin.GetController();

        return controller != null;
    }

    public static CCSPlayerController? GetController(this Admin admin)
    {
        if (admin.SteamId == null) return null;
        
        return ServerUtils.GetPlayer((ulong)admin.SteamId);
    }
}