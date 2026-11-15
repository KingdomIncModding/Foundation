using System;
using UnityEngine;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Invocation information independent of native game class names.</summary>
    public sealed class GameHookCall
    {
        public Component View { get; }
        /// <summary>Foundation-owned model identity, or a Unity component for a native view invocation.</summary>
        public object Subject { get; }
        public bool OriginalRan { get; internal set; }
        public bool IsCancelled { get; private set; }

        private readonly object[] arguments;
        private readonly int[] mutableArguments;
        internal object NativeSubject { get; }
        internal object[] NativeArguments { get; }

        private readonly Type nativeResultType;
        private object result;
        internal bool HasResult { get; private set; }
        internal object ResultValue => GameValueCodec.Unwrap(result);

        internal GameHookCall(object instance, object[] arguments, Type nativeResultType, int[] mutableArguments = null)
        {
            NativeSubject = instance;
            NativeArguments = arguments ?? Array.Empty<object>();
            View = instance as Component;
            Subject = GameValueCodec.Wrap(instance);
            this.arguments = NativeArguments.Select(GameValueCodec.Wrap).ToArray();
            this.nativeResultType = nativeResultType ?? typeof(void);
            this.mutableArguments = mutableArguments ?? Array.Empty<int>();
        }

        public bool IsArgumentMutable(int index)
        {
            if (index < 0 || index >= arguments.Length)
                return false;
            for (int i = 0; i < mutableArguments.Length; i++)
                if (mutableArguments[i] == index)
                    return true;
            return false;
        }

        public T Argument<T>(int index)
        {
            if (index < 0 || index >= arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            // Native objects are never exposed through this contract.
            if (arguments[index] != null && !(arguments[index] is Component) && arguments[index].GetType().Assembly == typeof(Assets.Source.Player.GamePlayer).Assembly)
                throw new InvalidOperationException("This native argument requires a Foundation adapter.");
            return (T)arguments[index];
        }

        public void SetArgument<T>(int index, T value)
        {
            if (index < 0 || index >= arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (!IsArgumentMutable(index))
                throw new InvalidOperationException($"Hook argument {index} is read-only; this Foundation hook does not propagate argument edits to the native method.");
            RequireFrameworkValue(value);
            if (arguments[index] != null && (value == null || !arguments[index].GetType().IsInstanceOfType(value)))
                throw new ArgumentException("Hook argument type must remain compatible with its Foundation contract.", nameof(value));
            arguments[index] = value;
        }

        public T Result<T>() => (T)result;
        public void SetResult<T>(T value)
        {
            RequireFrameworkValue(value);
            global::System.Object native = GameValueCodec.Unwrap(value);
            if (nativeResultType == typeof(void) || (native == null ? nativeResultType.IsValueType && Nullable.GetUnderlyingType(nativeResultType) == null : !nativeResultType.IsInstanceOfType(native)))
                throw new ArgumentException("Result must match the Foundation hook's return contract.", nameof(value));
            result = value;
            HasResult = true;
        }

        internal void CaptureResult(object value)
        {
            result = GameValueCodec.Wrap(value);
            HasResult = false;
        }

        private static void RequireFrameworkValue(object value)
        {
            if (value != null && !(value is Component) && value.GetType().Assembly == typeof(Assets.Source.Player.GamePlayer).Assembly)
                throw new ArgumentException("Native values require a Foundation adapter.");
        }

        /// <summary>Suppress native handling. Cancellation is monotonic across subscribers.</summary>
        public void Cancel()
        {
            IsCancelled = true;
        }
    }
}
