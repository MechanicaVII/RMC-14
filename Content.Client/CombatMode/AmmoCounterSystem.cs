using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.CombatMode;

public sealed class AmmoCounterSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCVars.CombatModeAmmoCounterShow, OnShowAmmoCounterChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<AmmoCounterOverlay>();
    }

    private void OnShowAmmoCounterChanged(bool show)
    {
        if (show)
        {
            if (!_overlay.HasOverlay<AmmoCounterOverlay>())
                _overlay.AddOverlay(new AmmoCounterOverlay());
        }
        else
        {
            _overlay.RemoveOverlay<AmmoCounterOverlay>();
        }
    }
}