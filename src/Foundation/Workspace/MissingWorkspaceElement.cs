using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Player;
using LightJson;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Inert save-preserving replacement for a workspace element whose defining plugin
    /// is unavailable. Preserves identity, native port buffers, pipe index mapping and
    /// mod-specific JSON fields; never simulates the missing machine's behavior.
    /// </summary>
    public sealed class MissingWorkspaceElement : FlowElement
    {
        static readonly Dictionary<string, Vector2Int> missingTypes = new Dictionary<string, Vector2Int>(StringComparer.Ordinal);
        readonly string missingType;
        JsonObject original;
        public override string ElementType => missingType;
        public override string Label => "Missing mod: " + missingType;
        public override ElementCategory Category => ElementCategory.Utility;
        public override string IconName => "Icons_104";
        public override bool HasCraftButton => false;
        public override int UpgradeSlots => 0;
        public override bool IsPurchaseable(GamePlayer player) => false;
        // The original cost is unknown and must not generate a vanilla refund.
        public override CurrencyCollection GetRefund(int alreadyRefunded = 0) => new CurrencyCollection();
        public override void Update(float delta) { }

        public MissingWorkspaceElement(string typeId)
        {
            SignalPropertyDefinition.ValidateId(typeId, nameof(typeId));
            missingType = typeId;
            Size = new Vector2Int(256, 128);
            Description = "This node belongs to an unavailable mod. Reinstall that mod to restore functionality.";
            missingTypes[typeId] = Size;
        }
        internal static bool IsMissing(string id) => id != null && missingTypes.ContainsKey(id);
        internal static Vector2Int GetSize(string id) => missingTypes.TryGetValue(id, out var size) ? size : new Vector2Int(256, 128);
        /// <summary>
        /// Reconstruct only safe inert runtime metadata. Unknown upgrades, items and
        /// mod-specific fields are retained AS JSON and deliberately not deserialized.
        /// Native FlowElement.FromJson cannot handle missing third-party upgrade/item
        /// definitions, and would otherwise abort loading the entire kingdom.
        /// </summary>
        internal void RestoreRaw(JsonObject data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            original = JsonValue.Parse(data.ToString()).AsJsonObject;
            if (data["Guid"].IsString)
            {
                // Native FlowElement.Guid has a private setter; preserve the saved
                // instance identity so connected pipes can find their endpoints.
                var setter = typeof(FlowElement).GetProperty(nameof(Guid)).GetSetMethod(true);
                setter.Invoke(this, new object[] { data["Guid"].AsString });
            }
            if (data["Position"].IsJsonArray && data["Position"].AsJsonArray.Count >= 2)
                Position = new Vector2Int(data["Position"][0].AsInteger, data["Position"][1].AsInteger);
            if (data["Progress"].IsNumber) Progress = (float)data["Progress"].AsNumber;
            if (data["ProductivityProgress"].IsNumber) ProductivityProgress = (float)data["ProductivityProgress"].AsNumber;
            if (data["StackSize"].IsInteger) StackSize = data["StackSize"].AsInteger;
            if (data["Mirrored"].IsBoolean) Mirrored = data["Mirrored"].AsBoolean;

            Inputs.Clear(); Outputs.Clear();
            if (data["Inputs"].IsJsonArray)
                for (int i = 0; i < data["Inputs"].AsJsonArray.Count; i++)
                {
                    var port = new FlowInput(this, i);
                    if (data["Inputs"][i].IsJsonObject && data["Inputs"][i]["Position"].IsInteger)
                        port.Position = data["Inputs"][i]["Position"].AsInteger;
                    // Keep unavailable inventory raw in original JSON. Zero native
                    // capacity prevents active neighbours from pushing items into an
                    // inert placeholder that has no valid gameplay processing.
                    port.BufferSize = 0;
                    Inputs.Add(port);
                }
            if (data["Outputs"].IsJsonArray)
                for (int i = 0; i < data["Outputs"].AsJsonArray.Count; i++)
                {
                    var port = new FlowOutput(this, i);
                    if (data["Outputs"][i].IsJsonObject && data["Outputs"][i]["Position"].IsInteger)
                        port.Position = data["Outputs"][i]["Position"].AsInteger;
                    port.BufferSize = 0;
                    Outputs.Add(port);
                }
        }

        protected override void DataFromJson(JsonObject data, FlowDiagram parent)
        {
            RestoreRaw(data);
        }
        protected override void DataToJson(JsonObject data)
        {
            if (original == null) return;
            foreach (var pair in original)
            {
                switch (pair.Key)
                {
                    case "Guid": case "ElementType": case "Position": case "Inputs": case "Outputs":
                    case "Progress": case "ProductivityProgress": case "StackSize": case "Mirrored":
                        break;
                    default: data[pair.Key] = ModSaveRegistry.Clone(pair.Value); break;
                }
            }
        }
        internal void PreservePortFields(JsonObject output)
        {
            if (original == null || output == null) return;
            // Native serialization would write empty port buffers because missing
            // items intentionally remain opaque. Restore the entire original port
            // arrays byte-for-byte at the JSON-value level; input/output ordering,
            // indices, items, and custom fields must survive a mod reinstall.
            foreach (var key in new[] { "Inputs", "Outputs" })
                if (original[key].IsJsonArray)
                    output[key] = ModSaveRegistry.Clone(original[key]);
        }
    }
}
