using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using IksAdmin.Menus;
using IksAdminApi;
using MenuManager;
using Microsoft.Extensions.Localization;
using MySqlConnector;

namespace IksAdmin.Commands;

public class BaseCommands
{
    private static IIksAdminApi? _api = IksAdmin.Api;
    private static IStringLocalizer _localizer = _api!.Localizer;
    public static PluginConfig? Config;
     

    
}