using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ARES.CoreSecurity;

/// <summary>
///     Marks a sentry as linked to an ARES core, letting the Core Security tab set its
///     friendly-faction preset directly. Independent of the Sentry Laptop's per-turret control.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ARESCoreSecuritySystem))]
public sealed partial class ARESCoreSentryComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedCore;
}
