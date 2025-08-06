using IksAdmin.Api.Application.Events;
using IksAdmin.Api.Entities.Admins;
using IksAdmin.Api.Entities.Bans;

namespace IksAdmin.Api.Application.Bans.Events;

public static class BansEvents
{
    
    public class OnBan : EventData
    {
        public override string EventName => "OnBan";
        
        
        public required Admin Admin { get; set; }
        public required Ban Ban { get; set; }
        public bool Announce { get; set; }
        public bool KickPlayer { get; set; }
    }
    
    
    public class OnUnBan : EventData
    {
        public override string EventName => "OnUnban";
        
        public required Admin Admin { get; set; }
        public required string Reason { get; set; }
        public required Ban Ban { get; set; }
        public bool Announce { get; set; }
    }
    
}