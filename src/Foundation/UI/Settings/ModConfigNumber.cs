using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Staged finite numeric entry with user-visible validation feedback.</summary>
    public sealed class ModConfigNumber : IModConfigElement, IModConfigEditSession
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public double Minimum { get; }
        public double Maximum { get; }
        public double Step { get; }
        /// <summary>Gets or sets the on changed value.</summary>
        public Action<double> OnChanged { get; set; }
        private readonly Func<double> getter;
        private double committed;
        private double pending;
        private TMP_InputField active;
        private string editing;

        public double Value
        {
            get => pending;
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value < Minimum || value > Maximum)
                    throw new ArgumentOutOfRangeException(nameof(value));
                pending = value;
                editing = value.ToString("0.########", CultureInfo.InvariantCulture);
                if (active) active.SetTextWithoutNotify(editing);
            }
        }

        public ModConfigNumber(string label, double minimum, double maximum, double value,
            double step, Action<double> changed, Func<double> getter = null, string id = null)
        {
            if (double.IsNaN(minimum) || double.IsNaN(maximum) || double.IsInfinity(minimum)
                || double.IsInfinity(maximum) || minimum > maximum) throw new ArgumentOutOfRangeException(nameof(minimum));
            if (step <= 0 || double.IsInfinity(step) || double.IsNaN(step)) throw new ArgumentOutOfRangeException(nameof(step));
            Minimum = minimum; Maximum = maximum; Step = step;
            Label = label ?? "";
            Id = id ?? Guid.NewGuid().ToString("N");
            this.getter = getter;
            OnChanged = changed;
            Value = value;
            committed = pending;
        }

        /// <summary>Creates view.</summary>
        public GameObject CreateView(IModConfigContext context)
        {
            var row = context.Templates.CreateInputRow(context.Container, Label, editing, 48,
                text => editing = text, out active);
            row.SetActive(IsVisible);
            return row;
        }
        public void BeginEdit()
        {
            Value = getter?.Invoke() ?? committed;
            committed = pending;
        }
        /// <summary>Performs the commit operation.</summary>
        public void Commit()
        {
            if (!double.TryParse(editing, NumberStyles.Float, CultureInfo.InvariantCulture, out var candidate)
                || double.IsNaN(candidate) || double.IsInfinity(candidate) || candidate < Minimum || candidate > Maximum)
                throw new ArgumentOutOfRangeException(nameof(Value), "Numeric input is outside the allowed range.");
            candidate = Math.Max(Minimum, Math.Min(Maximum,
                Minimum + Math.Round((candidate - Minimum) / Step) * Step));
            pending = candidate;
            if (Math.Abs(pending - committed) < 0.00000001) return;
            OnChanged?.Invoke(pending);
            committed = getter?.Invoke() ?? pending;
            Value = committed;
        }
        /// <summary>Performs the cancel operation.</summary>
        public void Cancel() => BeginEdit();
    }
}
