using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ARES.Tickets;

[Serializable, NetSerializable]
public sealed class RMCARESRequestSubmitTicket(ARESTicketType type) : BoundUserInterfaceMessage
{
    public readonly ARESTicketType Type = type;
}

[Serializable, NetSerializable]
public sealed record ARESSubmitTicketInputEvent(ARESTicketType Type, string Message) : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed class RMCARESShowTickets(ARESTicketType type) : BoundUserInterfaceMessage
{
    public readonly ARESTicketType Type = type;
}

[Serializable, NetSerializable]
public sealed class RMCARESClaimTicket(int id) : BoundUserInterfaceMessage
{
    public readonly int Id = id;
}

[Serializable, NetSerializable]
public sealed class RMCARESResolveTicket(int id, bool approve) : BoundUserInterfaceMessage
{
    public readonly int Id = id;
    public readonly bool Approve = approve;
}

[Serializable, NetSerializable]
public sealed class RMCARESCancelTicket(int id) : BoundUserInterfaceMessage
{
    public readonly int Id = id;
}
