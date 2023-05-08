using GXPEngine;
using System;

[Serializable] public class Collider : Component
{
	public bool IsTrigger { get; protected set; }
	private float _activeRadius = -1;
	public float ActiveRadius
	{
        get
        {
			if (_activeRadius == -1)
				_activeRadius = CalculateActiveRadius();

			return _activeRadius;
        }
	}
	public Vec2 LogicalCenterOfMass { get; protected set; }
	public Vec2 TransformedLogicalCenterOfMass 
	{ 
		get
		{
			Vec2 origin = Owner.position;
			Vec2 transformedLogicalCenterOfMass = LogicalCenterOfMass + Owner.position;
			transformedLogicalCenterOfMass.RotateAroundDegrees(-Owner.rotation, origin);
			return transformedLogicalCenterOfMass;
		} 
	}
	public virtual float momentOfInertia(float mass) => 1;
	public Collider(GameObject owner, params string[] args) : base(owner)
	{
		IsTrigger = args.Length > 0 && bool.Parse(args[0]);
	}
	public virtual float CalculateActiveRadius() => 1;
	public virtual void ShowDebug() { }
	public virtual void ShowEditorDebug() { }
	protected override void Update()
	{
		if (Settings.CollisionDebug)
			ShowDebug();
	}
}

