using System;

namespace KingdomFoundation.Json
{
    /// <summary>Foundation-owned JSON value. Native serialization details stay inside Foundation.</summary>
    public readonly struct JsonValue
    {
        internal LightJson.JsonValue Native { get; }
        internal JsonValue(LightJson.JsonValue native) { Native=native; }
        public static readonly JsonValue Null = new JsonValue(LightJson.JsonValue.Null);
        public JsonValueType Type => (JsonValueType)(byte)Native.Type;
        public bool IsNull => Native.IsNull;
        public bool IsBoolean => Native.IsBoolean;
        public bool IsInteger => Native.IsInteger;
        public bool IsNumber => Native.IsNumber;
        public bool IsString => Native.IsString;
        public bool IsJsonObject => Native.IsJsonObject;
        public bool IsJsonArray => Native.IsJsonArray;
        public bool IsDateTime => Native.IsDateTime;
        public bool AsBoolean => Native.AsBoolean;
        public int AsInteger => Native.AsInteger;
        public double AsNumber => Native.AsNumber;
        public string AsString => Native.AsString;
        public JsonObject AsJsonObject => JsonObject.Wrap(Native.AsJsonObject);
        public JsonArray AsJsonArray => JsonArray.Wrap(Native.AsJsonArray);
        public DateTime? AsDateTime => Native.AsDateTime;
        public object AsObject => IsJsonObject ? (object)AsJsonObject : IsJsonArray ? (object)AsJsonArray : Native.AsObject;
        public JsonValue this[string key] { get=>new JsonValue(Native[key]); set=>Native.AsJsonObject[key]=value.Native; }
        public JsonValue this[int index] { get=>new JsonValue(Native[index]); set=>Native.AsJsonArray[index]=value.Native; }
        public JsonValue(bool? value) { Native=new LightJson.JsonValue(value); }
        public JsonValue(double? value) { Native=new LightJson.JsonValue(value); }
        public JsonValue(string value) { Native=new LightJson.JsonValue(value); }
        public JsonValue(JsonObject value) { Native=new LightJson.JsonValue(value?.Native); }
        public JsonValue(JsonArray value) { Native=new LightJson.JsonValue(value?.Native); }
        public static implicit operator JsonValue(bool? value)=>new JsonValue(value);
        public static implicit operator JsonValue(double? value)=>new JsonValue(value);
        public static implicit operator JsonValue(string value)=>new JsonValue(value);
        public static implicit operator JsonValue(JsonObject value)=>new JsonValue(value);
        public static implicit operator JsonValue(JsonArray value)=>new JsonValue(value);
        public static implicit operator JsonValue(DateTime? value)=>new JsonValue((LightJson.JsonValue)value);
        public static implicit operator int(JsonValue value)=>(int)value.Native;
        public static implicit operator int?(JsonValue value)=>(int?)value.Native;
        public static implicit operator bool(JsonValue value)=>(bool)value.Native;
        public static implicit operator bool?(JsonValue value)=>(bool?)value.Native;
        public static implicit operator double(JsonValue value)=>(double)value.Native;
        public static implicit operator double?(JsonValue value)=>(double?)value.Native;
        public static implicit operator string(JsonValue value)=>(string)value.Native;
        public static implicit operator JsonObject(JsonValue value)=>JsonObject.Wrap((LightJson.JsonObject)value.Native);
        public static implicit operator JsonArray(JsonValue value)=>JsonArray.Wrap((LightJson.JsonArray)value.Native);
        public static implicit operator DateTime(JsonValue value)=>(DateTime)value.Native;
        public static implicit operator DateTime?(JsonValue value)=>(DateTime?)value.Native;
        public static bool operator ==(JsonValue left,JsonValue right)=>left.Native==right.Native;
        public static bool operator !=(JsonValue left,JsonValue right)=>left.Native!=right.Native;
        public static JsonValue Parse(string text)=>new JsonValue(LightJson.JsonValue.Parse(text));
        public override bool Equals(object obj)=>obj is JsonValue other && Native.Equals(other.Native);
        public override int GetHashCode()=>Native.GetHashCode();
        public override string ToString()=>Native.ToString();
        public string ToString(bool pretty)=>Native.ToString(pretty);
    }
}
