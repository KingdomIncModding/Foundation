using System;

namespace KingdomFoundation.Api
{
    /// <summary>Observe native execution, including exceptional exits, without replacing feature behavior.</summary>
    public interface IGameExecutionHooks
    {
        IDisposable Subscribe(string modId, string hookId, Func<GameExecutionCall, object> begin, Action<GameExecutionCall, object> end);
        /// <summary>Observe an optional mod's method. Native game targets must use Foundation's stable hook identifiers.</summary>
        IDisposable SubscribeExternal(string modId, System.Reflection.MethodBase method, Func<GameExecutionCall, object> begin, Action<GameExecutionCall, object> end);
    }
}
