using System;
using System.Collections.Generic;
using System.Linq;
using KingdomFoundation.Api;
namespace KingdomFoundation.Api
{
    public sealed class OptionsTabRegistry : IOptionsTabRegistry
    {
        readonly Dictionary<(string, string), OptionsTabRegistration> tabs = new Dictionary<(string, string), OptionsTabRegistration>();
        readonly List<IOptionsView> views = new List<IOptionsView>();
        readonly List<OptionsViewObserver> observers = new List<OptionsViewObserver>();
        public IReadOnlyList<IOptionsTabRegistration> Tabs => Array.AsReadOnly(tabs.Values.OrderBy(t => t.Tab.Order)
            .ThenBy(t => t.ModId, StringComparer.Ordinal).ThenBy(t => t.Id, StringComparer.Ordinal).Cast<IOptionsTabRegistration>().ToArray());
        public IReadOnlyList<IOptionsView> OpenViews => Array.AsReadOnly(views.Where(v => v.IsOpen).ToArray());
        public event Action Changed;
        public IOptionsTabRegistration Register(IOptionsTab tab)
        {
            Validate(tab); var key = (tab.ModId, tab.Id);
            if (tabs.ContainsKey(key)) throw new ArgumentException("This mod already registered this tab ID.", nameof(tab));
            var registration = new OptionsTabRegistration(this, tab); tabs.Add(key, registration); Notify(); return registration;
        }
        internal static void ValidateIdentity(string modId, string id, string title)
        {
            if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Tab mod GUID, local ID and title must not be blank.");
        }
        internal static void Validate(IOptionsTab tab)
        {
            if (tab == null) throw new ArgumentNullException(nameof(tab));
            ValidateIdentity(tab.ModId, tab.Id, tab.Title);
            if (tab.Settings == null || tab.Settings.ModId != tab.ModId) throw new ArgumentException("The tab must supply settings owned by the same mod.", nameof(tab));
        }
        public bool TryGet(string modId, string id, out IOptionsTabRegistration registration)
        {
            registration = null; if (modId == null || id == null || !tabs.TryGetValue((modId, id), out var item)) return false;
            registration = item; return true;
        }
        internal void Remove(OptionsTabRegistration registration)
        {
            var key = (registration.ModId, registration.Id);
            if (tabs.TryGetValue(key, out var current) && ReferenceEquals(current, registration)) { tabs.Remove(key); Notify(); }
        }
        internal void Notify()
        {
            if (Changed == null) return;
            foreach (Action callback in Changed.GetInvocationList()) try { callback(); } catch (Exception error) { FoundationApi.Report(error); }
        }
        public IDisposable ObserveViews(Action<IOptionsView> opened)
        {
            if (opened == null) throw new ArgumentNullException(nameof(opened));
            var observer = new OptionsViewObserver(this, opened); observers.Add(observer);
            foreach (var view in OpenViews) Dispatch(observer, view);
            return observer;
        }
        internal void Open(IOptionsView view)
        {
            if (!views.Contains(view)) views.Add(view);
            foreach (var observer in observers.ToArray()) Dispatch(observer, view);
        }
        static void Dispatch(OptionsViewObserver observer, IOptionsView view)
        { try { observer.Callback?.Invoke(view); } catch (Exception error) { FoundationApi.Report(error); } }
        internal void Close(IOptionsView view) { views.Remove(view); }
        internal void RemoveObserver(OptionsViewObserver observer) { observers.Remove(observer); }
    }
}
