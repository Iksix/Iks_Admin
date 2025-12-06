namespace IksAdminApi;

public class Cookie
{
    public ulong SteamId { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public int? ServerId { get; set; }
    
    public Cookie(long steamId, string cookieKey, string cookieValue, int? serverId)
    {
        SteamId = (ulong)steamId;
        Key = cookieKey;
        Value = cookieValue;
        ServerId = serverId;
    }  
    public Cookie(ulong steamId, string cookieKey, string cookieValue, int? serverId)
    {
        SteamId = steamId;
        Key = cookieKey;
        Value = cookieValue;
        ServerId = serverId;
    }   
}