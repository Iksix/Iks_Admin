
namespace Iks_Admin;

public class CustomItem
{
    public string Name {get;set;}
    public string Flag {get;set;}
    public string Command {get;set;}
    public bool ExecuteFromConsole {get;set;} // flase - from user, true - from console

    public CustomItem(string Name, string Flag, string Command, bool ExecuteFromConsole) // 
    {
        this.Name = Name;
        this.Flag = Flag;
        this.Command = Command;
        this.ExecuteFromConsole = ExecuteFromConsole;
    }
    
}