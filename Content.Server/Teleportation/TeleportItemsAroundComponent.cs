namespace Content.Server.Teleportation;

[RegisterComponent, Access(typeof(TeleportItemsAroundSystem))]
public sealed partial class TeleportItemsAroundComponent : Component
{
    [DataField]
    public float Range = 1f;

    [DataField]
    public int Weight = 1;

    [DataField(required: true)]
    public TimeSpan TeleportAt;

    [DataField(required: true)]
    public EntityUid Target;

}
