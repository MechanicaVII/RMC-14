using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.Sentry;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.ARES.CoreSecurity;

public sealed class ARESCoreSecuritySystem : EntitySystem
{
    [Dependency] private readonly ARESCoreSystem _aresCore = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedSentryTargetingSystem _sentryTargeting = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId<ARESLogTypeComponent> SecurityLog = "ARESTabARESLogs";

    public static readonly Dictionary<string, HashSet<string>> CoreSentryFactionPresets = new()
    {
        ["UNMC Only"] = ["UNMC"],
        ["WeYa Only"] = ["WeYa"],
        ["UNMC & WeYa"] = ["UNMC", "WeYa"],
        ["Hostile To All"] = [],
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<ARESCoreSentryComponent, ComponentStartup>(OnCoreSentryStartup);
        SubscribeLocalEvent<ARESCoreSentryComponent, EntityTerminatingEvent>(OnCoreSentryRemoved);
    }

    private void OnCoreSentryStartup(Entity<ARESCoreSentryComponent> ent, ref ComponentStartup args)
    {
        if (!_aresCore.TryGetARES(ent.Owner, out var ares) || ares == null)
            return;

        ent.Comp.LinkedCore = ares.Value.Owner;
        ares.Value.Comp.CoreSentries.Add(ent.Owner);
        Dirty(ent);
    }

    private void OnCoreSentryRemoved(Entity<ARESCoreSentryComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.LinkedCore is not { } core || !TryComp(core, out ARESCoreComponent? coreComp))
            return;

        coreComp.CoreSentries.Remove(ent.Owner);
    }

    public void SetLockdown(Entity<ARESCoreComponent> core, bool active, string requestedBy)
    {
        if (core.Comp.LockdownActive == active)
            return;

        core.Comp.LockdownActive = active;
        Dirty(core);

        var doors = EntityQueryEnumerator<RMCAresLockdownDoorComponent, DoorBoltComponent>();
        while (doors.MoveNext(out var doorUid, out _, out var bolt))
        {
            if (_transform.GetMap(doorUid) != _transform.GetMap(core.Owner))
                continue;

            _door.SetBoltsDown((doorUid, bolt), active);
        }

        var msg = active
            ? Loc.GetString("rmc-ares-core-security-lockdown-engaged-log", ("user", requestedBy))
            : Loc.GetString("rmc-ares-core-security-lockdown-lifted-log", ("user", requestedBy));
        _aresCore.CreateARESLog(core.Owner, SecurityLog, msg);
    }

    public bool SetCoreSentryFaction(Entity<ARESCoreComponent> core, string preset, string requestedBy)
    {
        if (!CoreSentryFactionPresets.TryGetValue(preset, out var factions))
            return false;

        foreach (var sentryUid in core.Comp.CoreSentries)
        {
            if (!TryComp(sentryUid, out SentryTargetingComponent? targeting))
                continue;

            _sentryTargeting.SetFriendlyFactions((sentryUid, targeting), factions);
        }

        var msg = Loc.GetString("rmc-ares-core-security-iff-updated-log", ("preset", preset), ("user", requestedBy));
        _aresCore.CreateARESLog(core.Owner, SecurityLog, msg);
        return true;
    }
}
