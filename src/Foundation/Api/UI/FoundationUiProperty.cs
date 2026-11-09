using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>A typed dynamic UI property. No reflection or private machine state is changed.</summary>
    public sealed class FoundationUiProperty
    {
        public string Id { get; }
        public string Title { get; }
        public Type ValueType { get; }
        public Func<object> Read { get; }
        public Action<object> Write { get; }
        public IReadOnlyList<string> Options { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public double Step { get; }

        public FoundationUiProperty(string id, string title, Type valueType, Func<object> read,
            Action<object> write = null, IReadOnlyList<string> options = null,
            double minimum = 0, double maximum = 100, double step = 1)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Property ID required", nameof(id));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Property title required", nameof(title));
            if (valueType != typeof(bool) && valueType != typeof(int) && valueType != typeof(float)
                && valueType != typeof(double) && valueType != typeof(string) && valueType != typeof(SignalSelection))
                throw new ArgumentException("Use bool, int, float, double, string, or SignalSelection.", nameof(valueType));
            if (read == null) throw new ArgumentNullException(nameof(read));
            if (double.IsNaN(minimum) || double.IsNaN(maximum) || minimum > maximum
                || double.IsInfinity(minimum) || double.IsInfinity(maximum))
                throw new ArgumentOutOfRangeException(nameof(minimum));
            if (step <= 0 || double.IsNaN(step) || double.IsInfinity(step))
                throw new ArgumentOutOfRangeException(nameof(step));
            Id = id; Title = title; ValueType = valueType; Read = read; Write = write;
            Options = options; Minimum = minimum; Maximum = maximum; Step = step;
        }
    }
}
