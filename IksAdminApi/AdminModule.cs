using CounterStrikeSharp.API.Core;

namespace IksAdminApi;

public abstract class AdminModule : BasePlugin
{
    public static IIksAdminApi Api { get; set; } = null!;
    public static BasePlugin Instance {get; set;} = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        Instance = this;
        Api.EOnModuleLoaded(this);
        Api.SetCommandInititalizer(ModuleName);
        InitializeCommands();
        Api.ClearCommandInitializer();
        Ready();
    }
    /// <summary>
    /// Used instead Load()
    /// </summary>
    public virtual void Ready()
    {
        // Used instead Load()
    }
    /// <summary>
    /// use AdminApi.AddNewCommand(...) here
    /// </summary>
    public virtual void InitializeCommands()
    {
        // use AdminApi.AddNewCommand(...) here
    }

    public override void Unload(bool hotReload)
    {
        Api.EOnModuleUnload(this);
    }
}