using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ARES.Emergency;

[Serializable, NetSerializable]
public sealed class RMCARESRequestGeneralQuarters : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCARESRequestEvacuation : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed record ARESGeneralQuartersConfirmEvent;

[Serializable, NetSerializable]
public sealed record ARESEvacuationConfirmEvent;
