using GXPEngine;
using System;

[Serializable] public class CircleCollider : Collider
{
    private float _radius;
    public float Radius 
    { 
        get => _radius * Owner.scaleX;
        set => _radius = value; 
    }
    public Vec2 Offset { get; protected set; }
    public override float momentOfInertia(float mass) => (mass * Radius * Radius) / 2f;

    public CircleCollider(GameObject owner, params string[] args) : base(owner)
    {
        IsTrigger = args.Length > 0 && bool.Parse(args[0]);

        if (owner is Sprite sprite)
            Radius = args.Length > 1 ? float.Parse(args[1]) : (sprite.width / sprite.scaleY) / 2;
        else
            Radius = args.Length > 1 ? float.Parse(args[1]) : 1;

        Offset = args.Length > 3 ? new Vec2(float.Parse(args[2]), float.Parse(args[3])) : Vec2.Zero;
    }
    public override float CalculateActiveRadius() 
    {
        LogicalCenterOfMass = Offset;
        return Radius;
    }
    public override void ShowDebug()
    {
        Settings.ColliderDebug.Stroke(255, 255, 255);
        Settings.ColliderDebug.Ellipse( Owner.x + Camera.Position.x, Owner.y + Camera.Position.y, Radius * 2, Radius * 2);
    }
    public override void ShowEditorDebug()
    {
        Settings.EditorColliderDebug.Ellipse(Owner.x + Camera.Position.x, Owner.y + Camera.Position.y, Radius * 2, Radius * 2);
    }
}
