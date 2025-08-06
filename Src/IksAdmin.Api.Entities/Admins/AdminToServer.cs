using IksAdmin.Api.Entities.Servers;

namespace IksAdmin.Api.Entities.Admins;

public class AdminToServer
{
    public int Id { get; set; }

    public int AdminId { get; set; }
    public required Admin Admin { get; set; }

    public int? ServerId { get; set; }
    public Server? Server { get; set; }
}