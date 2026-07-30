using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ARES.CoreSecurity;

/// <summary>
///     Marks a door as bolted/unbolted by the ARES Core Security lockdown toggle.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCAresLockdownDoorComponent : Component;
