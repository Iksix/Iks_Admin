namespace IksAdminApi;
public class PlayerSummaries
{
    public ulong SteamId {get; set;}
    public string PersonaName {get; set;}
    public string ProfileUrl {get; set;}
    public string Avatar {get; set;}
    public string AvatarFull {get; set;}
    public string AvatarMedium {get; set;}

    public PlayerSummaries(ulong steamId, string personaName, string profileUrl, string avatar, string avatarFull, string avatarMedium)
    {
        SteamId = steamId;
        PersonaName = personaName;
        ProfileUrl = profileUrl;
        Avatar = avatar;
        AvatarMedium = avatarMedium;
        AvatarFull = avatarFull;
    }
}