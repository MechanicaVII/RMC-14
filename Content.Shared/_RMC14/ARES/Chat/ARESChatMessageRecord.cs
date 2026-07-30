using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ARES.Chat;

[Serializable, NetSerializable]
public sealed class ARESChatMessageRecord(string sender, string message, string time)
{
    public readonly string Sender = sender;
    public readonly string Message = message;
    public readonly string Time = time;
}
