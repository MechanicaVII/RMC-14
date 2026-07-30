using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ARES.Chat;

[Serializable, NetSerializable]
public sealed class RMCARESRequestSendChatMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCARESShowChat : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCARESClearChat : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed record ARESChatMessageInputEvent(string Message) : DialogInputEvent(Message);
