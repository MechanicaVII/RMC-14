using Content.Shared._RMC14.ARES.Logs;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.ARES.Chat;

public sealed class ARESChatSystem : EntitySystem
{
    [Dependency] private readonly ARESCoreSystem _aresCore = default!;
    [Dependency] private readonly SharedGameTicker _ticker = default!;

    private static readonly EntProtoId<ARESLogTypeComponent> ChatLog = "ARESTabARESLogs";
    private const int MaxMessagesPerUser = 100;

    public void SendMessage(Entity<ARESCoreComponent> core, string sender, string message)
    {
        var record = new ARESChatMessageRecord(sender, message, _ticker.RoundDuration().ToString(@"hh\:mm\:ss"));
        var conversation = core.Comp.Conversations.GetOrNew(sender);
        conversation.Add(record);

        if (conversation.Count > MaxMessagesPerUser)
            conversation.RemoveAt(0);

        _aresCore.CreateARESLog(core.Owner, ChatLog, Loc.GetString("rmc-ares-chat-message-log", ("user", sender), ("message", message)));
    }

    public List<ARESChatMessageRecord> GetConversation(Entity<ARESCoreComponent> core, string user)
    {
        return core.Comp.Conversations.TryGetValue(user, out var conversation) ? conversation : [];
    }

    public void ClearConversation(Entity<ARESCoreComponent> core, string user)
    {
        core.Comp.Conversations.Remove(user);
    }
}
