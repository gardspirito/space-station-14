using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Item;
using Content.Shared.Paper;
using Content.Server.Atmos.Components;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Server.Paper;

public sealed class PaperQuantumSystem : SharedPaperQuantumSystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperQuantumComponent, IgnitedEvent>(OnIgnited);
        SubscribeLocalEvent<PaperQuantumComponent, PaperQuantumDoAfterEvent>(OnTeleport);
    }

    private void OnIgnited(Entity<PaperQuantumComponent> entity, ref IgnitedEvent args)
    {
        // Disentangle
        var entangledNet = entity.Comp.Entangled;
        if (!TryGetEntity(entangledNet, out var entangled))
            return;
        DisentangleOne((entity.Owner, entity.Comp));
        DisentangleOne(entangled.Value);

        if (TryComp(entangled.Value, out FlammableComponent? entangledFlammable))
            entangledFlammable.Damage = new();
        _explosion.TriggerExplosive(entangled.Value);

        EnsureComp<DoAfterComponent>(entity);
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, entity, entity.Comp.TeleportDelay, new PaperQuantumDoAfterEvent(), entangled));
    }

    private void OnTeleport(EntityUid entity, PaperQuantumComponent comp, DoAfterEvent args)
    {
        if (args.Target is null)
            return;
        var entangled = args.Target.Value;
        var teleportWeight = comp.TeleportWeight;
        if (teleportWeight <= 0)
            return;
        var destination = _transform.GetMapCoordinates(entangled);
        foreach (var nearEnt in _lookup.GetEntitiesInRange(entity.ToCoordinates(), 1f, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (teleportWeight <= 0)
                break;
            if (nearEnt == entity || nearEnt == entangled)
                continue;
            if (TryComp(nearEnt, out ItemComponent? nearItem))
            {
                var weight = _item.GetItemSizeWeight(nearItem.Size);
                if (weight <= teleportWeight)
                {
                    teleportWeight -= weight;
                    _transform.SetMapCoordinates(nearEnt, destination);
                }
            } else
            {
                teleportWeight -= 1;
                _damage.TryChangeDamage(nearEnt, comp.Damage);
            }
        }
    }
}

[Serializable]
public sealed partial class PaperQuantumDoAfterEvent : SimpleDoAfterEvent
{
}
