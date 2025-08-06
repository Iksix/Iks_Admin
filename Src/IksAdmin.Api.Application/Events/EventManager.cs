namespace IksAdmin.Api.Application.Events;

public static class EventManager
{
    public delegate bool EventAction<T>(T eventData);

    private static readonly Dictionary<Type, List<Delegate>> EventsPre = [];
    private static readonly Dictionary<Type, List<Delegate>> EventsPost = [];

    public static void RegisterEventHandler<T>(EventAction<T> action, bool postMode = false) where T : EventData
    {
        if (postMode)
        {
            if (EventsPost.ContainsKey(typeof(T)))
                EventsPost[typeof(T)].Add(action);
            else
                EventsPost[typeof(T)] = new List<Delegate>();
            return;
        }

        if (EventsPre.ContainsKey(typeof(T)))
        {
            EventsPre[typeof(T)].Add(action);
            return;
        }
        EventsPre[typeof(T)] = new List<Delegate>();
    }

    public static void DeregisterEventHandler<T>(EventAction<T> action, bool postMode = false) where T : EventData
    {
        if (postMode)
        {
            if (EventsPost.ContainsKey(typeof(T)))
                EventsPost[typeof(T)].Remove(action);
            
            return;
        }
        
        if (EventsPre.ContainsKey(typeof(T)))
            EventsPre[typeof(T)].Remove(action);
    }

    public static void Invoke<T>(T eventData, Action action) where T : EventData
    {
        if (EventsPre.TryGetValue(typeof(T), out var eventsPre))
        {
            foreach (var eventAction in eventsPre)
            {
                bool eventResult = (bool)eventAction.DynamicInvoke(eventData)!;

                if (!eventResult)
                {
                    // TODO: Logs
                    Console.WriteLine($"Event: {eventData.EventName}_PRE, stopped by event handler");
                    return;
                }
            }
        }
        
        action();

        if (EventsPost.TryGetValue(typeof(T), out var eventsPost))
        {
            foreach (var eventAction in eventsPost)
            {
                bool eventResult = (bool)eventAction.DynamicInvoke(eventData)!;

                if (!eventResult)
                {
                    // TODO: Logs
                    Console.WriteLine($"Event: {eventData.EventName}_POST, stopped by event handler");
                    return;
                }
            }
        }
    }
    
    public static Task InvokeAsync<T>(T eventData, Action action) where T : EventData
    {
        if (EventsPre.TryGetValue(typeof(T), out var eventsPre))
        {
            foreach (var eventAction in eventsPre)
            {
                bool eventResult = (bool)eventAction.DynamicInvoke(eventData)!;

                if (!eventResult)
                {
                    // TODO: Logs
                    Console.WriteLine($"Event: {eventData.EventName}_PRE, stopped by event handler");
                    return Task.CompletedTask;
                }
            }
        }
        
        action();

        if (EventsPost.TryGetValue(typeof(T), out var eventsPost))
        {
            foreach (var eventAction in eventsPost)
            {
                bool eventResult = (bool)eventAction.DynamicInvoke(eventData)!;

                if (!eventResult)
                {
                    // TODO: Logs
                    Console.WriteLine($"Event: {eventData.EventName}_POST, stopped by event handler");
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }
}