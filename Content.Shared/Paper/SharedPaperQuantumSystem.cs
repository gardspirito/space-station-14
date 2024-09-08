using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

public abstract class SharedPaperQuantumSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperQuantumComponent, GetVerbsEvent<Verb>>(AddSplitVerb);
        SubscribeLocalEvent<PaperQuantumComponent, PaperQuantumSplitDoAfter>(OnSplit);
        SubscribeLocalEvent<PaperQuantumComponent, StampedEvent>(OnStamped);
    }

    private void AddSplitVerb(EntityUid uid, PaperQuantumComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract || component.Entangled is not null)
            return;

        args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString(component.SplitVerb),
                Act = () => TryStartSplit((uid, component), args.User)
            });
    }

    private bool TryStartSplit(Entity<PaperQuantumComponent> entity, EntityUid user)
    {
        if (entity.Comp.Entangled is not null)
            return false;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            entity.Comp.SplitDuration,
            new PaperQuantumSplitDoAfter(),
            eventTarget: entity,
            target: user,
            used: entity)
        {
            NeedHand = true,
            BreakOnDamage = true,
            DistanceThreshold = 1,
            MovementThreshold = 0.01f,
            BreakOnHandChange = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return false;
        // message
        return true;
    }

    protected void DisentangleOne(Entity<PaperQuantumComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.Entangled = null;
        EntityManager.RemoveComponentDeferred<PaperQuantumComponent>(entity);
    }
    
    // private DisentangleBoth(Entity<PaperQuantumComponent> entity)
    // {
    //     if (TryGetEntity(entity.Comp.Entangled, out var entangled) && TryComp(entangled, out PaperQuantumComponent? entangledComp))
    //         DisentangleOne((entangled, entangledComp));
    //     DisentangleOne(entity);
    // }

    private void OnSplit(Entity<PaperQuantumComponent> entity, ref PaperQuantumSplitDoAfter args)
    {
        if (!_net.IsServer || args.Cancelled)
            return;

        var otherUid = EntityManager.Spawn(entity.Comp.QuantumPaperProto);
        var otherComp = EntityManager.GetComponent<PaperQuantumComponent>(otherUid);

        if (TryComp(entity.Owner, out PaperComponent? paperComp))
        {
            _paper.Fill(otherUid, paperComp.Content, paperComp.StampState, paperComp.StampedBy, paperComp.EditingDisabled);
        }

        EntanglePaper(entity, entity.Comp.EntangledName1, otherUid); // rename
        EntanglePaper((otherUid, otherComp), entity.Comp.EntangledName2, entity.Owner);

        _handsSystem.PickupOrDrop(args.User, otherUid);
    }

    private void EntanglePaper(Entity<PaperQuantumComponent> entity, string locName, EntityUid otherUid)
    {
        // entity.Comp.IsEntangled = true;
        // entity.Comp.Entangled = otherUid;
        entity.Comp.Entangled = GetNetEntity(otherUid);
        Dirty(entity.Owner, entity.Comp);

        if (TryComp(entity.Owner, out MetaDataComponent? metaComp))
        {
            _meta.SetEntityName(entity.Owner, Loc.GetString(locName), metaComp);
            _meta.SetEntityDescription(entity.Owner, Loc.GetString(entity.Comp.EntangledDesc), metaComp);
        }
        if (TryComp(entity.Owner, out PaperComponent? paperComp))
        {
            // paperComp.EditingDisabled = true;
            Dirty(entity.Owner, paperComp);
        }
    }

    private void OnStamped(Entity<PaperQuantumComponent> entity, ref StampedEvent args)
    {
        if (!TryGetEntity(entity.Comp.Entangled, out var entangled))
            return;
        _paper.TryStamp(entangled.Value, args.StampInfo, args.SpriteStampState);

        if (!_net.IsServer)
            return;
        var light = Spawn(entity.Comp.BluespaceStampEffectProto, entangled.Value.ToCoordinates());
        _light.SetColor(light, args.StampInfo.StampedColor);
    }

    /// <summary>
    ///     Check if this entity is entangled with some other.
    /// </summary>
    public bool IsEntangled(Entity<PaperQuantumComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;
        return entity.Comp.Entangled is not null;
    }
}

[Serializable, NetSerializable]
public sealed partial class PaperQuantumSplitDoAfter : SimpleDoAfterEvent
{
}
