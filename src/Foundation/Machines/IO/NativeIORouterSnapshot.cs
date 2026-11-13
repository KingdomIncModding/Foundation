using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    /// <summary>Native logistical machine routing state; values are read from actual game fields.</summary>
    public sealed class NativeIORouterSnapshot
    {
        public string MachineGuid { get; }
        public string NativeType { get; }
        public int InputCount { get; }
        public int OutputCount { get; }
        public int? CurrentInputIndex { get; }
        public int? CurrentOutputIndex { get; }
        public string LastItemTypeId { get; }
        /// <summary>Four is the observed in-game UI expansion limit, not a transfer rate.</summary>
        public int NativeUiPortLimit { get; }

        internal NativeIORouterSnapshot(FlowElement machine, int? currentInput, int? currentOutput, string lastType, int nativeUiPortLimit)
        {
            MachineGuid = machine.Guid;
            NativeType = machine.ElementType;
            InputCount = machine.Inputs.Count;
            OutputCount = machine.Outputs.Count;
            CurrentInputIndex = currentInput;
            CurrentOutputIndex = currentOutput;
            LastItemTypeId = lastType;
            NativeUiPortLimit = nativeUiPortLimit;
        }
    }
}
