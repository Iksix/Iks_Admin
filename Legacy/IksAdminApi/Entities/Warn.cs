namespace IksAdminApi;

public class Warn
{
    public int Id {get; set;}
    public int AdminId {get; set;}
    public int TargetId {get; set;}
    public int Duration {get; set;}
    public string Reason {get; set;}
    public int CreatedAt {get; set;} = AdminUtils.CurrentTimestamp();

    public int EndAt { get; set; } = 0;

    public int UpdatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int? DeletedAt {get; set;} = null;
    public int? DeletedBy {get; set;} = null;

    public Admin? Admin {get {
        return AdminUtils.Admin(AdminId);
    }}
    public Admin? TargetAdmin {get {
        return AdminUtils.Admin(TargetId);
    }}
    public Admin? DeletedByAdmin {get {
        return DeletedBy == null ? null : AdminUtils.Admin((int)DeletedBy);
    }}

    public Warn(
        int id,
        int adminId,
        int targetId,
        int duration,
        string reason,
        int createdAt,
        int updatedAt,
        int endAt,
        int? deletedAt,
        int? deletedBy
    )
    {
        Id = id;
        AdminId = adminId;
        TargetId = targetId;
        Duration = duration;
        Reason = reason;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        EndAt = endAt;
    }
    public Warn(
        int adminId,
        int targetId,
        int duration,
        string reason
    ) {
        AdminId = adminId;   
        TargetId = targetId;  
        Duration = duration*60;  
        Reason = reason;
        SetEndAt();
    }
    public void SetEndAt()
    {
        EndAt = Duration == 0 ? 0 : AdminUtils.CurrentTimestamp() + Duration;
    }
}