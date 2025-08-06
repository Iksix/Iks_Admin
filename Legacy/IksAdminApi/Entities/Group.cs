namespace IksAdminApi;

public class Group {
    public int Id {get; set;}
    public string Name {get; set;}
    public string Flags {get; set;}
    public int Immunity {get; set;}
    public string? Comment {get; set;}

    public List<Admin> Admins { get; set; } = [];
    
    public GroupLimitation[] Limitations {get {
        return AdminUtils.CoreApi.GroupLimitations.Where(x => x.GroupId == Id).ToArray();
    }}

    public Group() {}
    
    /// <summary>
    /// For getting from db
    /// </summary>
    public Group(int id, string name, string flags, int immunity, string? comment)
    {
        Id = id;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        Comment = comment;
    }
    /// <summary>
    /// For creating new group
    /// </summary>
    public Group(string name, string flags, int immunity, string? comment = null)
    {
        Name = name;
        Flags = flags;
        Immunity = immunity;
        Comment = comment;
    }
    
}