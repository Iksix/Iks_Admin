namespace IksAdminApi;

public class SortMenu
{
    public string Id {get; set;}
    public string ViewFlags {get; set;} = "not override";
    public bool View {get; set;} = true;
    public SortMenu(string id, string viewFlags = "not override", bool view = true)
    {
        Id = id;
        ViewFlags = viewFlags;
        View = view;
    }
}