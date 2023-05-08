using GXPEngine;
using System;

[Serializable] public class LineCollider : Collider
{
    public Vec2 LineStart { get; protected set; }
    public Vec2 LineEnd { get; protected set; }
    public override float momentOfInertia(float mass) => mass * Mathf.Pow(Vec2.Distance(LineStart, LineEnd), 2)/3;

    public LineCollider(GameObject owner, params string[] args) : base(owner)
    {
        IsTrigger = args.Length > 0 && bool.Parse(args[0]);
        LineStart = args.Length > 2 ? new Vec2(float.Parse(args[1]), float.Parse(args[2])) : Vec2.Zero;
        LineEnd = args.Length > 4 ? new Vec2(float.Parse(args[3]), float.Parse(args[4])) : Vec2.Up;
    }
}

