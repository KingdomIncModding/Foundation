using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    public sealed class MachineOperationResult
    {
        public bool Succeeded { get; }
        public string Message { get; }

        private MachineOperationResult(bool success, string message)
        {
            Succeeded = success;
            Message = message;
        }

        public static MachineOperationResult Ok(string message = "Native operation completed") => new MachineOperationResult(true, message);
        public static MachineOperationResult Reject(string message) => new MachineOperationResult(false, message);
        public override string ToString() => (Succeeded ? "OK: " : "REJECTED: ") + Message;
    }
}
