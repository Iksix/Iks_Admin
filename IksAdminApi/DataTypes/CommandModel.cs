using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using static CounterStrikeSharp.API.Modules.Commands.CommandInfo;

namespace IksAdminApi;


public class CommandModel
{
    public required string Command;
    public required string Description;
    public required string Pemission;
    public required string Usage;
    public CommandUsage CommandUsage;
    public required CommandDefinition Definition;
    public string? Tag;
    string? NotEnoughPermissionsMessage;
    int MinArgs;
}