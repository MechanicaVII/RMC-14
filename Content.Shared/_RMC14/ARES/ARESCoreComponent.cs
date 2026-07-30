using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.ARES;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ARESCoreComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ARESCoreName = "Hermes";

    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent> Faction = "FactionMarine";

    [DataField, AutoNetworkedField]
    public bool Active = true;

    [DataField, AutoNetworkedField]
    public int MaxLogs = 5000;

    [DataField, AutoNetworkedField]
    public bool LockdownActive;

    // Client Empty.
    [DataField, Access(typeof(ARESCoreSystem), Other = AccessPermissions.None)]
    public Dictionary<EntProtoId<ARESLogTypeComponent>, List<string>> Logs = new();

    // Client Empty.
    [DataField, Access(typeof(CoreSecurity.ARESCoreSecuritySystem), Other = AccessPermissions.None)]
    public HashSet<EntityUid> CoreSentries = new();
}
