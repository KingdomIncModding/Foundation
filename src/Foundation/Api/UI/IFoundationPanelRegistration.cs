using System;
using System.Collections.Generic;
using Assets.Source.Flow;

namespace KingdomFoundation.Api
{
    public interface IFoundationPanelRegistration : IDisposable
    {
        string ModId { get; }

        string Id { get; }

        bool IsRegistered { get; }

        FoundationPanelDefinition Definition { get; }
    }
}
