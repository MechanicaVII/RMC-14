using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ARES.CoreSecurity;

[Serializable, NetSerializable]
public sealed class RMCARESRequestLockdown(bool active) : BoundUserInterfaceMessage
{
    public readonly bool Active = active;
}

[Serializable, NetSerializable]
public sealed class RMCARESRequestCoreSentryFaction(string preset) : BoundUserInterfaceMessage
{
    public readonly string Preset = preset;
}

[Serializable, NetSerializable]
public sealed record ARESLockdownConfirmEvent(bool Active);

[Serializable, NetSerializable]
public sealed record ARESCoreSentryFactionConfirmEvent(string Preset);
