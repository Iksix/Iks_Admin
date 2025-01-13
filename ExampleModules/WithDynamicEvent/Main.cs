using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;

namespace WithDynamicEvent;

public class Main : AdminModule
{
    public override string ModuleName => "WithDynamicEvent";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "iks__";

    public string? ReplaceReason = null;
    public bool BlockKick = false;

    [ConsoleCommand("css_replace_reasons")]
    public void ReplaceReasonsCmd(CCSPlayerController caller, CommandInfo info)
    {
        ReplaceReason = info.GetArg(1);
    }
    [ConsoleCommand("css_block_kick")]
    public void BlockKickCmd(CCSPlayerController caller, CommandInfo info)
    {
        BlockKick = !BlockKick;
        Console.WriteLine($"Block Kick: {BlockKick}");
    }

    public override void Ready()
    {
        Api.OnDynamicEvent += OnDynamicEvent;
    }

    private HookResult OnDynamicEvent(EventData data)
    {
        switch (data.EventKey)
        {
            case "kick_player_pre":
                return OnKickPlayerPre(data);
            case "kick_player_post":
                return OnKickPlayerPost(data);
        }
        return HookResult.Continue;
    }

    private HookResult OnKickPlayerPost(EventData data)
    {
        return HookResult.Continue;
    }

    private HookResult OnKickPlayerPre(EventData data)
    {
        AdminUtils.LogDebug("KICK PRE FROM MODULE:");
        AdminUtils.LogDebug("Admin name: " + data.Get<Admin>("admin").Name);
        AdminUtils.LogDebug("Player name:" + data.Get<CCSPlayerController>("player").PlayerName);
        AdminUtils.LogDebug("Reason:" + data.Get<string>("reason"));

        if (ReplaceReason != null)
        {
            data.Set("reason", ReplaceReason);
        }

        if (BlockKick)
        {
            return HookResult.Stop;
        }
        
        return HookResult.Continue;
    }
}
