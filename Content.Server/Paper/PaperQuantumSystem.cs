using Content.Shared.Paper;
using Content.Server.Atmos.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Teleportation;
using Robust.Shared.Serialization;

namespace Content.Server.Paper;

public sealed class PaperQuantumSystem : SharedPaperQuantumSystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly TeleportItemsAroundSystem _teleport = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperQuantumComponent, IgnitedEvent>(OnIgnited);
    }

    private void OnIgnited(Entity<PaperQuantumComponent> entity, ref IgnitedEvent args)
    {
        // Disentangle
        var entangledNet = entity.Comp.Entangled;
        if (!TryGetEntity(entangledNet, out var entangled))
            return;
        DisentangleOne((entity.Owner, entity.Comp));
        DisentangleOne(entangled.Value);

        // Queue explosion
        if (TryComp(entangled.Value, out FlammableComponent? entangledFlammable))
            entangledFlammable.Damage = new();
        _explosion.TriggerExplosive(entangled.Value);

        // Queue teleport
        _teleport.QueueTeleport(entity, entity.Comp.TeleportAroundRange, entity.Comp.TeleportWeight, entity.Comp.TeleportDelay);
    }

    // private void OnTeleport(EntityUid entity, PaperQuantumComponent comp, DoAfterEvent args)
    // {
    //     if (args.Target is null)
    //         return;
    //     var entangled = args.Target.Value;
    //     var teleportWeight = comp.TeleportWeight;
    //     if (teleportWeight <= 0)
    //         return;
    //     var destination = _transform.GetMapCoordinates(entangled);
    //     foreach (var nearEnt in _lookup.GetEntitiesInRange(entity.ToCoordinates(), 1f, LookupFlags.Dynamic | LookupFlags.Sundries))
    //     {
    //         if (teleportWeight <= 0)
    //             break;
    //         if (nearEnt == entity || nearEnt == entangled)
    //             continue;
    //         if (TryComp(nearEnt, out ItemComponent? nearItem))
    //         {
    //             var weight = _item.GetItemSizeWeight(nearItem.Size);
    //             if (weight <= teleportWeight)
    //             {
    //                 teleportWeight -= weight;
    //                 _transform.SetMapCoordinates(nearEnt, destination);
    //             }
    //         } else
    //         {
    //             teleportWeight -= 1;
    //             _damage.TryChangeDamage(nearEnt, comp.Damage);
    //         }
    //     }
    // }
}
