using GXPEngine;
using System;

[Serializable] public class RectCollider : PolygonCollider
{
    private float _width;
    private float _height;
    public float Width 
    { 
        get => _width;
        protected set
        {
            _width = value;
            RefreshPoints();
        }
    }
    public float Height 
    {
        get => _height;
        protected set
        {
            _height = value;
            RefreshPoints();
        }
    }
    public override float momentOfInertia(float mass) => (mass * (Width * Width + Height * Height)) / 12f;

    public RectCollider(GameObject owner, params string[] args) : base(owner)
    {
        IsTrigger = args.Length > 0 && bool.Parse(args[0]);
        if (owner is Sprite sprite)
        {
            Width = args.Length > 1 ? float.Parse(args[1]) : sprite.width / sprite.scaleX;
            Height = args.Length > 2 ? float.Parse(args[2]) : sprite.height / sprite.scaleY;
        }
        else
        {
            Width = args.Length > 1 ? float.Parse(args[1]) : 10;
            Height = args.Length > 2 ? float.Parse(args[2]) : 10;
        }
        RefreshPoints();
    }
    protected override void RefreshPoints()
    {
        Points = new Vec2[]
        {
            new Vec2(-Width/2, Height/2),
            new Vec2(Width/2, Height/2),
            new Vec2(Width/2, -Height/2),
            new Vec2(-Width/2, -Height/2),
        };
    }
}
