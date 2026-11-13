using System;

namespace KingdomFoundation.Api
{
    /// <summary>Foundation-owned native hooks; feature handlers remain in the subscribing mod.</summary>
    public interface IGameHookRegistry
    {
        IDisposable Subscribe(string modId, string hookId, Action<GameHookCall> before = null,
            Action<GameHookCall> after = null, int priority = 0);
        IDisposable SubscribeFiltered(string modId,string hookId,GameHookFilter filter,Action<GameHookCall> before=null,
            Action<GameHookCall> after=null,int priority=0);
    }
}
