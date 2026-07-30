using System.Collections.Immutable;
using System.Linq;
using Content.Shared._RMC14.Access;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.ARES.CoreSecurity;
using Content.Shared._RMC14.ARES.Emergency;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.ARES.Tabs;
using Content.Shared._RMC14.ARES.Tickets;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Prototypes;
using Content.Shared.UserInterface;
using Microsoft.VisualBasic;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._RMC14.ARES.ExternalTerminals;

public sealed class ARESExternalTerminalSystem : EntitySystem
{
    [Dependency] private readonly RMCAlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly ARESCoreSystem _core = default!;
    [Dependency] private readonly ARESCoreSecuritySystem _coreSecurity = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedEvacuationSystem _evacuation = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly GunIFFSystem _iffSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ARESTicketSystem _tickets = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId<ARESLogTypeComponent> CoreLog = "ARESTabARESLogs";
    private static readonly EntProtoId<ARESTabComponent> CoreSecurityTab = "ARESTabCoreSecurity";
    private static readonly EntProtoId<ARESTabComponent> EmergencyTab = "ARESTabEmergency";
    private static readonly EntProtoId<ARESTabComponent> TicketsTab = "ARESTabTickets";
    private static readonly int LogsShown = 12;
    private static readonly int TicketDescriptionLimit = 200;

    public HashSet<EntProtoId<ARESLogTypeComponent>> LogTypes { get; private set; } = [];
    public HashSet<EntProtoId<ARESTabComponent>> TabTypes { get; private set; } = [];

    public override void Initialize()
    {
        Subs.BuiEvents<ARESExternalTerminalComponent>(ARESExternalTerminalUIKey.Key,
            subs =>
            {
                subs.Event<RMCARESExternalLogin>(OnExternalLogin);
                subs.Event<RMCARESExternalLogout>(OnExternalLogout);
                subs.Event<RMCARESExternalShowLogs>(OnExternalShowLogs);
                subs.Event<RMCARESRequestLockdown>(OnRequestLockdown);
                subs.Event<RMCARESRequestCoreSentryFaction>(OnRequestCoreSentryFaction);
                subs.Event<RMCARESRequestGeneralQuarters>(OnRequestGeneralQuarters);
                subs.Event<RMCARESRequestEvacuation>(OnRequestEvacuation);
                subs.Event<RMCARESRequestSubmitTicket>(OnRequestSubmitTicket);
                subs.Event<RMCARESShowTickets>(OnShowTickets);
                subs.Event<RMCARESClaimTicket>(OnClaimTicket);
                subs.Event<RMCARESResolveTicket>(OnResolveTicket);
                subs.Event<RMCARESCancelTicket>(OnCancelTicket);
            });

        SubscribeLocalEvent<ARESExternalTerminalComponent, ARESLockdownConfirmEvent>(OnLockdownConfirm);
        SubscribeLocalEvent<ARESExternalTerminalComponent, ARESCoreSentryFactionConfirmEvent>(OnCoreSentryFactionConfirm);
        SubscribeLocalEvent<ARESExternalTerminalComponent, ARESGeneralQuartersConfirmEvent>(OnGeneralQuartersConfirm);
        SubscribeLocalEvent<ARESExternalTerminalComponent, ARESEvacuationConfirmEvent>(OnEvacuationConfirm);
        SubscribeLocalEvent<ARESExternalTerminalComponent, ARESSubmitTicketInputEvent>(OnSubmitTicketInput);

        SubscribeLocalEvent<ARESExternalTerminalComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ARESExternalTerminalComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        ReloadTabs();
    }

    private void OnExternalShowLogs(Entity<ARESExternalTerminalComponent> ent, ref RMCARESExternalShowLogs args)
    {
        ent.Comp.Logs.Clear();
        if (_net.IsClient)
            return;

        if (args.Type == null)
            return;

        if (!ent.Comp.ARESCore.HasValue ||
            !_core.PullARESLogs(ent.Comp.ARESCore.Value, args.Type, out var logs) || logs == null)
        {
            ent.Comp.LogsLength = 1;
            ent.Comp.Logs.Add("No logs to display");
            Dirty(ent);
            return;
        }

        ent.Comp.LogsLength = logs.Count;

        ent.Comp.Logs = logs.SkipLast(args.Index * LogsShown).TakeLast(LogsShown).Reverse().ToList();
        Dirty(ent);
    }

    private void OnRequestLockdown(Entity<ARESExternalTerminalComponent> ent, ref RMCARESRequestLockdown args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(CoreSecurityTab))
            return;

        var title = Loc.GetString("rmc-ares-core-security-lockdown-title");
        var message = args.Active
            ? Loc.GetString("rmc-ares-core-security-lockdown-engage-confirm")
            : Loc.GetString("rmc-ares-core-security-lockdown-lift-confirm");

