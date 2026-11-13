using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KingdomFoundation.Json
{
    public sealed class JsonObject : IEnumerable<KeyValuePair<string,JsonValue>>, IEnumerable<JsonValue>
    {
        private static readonly ConditionalWeakTable<LightJson.JsonObject,JsonObject> Cache=new ConditionalWeakTable<LightJson.JsonObject,JsonObject>();
        internal LightJson.JsonObject Native { get; }
        internal static JsonObject Wrap(LightJson.JsonObject native)=>native==null ? null : Cache.GetValue(native,value=>new JsonObject(value));
        private JsonObject(LightJson.JsonObject native) { Native=native; }
        public JsonObject() { Native=new LightJson.JsonObject(); Cache.Add(Native,this); }
        public int Count=>Native.Count;
        public JsonValue this[string key] { get=>new JsonValue(Native[key]); set=>Native[key]=value.Native; }
        public JsonObject Add(string key) { Native.Add(key); return this; }
        public JsonObject Add(string key,JsonValue value) { Native.Add(key,value.Native); return this; }
        public JsonObject AddIfNotNull(string key,JsonValue value) { Native.AddIfNotNull(key,value.Native); return this; }
        public bool Remove(string key)=>Native.Remove(key);
        public JsonObject Clear() { Native.Clear(); return this; }
        public JsonObject Rename(string oldKey,string newKey) { Native.Rename(oldKey,newKey); return this; }
        public bool ContainsKey(string key)=>Native.ContainsKey(key);
        public bool Contains(JsonValue value)=>Native.Contains(value.Native);
        /// <summary>Append entries to a reusable buffer without allocating a managed iterator.</summary>
        public void CopyEntriesTo(List<KeyValuePair<string,JsonValue>> target)
        {
            if(target==null)throw new System.ArgumentNullException(nameof(target));
            var properties=JsonObjectStorage.Properties(Native);
            if(properties is Dictionary<string,LightJson.JsonValue> dictionary)
            {foreach(var entry in dictionary)target.Add(new KeyValuePair<string,JsonValue>(entry.Key,new JsonValue(entry.Value)));}
            else
            {foreach(var entry in properties)target.Add(new KeyValuePair<string,JsonValue>(entry.Key,new JsonValue(entry.Value)));}
        }
        public IEnumerator<KeyValuePair<string,JsonValue>> GetEnumerator()
        {
            foreach (var entry in Native) yield return new KeyValuePair<string,JsonValue>(entry.Key,new JsonValue(entry.Value));
        }
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator()
        { foreach (var entry in Native) yield return new JsonValue(entry.Value); }
        public override string ToString()=>Native.ToString();
        public string ToString(bool pretty)=>Native.ToString(pretty);
    }
}
