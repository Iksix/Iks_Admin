using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.NETCore.Plugins.Loader;

namespace IksAdminApi;

public class AdminToServer
{
    public int AdminId {get; set;}
    public int? ServerId {get; set;}
    Admin? Admin {get {
        return AdminUtils.Admin(AdminId);
    }}
    ServerModel? Server {get {
        return AdminUtils.CoreApi.AllServers.FirstOrDefault(x => x.Id == ServerId);
    }}
}