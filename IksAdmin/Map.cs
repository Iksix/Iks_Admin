namespace IksAdmin;

public class Map
{
    public string Title { get; set; }
    public string Id { get; set; }
    public bool Workshop { get; set; }
    
    public Map(string title, string id, bool workshop)
    {
        Title = title;
        Id = id;
        Workshop = workshop;
    }
}