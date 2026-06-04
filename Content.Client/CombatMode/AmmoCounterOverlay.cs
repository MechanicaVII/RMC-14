using System.Linq;
using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.CombatMode;

public sealed class AmmoCounterOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IResourceCache _resource = default!;

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

        BallisticAmmoProviderComponent? ballistic = null;

        // Случай 1: оружие с магазином (M54C, M41A и т.д.)
        if (_entity.TryGetComponent<ContainerManagerComponent>(activeItem.Value, out var containers)
            && containers.Containers.TryGetValue("gun_magazine", out var magazineContainer))
        {
            var magazine = magazineContainer.ContainedEntities.FirstOrDefault();
            if (magazine != default)
                _entity.TryGetComponent(magazine, out ballistic);
        }

        // Случай 2: патроны прямо на оружии (XM88, дробовики и т.д.)
        if (ballistic == null)
            _entity.TryGetComponent(activeItem.Value, out ballistic);

        if (ballistic == null)
            return;

        var ammoText = $"{ballistic.Count}/{ballistic.Capacity}";

        var handle = args.ScreenHandle;
        var font = new VectorFont(
            _resource.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"),
            12
        );

        var mousePos = _input.MouseScreenPosition.Position;
        var pos = new Vector2(mousePos.X + 16f, mousePos.Y - 24f);

        // Только текст, без фона
        handle.DrawString(font, pos, ammoText, Color.White);
    }
}