using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    public sealed class NativeIOValidation
    {
        public bool Allowed { get; }
        public string Reason { get; }

        internal NativeIOValidation(bool allowed, string reason)
        {
            Allowed = allowed;
            Reason = reason;
        }

        internal static NativeIOValidation Yes() => new NativeIOValidation(true, "Native operation permitted by currently observable port/workspace state.");
        internal static NativeIOValidation No(string message) => new NativeIOValidation(false, message);
    }
}
