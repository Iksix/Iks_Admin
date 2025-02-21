namespace IksAdminApi;

public class Utils
{
    public static string GetDateString( int unixTimeStamp )
    {
        var dateTime = UnixTimeStampToDateTime( unixTimeStamp );
        return dateTime.ToString( "dd.MM.yyyy HH:mm:ss" );
    }
    public static string GetDateString( int unixTimeStamp, string format = "dd.MM.yyyy HH:mm:ss" )
    {
        var dateTime = UnixTimeStampToDateTime( unixTimeStamp );
        return dateTime.ToString( format );
    }
    public static DateTime UnixTimeStampToDateTime( int unixTimeStamp )
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
        return dateTime;
    }
}