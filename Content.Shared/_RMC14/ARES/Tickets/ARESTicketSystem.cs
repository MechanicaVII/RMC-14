using System.Linq;
using Content.Shared._RMC14.ARES.Logs;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.ARES.Tickets;

public sealed class ARESTicketSystem : EntitySystem
{
    [Dependency] private readonly ARESCoreSystem _aresCore = default!;

    private static readonly EntProtoId<ARESLogTypeComponent> TicketLog = "ARESTabARESLogs";

    public ARESTicketRecord SubmitTicket(Entity<ARESCoreComponent> core, ARESTicketType type, string requester, string description)
    {
        var ticket = new ARESTicketRecord(core.Comp.NextTicketId++, type, requester, description);
        core.Comp.Tickets.Add(ticket);
        Dirty(core);

        _aresCore.CreateARESLog(core.Owner, TicketLog,
            Loc.GetString("rmc-ares-ticket-submitted-log", ("type", type), ("user", requester), ("description", description)));

        return ticket;
    }

    public List<ARESTicketRecord> GetTickets(Entity<ARESCoreComponent> core, ARESTicketType type)
    {
        return core.Comp.Tickets.Where(t => t.Type == type).ToList();
    }

    public bool TryClaimTicket(Entity<ARESCoreComponent> core, int id, string claimant)
    {
        var ticket = core.Comp.Tickets.FirstOrDefault(t => t.Id == id);
        if (ticket == null || ticket.Status != ARESTicketStatus.Open)
            return false;

        ticket.Status = ARESTicketStatus.Claimed;
        ticket.Claimant = claimant;
        Dirty(core);

        _aresCore.CreateARESLog(core.Owner, TicketLog,
            Loc.GetString("rmc-ares-ticket-claimed-log", ("id", id), ("user", claimant)));

        return true;
    }

    public bool TryResolveTicket(Entity<ARESCoreComponent> core, int id, bool approve, string resolver)
    {
        var ticket = core.Comp.Tickets.FirstOrDefault(t => t.Id == id);
        if (ticket == null || ticket.Status is ARESTicketStatus.Approved or ARESTicketStatus.Rejected or ARESTicketStatus.Cancelled)
            return false;

        ticket.Status = approve ? ARESTicketStatus.Approved : ARESTicketStatus.Rejected;
        Dirty(core);

        _aresCore.CreateARESLog(core.Owner, TicketLog,
            Loc.GetString(approve ? "rmc-ares-ticket-approved-log" : "rmc-ares-ticket-rejected-log", ("id", id), ("user", resolver)));

        return true;
    }

    public bool TryCancelTicket(Entity<ARESCoreComponent> core, int id, string requester)
    {
        var ticket = core.Comp.Tickets.FirstOrDefault(t => t.Id == id);
        if (ticket == null || ticket.Requester != requester || ticket.Status is ARESTicketStatus.Approved or ARESTicketStatus.Rejected or ARESTicketStatus.Cancelled)
            return false;

        ticket.Status = ARESTicketStatus.Cancelled;
        Dirty(core);

        _aresCore.CreateARESLog(core.Owner, TicketLog,
            Loc.GetString("rmc-ares-ticket-cancelled-log", ("id", id), ("user", requester)));

        return true;
    }
}
