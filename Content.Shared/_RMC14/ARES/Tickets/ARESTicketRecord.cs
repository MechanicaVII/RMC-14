using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ARES.Tickets;

[Serializable, NetSerializable]
public enum ARESTicketType : byte
{
    Access,
    Maintenance,
}

[Serializable, NetSerializable]
public enum ARESTicketStatus : byte
{
    Open,
    Claimed,
    Approved,
    Rejected,
    Cancelled,
}

[Serializable, NetSerializable]
public sealed class ARESTicketRecord(int id, ARESTicketType type, string requester, string description)
{
    public readonly int Id = id;
    public readonly ARESTicketType Type = type;
    public readonly string Requester = requester;
    public readonly string Description = description;
    public ARESTicketStatus Status = ARESTicketStatus.Open;
    public string? Claimant;
}
