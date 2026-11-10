using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Session-bound native-themed editor. All callbacks execute on the Unity main thread.</summary>
    public interface IFoundationPanelBuilder
    {
        FlowElement Machine { get; }
        string PanelId { get; }
        void Text(string value);
        void Header(string value);
        void Button(string title, Action clicked, string tooltip = null);
        void Toggle(string title, bool value, Action<bool> changed, string tooltip = null);
        /// <summary>Performs the slider operation.</summary>
        void Slider(string title, float min, float max, float value, float step, Action<float> changed);
        void Number(string title, double value, double min, double max, double step, Action<double> changed);
        void Input(string title, string value, Action<string> changed, int maxLength = 256);
        void Dropdown(string title, IReadOnlyList<string> items, int selected, Action<int> changed);
        void Search(string title, IReadOnlyList<string> items, int selected, Action<int> changed);
        void Colors(string title, IReadOnlyList<Color> colors, int selected, Action<int> changed);
        void Signal(string title, SignalSelection initial, Action<SignalSelection> changed);
        void Tabs(string key, IReadOnlyList<string> titles, Action<int, IFoundationPanelBuilder> build);
        /// <summary>Performs the columns operation.</summary>
        void Columns(int count, Action<int, IFoundationPanelBuilder> build);
        void Reorder(string title, IReadOnlyList<string> items, Action<int, int> onMove);
        void Section(string key, string title, Action<IFoundationPanelBuilder> build, bool initiallyExpanded = true);
        void Confirm(string question, Action accepted, Action rejected = null);
        void Property(FoundationUiProperty property);
        void LiveText(string title, Func<string> read, float refreshSeconds = 0.4f);
        void Rebuild();
        void Message(string value);
    }
}