        _dialog.OpenConfirmation(ent.Owner, args.Actor, title, message, new ARESLockdownConfirmEvent(args.Active));
    }

    private void OnLockdownConfirm(Entity<ARESExternalTerminalComponent> ent, ref ARESLockdownConfirmEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.ARESCore is not { } coreUid || !TryComp(coreUid, out ARESCoreComponent? core))
            return;

        _coreSecurity.SetLockdown((coreUid, core), args.Active, ent.Comp.LoggedInUser);
    }

    private void OnRequestCoreSentryFaction(Entity<ARESExternalTerminalComponent> ent, ref RMCARESRequestCoreSentryFaction args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(CoreSecurityTab))
            return;

        if (!ARESCoreSecuritySystem.CoreSentryFactionPresets.ContainsKey(args.Preset))
            return;

        var title = Loc.GetString("rmc-ares-core-security-iff-title");
        var message = Loc.GetString("rmc-ares-core-security-iff-confirm", ("preset", args.Preset));

        _dialog.OpenConfirmation(ent.Owner, args.Actor, title, message, new ARESCoreSentryFactionConfirmEvent(args.Preset));
    }

    private void OnCoreSentryFactionConfirm(Entity<ARESExternalTerminalComponent> ent, ref ARESCoreSentryFactionConfirmEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.ARESCore is not { } coreUid || !TryComp(coreUid, out ARESCoreComponent? core))
            return;

        _coreSecurity.SetCoreSentryFaction((coreUid, core), args.Preset, ent.Comp.LoggedInUser);
    }

    private void OnRequestGeneralQuarters(Entity<ARESExternalTerminalComponent> ent, ref RMCARESRequestGeneralQuarters args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(EmergencyTab))
            return;

        var title = Loc.GetString("rmc-ares-emergency-general-quarters-title");
        var message = Loc.GetString("rmc-ares-emergency-general-quarters-confirm");

        _dialog.OpenConfirmation(ent.Owner, args.Actor, title, message, new ARESGeneralQuartersConfirmEvent());
    }

    private void OnGeneralQuartersConfirm(Entity<ARESExternalTerminalComponent> ent, ref ARESGeneralQuartersConfirmEvent args)
    {
        if (_net.IsClient)
            return;

        _alertLevel.Set(RMCAlertLevels.Red, ent.Owner);
    }

    private void OnRequestEvacuation(Entity<ARESExternalTerminalComponent> ent, ref RMCARESRequestEvacuation args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(EmergencyTab))
            return;

        var title = Loc.GetString("rmc-ares-emergency-evacuation-title");
        var message = Loc.GetString("rmc-ares-emergency-evacuation-confirm");

        _dialog.OpenConfirmation(ent.Owner, args.Actor, title, message, new ARESEvacuationConfirmEvent());
    }

    private void OnEvacuationConfirm(Entity<ARESExternalTerminalComponent> ent, ref ARESEvacuationConfirmEvent args)
    {
        if (_net.IsClient)
            return;

        var map = _transform.GetMap(ent.Owner);
        _evacuation.ToggleEvacuation(null, null, map);
    }

    private void OnRequestSubmitTicket(Entity<ARESExternalTerminalComponent> ent, ref RMCARESRequestSubmitTicket args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(TicketsTab))
            return;

        var message = Loc.GetString("rmc-ares-ticket-submit-prompt", ("type", args.Type));
        _dialog.OpenInput(ent.Owner, args.Actor, message, new ARESSubmitTicketInputEvent(args.Type, ""), true, TicketDescriptionLimit);
    }

    private void OnSubmitTicketInput(Entity<ARESExternalTerminalComponent> ent, ref ARESSubmitTicketInputEvent args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(TicketsTab) || _net.IsClient)
            return;

        if (ent.Comp.ARESCore is not { } coreUid || !TryComp(coreUid, out ARESCoreComponent? core))
            return;

        if (string.IsNullOrWhiteSpace(args.Message))
            return;

        _tickets.SubmitTicket((coreUid, core), args.Type, ent.Comp.LoggedInUser, args.Message);
        RefreshShownTickets(ent, (coreUid, core), args.Type);
    }

    private void OnShowTickets(Entity<ARESExternalTerminalComponent> ent, ref RMCARESShowTickets args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(TicketsTab) || _net.IsClient)
            return;

        if (ent.Comp.ARESCore is not { } coreUid || !TryComp(coreUid, out ARESCoreComponent? core))
            return;

        RefreshShownTickets(ent, (coreUid, core), args.Type);
    }

    private void OnClaimTicket(Entity<ARESExternalTerminalComponent> ent, ref RMCARESClaimTicket args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(TicketsTab) || _net.IsClient)
            return;

        if (ent.Comp.ARESCore is not { } coreUid || !TryComp(coreUid, out ARESCoreComponent? core))
            return;

        _tickets.TryClaimTicket((coreUid, core), args.Id, ent.Comp.LoggedInUser);
        RefreshShownTickets(ent, (coreUid, core), ent.Comp.ShownTicketType);
    }

    private void OnResolveTicket(Entity<ARESExternalTerminalComponent> ent, ref RMCARESResolveTicket args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(TicketsTab) || _net.IsClient)
            return;

        if (ent.Comp.ARESCore is not { } coreUid || !TryComp(coreUid, out ARESCoreComponent? core))
            return;

        _tickets.TryResolveTicket((coreUid, core), args.Id, args.Approve, ent.Comp.LoggedInUser);
        RefreshShownTickets(ent, (coreUid, core), ent.Comp.ShownTicketType);
    }

    private void OnCancelTicket(Entity<ARESExternalTerminalComponent> ent, ref RMCARESCancelTicket args)
    {
        if (!ent.Comp.LoggedIn || !ent.Comp.ShownTabs.Contains(TicketsTab) || _net.IsClient)
            return;

        if (ent.Comp.ARESCore is not { } coreUid || !TryComp(coreUid, out ARESCoreComponent? core))
            return;

        _tickets.TryCancelTicket((coreUid, core), args.Id, ent.Comp.LoggedInUser);
        RefreshShownTickets(ent, (coreUid, core), ent.Comp.ShownTicketType);
    }

    private void RefreshShownTickets(Entity<ARESExternalTerminalComponent> ent, Entity<ARESCoreComponent> core, ARESTicketType type)
    {
        ent.Comp.ShownTicketType = type;
        ent.Comp.ShownTickets = _tickets.GetTickets(core, type);
        Dirty(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadTabs();
    }

    private void ReloadTabs()
    {
        LogTypes = [];
        TabTypes = [];

        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.HasComponent<ARESLogTypeComponent>())
            {
                LogTypes.Add(entity.ID);
                continue;
            }

            if (entity.HasComponent<ARESTabComponent>())
            {
                TabTypes.Add(entity.ID);
                continue;
            }
        }
    }

    private void OnExternalLogout(Entity<ARESExternalTerminalComponent> ent, ref RMCARESExternalLogout args)
    {
        if (Prototype(ent) is not { } proto)
            return;

        proto.TryGetComponent<ARESExternalTerminalComponent>(out var comp, _componentFactory);
        var refComp = ent.Comp;
        _serialization.CopyTo(comp, ref refComp);

        Dirty(ent);
    }

    private void OnExternalLogin(Entity<ARESExternalTerminalComponent> ent, ref RMCARESExternalLogin args)
    {
        SetAres(ent);
        if (!_idCard.TryFindIdCard(args.Actor, out var idCard) || !TryComp<AccessComponent>(idCard, out var access) ||
            idCard.Comp.FullName == null || idCard.Comp._jobTitle == null || !_iffSystem.HasFaction(idCard, ent.Comp.Faction))
            return;

        _core.CreateARESLog(ent.Comp.Faction,
            CoreLog,
            $"{idCard.Comp.FullName}'s ID card was used to log into the ARES system.");

        ent.Comp.LoggedIn = true;
        ent.Comp.Accesses = access.Tags;
        ent.Comp.LoggedInUser = $"{idCard.Comp.FullName} ({idCard.Comp._jobTitle})";

        // logs are special...
        if (ent.Comp.ShowsLogs)
        {
            //This compares the stored ID card data and determines what log types you can view.
            foreach (var logType in LogTypes)
            {
                var logPermission = logType.Get(_prototypes, _componentFactory).Permissions;
                if (logPermission == null || logPermission.Count == 0)
                {
                    ent.Comp.ShownLogs.Add(logType);
                    continue;
                }

                foreach (var permission in ent.Comp.Accesses)
                {
                    if (!logPermission.Contains(permission))
                        continue;

                    ent.Comp.ShownLogs.Add(logType);
                    break;
                }
            }
        }

        //This compares the stored ID card data and determines what tabs/sections you can see.
        foreach (var tabType in TabTypes)
        {
            var tabPermission = tabType.Get(_prototypes, _componentFactory).Permissions;
            if (tabPermission.Count == 0)
            {
                ent.Comp.ShownTabs.Add(tabType);
                continue;
            }

            foreach (var permission in ent.Comp.Accesses)
            {
                if (!tabPermission.Contains(permission))
                    continue;

                ent.Comp.ShownTabs.Add(tabType);
                break;
            }
        }

        Dirty(ent);
    }

    private void OnBeforeActivatableUIOpen(Entity<ARESExternalTerminalComponent> ent,
        ref BeforeActivatableUIOpenEvent args)
    {
        SetAres(ent);
    }

    private void OnMapInit(Entity<ARESExternalTerminalComponent> ent, ref MapInitEvent args)
    {
        SetAres(ent);
    }

    private void SetAres(Entity<ARESExternalTerminalComponent> ent)
    {
        if (ent.Comp.ARESCore == null)
        {
            _core.TryGetARES(ent.Comp.Faction, out var ares);
            if (ares != null)
            {
                ent.Comp.ARESCore = ares.Value;
                Dirty(ent);
            }
        }
    }
}
