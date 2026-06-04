using System.Numerics;
using Content.Shared.CCVar;
using Content.Client.Hands.Systems;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.CombatMode;

public sealed class AmmoCounterOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly HandsSystem _hands;
    private readonly CombatModeSystem _combatMode;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public AmmoCounterOverlay()
    {
        IoCManager.InjectDependencies(this);
        _hands = _entity.System<HandsSystem>();
        _combatMode = _entity.System<CombatModeSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_combatMode.IsInCombatMode())
            return;

        var activeItem = _hands.GetActiveHandEntity();
        if (activeItem == null)
            return;

        if (!_entity.HasComponent<GunComponent>(activeItem.Value))
            return;

        var ammoCountEvent = new GetAmmoCountEvent();
        _entity.EventBus.RaiseLocalEvent(activeItem.Value, ref ammoCountEvent);

        if (ammoCountEvent.Capacity == 0)
            return;

        var ammoText = $"{ammoCountEvent.Count}/{ammoCountEvent.Capacity}";

        var handle = args.ScreenHandle;
        var font = new VectorFont(
            _resource.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"),
            12
        );

        var position = _cfg.GetCVar(CCVars.CombatModeAmmoCounterPosition);
        Vector2 pos;

        if (position == 1)
        {
            // Над слотом активной руки
            var hotbar = _ui.GetActiveUIWidgetOrNull<HotbarGui>();
            if (hotbar?.HandContainer != null)
            {
                var handPos = hotbar.HandContainer.GlobalPixelPosition;
                var handSize = hotbar.HandContainer.PixelSize;
                pos = new Vector2(
                    handPos.X + handSize.X / 2f - 16f,
                    handPos.Y - 20f
                );
            }
            else
            {
                // Fallback — внизу по центру
                pos = new Vector2(args.ViewportBounds.Right / 2f - 16f, args.ViewportBounds.Bottom - 80f);
            }
        }
        else
        {
            // У курсора (по умолчанию)
            var mousePos = _input.MouseScreenPosition.Position;
            pos = new Vector2(mousePos.X + 16f, mousePos.Y - 24f);
        }

        handle.DrawString(font, pos, ammoText, Color.White);
    }
}