namespace IksAdmin;

public class Reason
{
    public string Title { get; set; }
    /// <summary>
    /// >= 0 - instantly ban
    /// == null - Select time
    /// == -1 - Own reason and select time
    /// </summary>
    public int? Time { get; set; }
    
    public Reason(string title, int? time)
    {
        Title = title;
        Time = time;
    }
}