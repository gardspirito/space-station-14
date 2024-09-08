using Content.Shared.Damage;
using Content.Shared.Explosion;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedPaperQuantumSystem))]
public sealed partial class PaperQuantumComponent : Component
{
    [DataField]
    public EntProtoId QuantumPaperProto = "PaperQuantum";

    [DataField]
    public string SplitVerb = "paper-quantum-split-verb";

    [DataField]
    public string EntangledName1 = "paper-quantum-entangled-name-1";

    [DataField]
    public string EntangledName2 = "paper-quantum-entangled-name-2";

    [DataField]
    public string EntangledDesc = "paper-quantum-entangled-desc";

    [DataField]
    public TimeSpan SplitDuration = TimeSpan.FromSeconds(5f);

    [DataField]
    public EntProtoId BluespaceStampEffectProto = "EffectFlashBluespaceMini";

    [DataField]
    public int TeleportWeight = 4;

    [DataField]
    public TimeSpan TeleportDelay = TimeSpan.FromSeconds(0.5f);

    [DataField]
    public DamageSpecifier Damage = default!;
    // /// <summary>
    // /// The explosion prototype to spawn
    // /// </summary>
    // [DataField]
    // public ProtoId<ExplosionPrototype> ExplosionProto = "Default";

    // /// <summary>
    // /// The total amount of intensity an explosion can achieve
    // /// </summary>
    // [DataField]
    // public float ExplosionTotalIntensity = 3f;

    // /// <summary>
    // /// How quickly does the explosion's power slope? Higher = smaller area and more concentrated damage, lower = larger area and more spread out damage
    // /// </summary>
    // [DataField]
    // public float ExplosionDropoff = 2f;

    // /// <summary>
    // /// How much intensity can be applied per tile?
    // /// </summary>
    // [DataField]
    // public float ExplosionMaxTileIntensity = 1f;

    [DataField, AutoNetworkedField]
    public NetEntity? Entangled;
}
