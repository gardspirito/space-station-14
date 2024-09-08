using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Item;
using Robust.Shared.Timing;

namespace Content.Server.Teleportation;

public sealed class TeleportItemsAroundSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
    }

    public void Update(float frameTime) {
        var query = EntityQueryEnumerator<TransformComponent, TeleportItemsAroundComponent>();
        while (query.MoveNext(out var uid, out var xform, out var teleport))
        {
            if (teleport.TeleportAt > _timing.CurTime)
                continue;
            var teleportWeight = teleport.Weight;
            if (teleportWeight <= 0)
                continue;
            var destination = _transform.GetMapCoordinates(teleport.Target);
            foreach (var nearUid in _lookup.GetEntitiesInRange((uid, xform), 1f, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (teleportWeight <= 0)
                    break;
                if (nearUid == uid || nearUid == teleport.Target)
                    continue;
                if (TryComp(nearUid, out ItemComponent? nearItem))
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

    public void QueueTeleport(EntityUid entity, float range, int weight, TimeSpan delay, EntityUid target)
    {
        EntityManager.AddComponent<TeleportItemsAroundComponent>(entity, new TeleportAroundComponent(range, weight, _timing.CurTime+delay, target));
    }
}
