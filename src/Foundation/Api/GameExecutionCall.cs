using System;

namespace KingdomFoundation.Api
{
    /// <summary>Read-only execution metadata. The original exception is never replaced by observers.</summary>
    public sealed class GameExecutionCall
    {
        public string HookId { get; }
        /// <summary>Stable activity operation label; Foundation maps native method changes to these labels.</summary>
        public string Operation {get;}
        public int ArgumentCount=>arguments.Length;
        public object Subject { get; }
        public bool OriginalRan { get; internal set; }
        public Exception Exception { get; internal set; }
        private readonly object[] arguments;
        internal readonly GameExecutionSubscription[] Observers;
        internal readonly object[] States;
        internal static readonly object BeginFailed = new object();
        internal GameExecutionCall(string id,string operation, object subject, object[] args, GameExecutionSubscription[] observers)
        { HookId = id; Operation=operation; Subject = GameValueCodec.Wrap(subject); arguments = args; Observers = observers; States = new object[observers.Length]; }
        public T Argument<T>(int index) => (T)GameValueCodec.Wrap(arguments[index]);
        /// <summary>Text representation, including native enum labels, without exposing their types.</summary>
        public string ArgumentText(int index) => arguments[index]?.ToString();
    }
}
