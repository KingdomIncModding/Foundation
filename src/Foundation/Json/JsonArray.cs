using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace KingdomFoundation.Json
{
    public sealed class JsonArray : IEnumerable<JsonValue>
    {
        private static readonly ConditionalWeakTable<LightJson.JsonArray,JsonArray> Cache=new ConditionalWeakTable<LightJson.JsonArray,JsonArray>();
        internal LightJson.JsonArray Native { get; }
        internal static JsonArray Wrap(LightJson.JsonArray native)=>native==null ? null : Cache.GetValue(native,value=>new JsonArray(value));
        private JsonArray(LightJson.JsonArray native) { Native=native; }
        public JsonArray() { Native=new LightJson.JsonArray(); Cache.Add(Native,this); }
        public JsonArray(params JsonValue[] values) { Native=new LightJson.JsonArray(values?.Select(value=>value.Native).ToArray()); Cache.Add(Native,this); }
        public int Count=>Native.Count;
        public JsonValue this[int index] { get=>new JsonValue(Native[index]); set=>Native[index]=value.Native; }
        public JsonArray Add(JsonValue value) { Native.Add(value.Native); return this; }
        public JsonArray AddIfNotNull(JsonValue value) { Native.AddIfNotNull(value.Native); return this; }
        public JsonArray Insert(int index,JsonValue value) { Native.Insert(index,value.Native); return this; }
        public JsonArray InsertIfNotNull(int index,JsonValue value) { Native.InsertIfNotNull(index,value.Native); return this; }
        public JsonArray Remove(int index) { Native.Remove(index); return this; }
        public JsonArray Clear() { Native.Clear(); return this; }
        public bool Contains(JsonValue value)=>Native.Contains(value.Native);
        public int IndexOf(JsonValue value)=>Native.IndexOf(value.Native);
        public IEnumerator<JsonValue> GetEnumerator() { foreach(var value in Native) yield return new JsonValue(value); }
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        public override string ToString()=>Native.ToString();
        public string ToString(bool pretty)=>Native.ToString(pretty);
    }
}
