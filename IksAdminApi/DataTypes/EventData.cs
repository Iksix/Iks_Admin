using CounterStrikeSharp.API.Core;

namespace IksAdminApi;



public class EventData
{
    public string EventKey { get; set;  }

    public HookResult Invoke()
    {
        return AdminUtils.CoreApi.InvokeDynamicEvent(this);
    }
    public HookResult Invoke(string eventKey)
    {
        EventKey = eventKey;
        return AdminUtils.CoreApi.InvokeDynamicEvent(this);
    }

    public EventData(string eventKey)
    {
        EventKey = eventKey;
    }

    private Dictionary<string, Object> _data = new();
    
    /// <summary>
    /// Use that for add new data value
    /// </summary>
    public void Insert(string key, object value)
    {
        _data.Add(key, value);
    }
    /// <summary>
    /// Use that for add new data value
    /// </summary>
    public void Insert<T>(string key, List<T> value)
    {
        _data.Add(key, value);
    }
    /// <summary>
    /// Use that for add new data value
    /// </summary>
    public void Insert<T>(string key, T value)
    {
        _data.Add(key, value);
    }
    /// <summary>
    /// Use that for getting exists data value
    /// </summary>
    public T Get<T>(string key)
    {
        if (!_data.TryGetValue(key, out var value))
        {
            throw new Exception("Trying to get event data that doesn't exist");
        }
        return (T)value;
    }
    
    /// <summary>
    /// Use that for settings exists data value
    /// </summary>
    public void Set<T>(string key, T value)
    {
        if (!_data.ContainsKey(key))
        {
            throw new Exception("Trying to set event data that doesn't exist");
        }
        _data[key] = value;
    }
}