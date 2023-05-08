using GXPEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class Rigidbody : Component
{
    public bool IsStatic { get; protected set; }

    public Vec2 Velocity { get; protected set; }
    public float AngularVelocity { get; protected set; }
    public float Friction { get; protected set; }
    public float Bounciness { get; protected set; }
    public float Mass { get; protected set; }
    public float Softness { get; protected set; }
    public Vec2 CenterOfMass { get; protected set; }
    public Vec2 ActualVelocity { get; protected set; }
    public float ActualAngularVelocity { get; protected set; }

    private Vec2 _prevPosition;
    private float _prevRotation;
    private Vec2 _prevVelocity;
    private float _prevAngularVelocity;

    private readonly List<Action> CollisionDebugStack;
    private readonly List<Action> SyncedCollisionDebugs;

    public static Rigidbody Standard(GameObject owner) { return new Rigidbody(owner, new string[] { "1", "0,2", "6", "0,2", "0", "0", "false" }); }
    public static Rigidbody Static(GameObject owner) { return new Rigidbody(owner, new string[] { "4", "0,2", "1000", "0,2", "0", "0", "true" }); }
    public static Rigidbody Bullet(GameObject owner) { return new Rigidbody(owner, new string[] { "0", "0,2", "1", "0,2", "0", "0", "false" }); }

    public Rigidbody(GameObject owner, params string[] args) : base(owner)
    {
        _prevVelocity = Vec2.Zero;
        _prevAngularVelocity = 0;

        Velocity = Vec2.Zero;
        AngularVelocity = 0;

        CollisionDebugStack = new List<Action>();
        SyncedCollisionDebugs = new List<Action>();

        if (args.Length == 0) 
            parseParams();
        else
        {
            if (args[0] == "Standard") copyParams(Standard(owner));
            else if (args[0] == "Static") copyParams(Static(owner));
            else if (args[0] == "Bullet") copyParams(Bullet(owner));
            else parseParams();
        }  

        void copyParams(Rigidbody other)
        {
            IsStatic = other.IsStatic;
            Friction = other.Friction;
            Bounciness = other.Bounciness;
            Mass = other.Mass;
            Softness = other.Softness;
            CenterOfMass = other.CenterOfMass;
        }
        void parseParams()
        {
            Friction = args.Length > 0 ? float.Parse(args[0]) : 0;
            Bounciness = args.Length > 1 ? float.Parse(args[1]) : 0;
            Mass = args.Length > 2 ? float.Parse(args[2]) : 0.1f;
            Softness = args.Length > 3 ? float.Parse(args[3]) : 0;
            CenterOfMass = args.Length > 5 ? new Vec2(float.Parse(args[4]), float.Parse(args[5])) : Vec2.Zero;
            IsStatic = args.Length > 6 && bool.Parse(args[6]);
        }
    }

    protected override void Update()
    {
        if (!Enabled)
            return;

        if (Settings.CollisionDebug)
            CollisionDebugUpdate();
    }
    public void PhysicsUpdate()
    {
        if (!Enabled)
            return;

        Move();
        //AngularMove();

        if (Owner.Collider != null && !IsStatic)
        {
            Physics.Collision.CheckAllFor(Owner.Collider, triggers: false, out CollisionData[] collisionDatas);
            ResolveAllCollision(collisionDatas);
            AddCollisionDebugToStack(() => CollisionDebug(collisionDatas));
        }
    }

    protected void Move()
    {
        if (IsStatic)
            return;

        _prevPosition = Owner.position;

        Vec2 stepVelocity = Velocity * (Time.deltaTime / 1000f);
        Owner.SetXY(Owner.x + stepVelocity.x, Owner.y + stepVelocity.y);

        if (_prevVelocity == Velocity)
            ApplyFriction(Friction);

        _prevVelocity = Velocity;
        ActualVelocity = Owner.position - _prevPosition;
    }
    private void ApplyFriction(float friction) {
        Velocity /= 1 + friction * Time.deltaTime / 1000f;
    }

    public void AngularMove()
    {
        if (IsStatic)
            return;

        _prevRotation = Owner.rotation;

        float stepAngularVelocity = AngularVelocity * (Time.deltaTime / 1000f);
        Owner.rotation += Mathf.Rad2Deg * stepAngularVelocity;

        if (_prevAngularVelocity == AngularVelocity)
            ApplyAngularFriction(Friction);

        _prevAngularVelocity = AngularVelocity;
        ActualAngularVelocity = Owner.rotation - _prevRotation;
    }
    private void ApplyAngularFriction(float friction)
    {
        AngularVelocity /= 1 + friction * Time.deltaTime / 1000f;
    }
    public void AddForce(Vec2 force) 
    { 
        Velocity += force / Mass; 
    }   
    public void AddForceAtPosition(Vec2 force, Vec2 position)
    {
        AddForce(force);

        if (Owner.Collider is null)
            return;

        Vec2 torque = (position - CenterOfMass).normal * force.length;
        AngularVelocity += torque.length / Owner.Collider.momentOfInertia(Mass);
    }
    private void ResolveCollision(CollisionData data)
    {
        if (data.isEmpty || data.IsDisabled || data.self.Collider is null || data.self.Rigidbody is null) return;

        if(Physics.CurrentCollisions.ContainsKey(data.other))
            for (int i = 0; i < Physics.CurrentCollisions[data.other].Length; i++)
                Physics.CurrentCollisions[data.other][i].IsDisabled = true;

        Rigidbody selfRigidbody = Owner.Rigidbody;
        Rigidbody otherRigidbody = data.other.Rigidbody is null? Static(data.other) : data.other.Rigidbody;

        if (!IsStatic)
        {
            Vec2 POI = Vec2.Lerp
            (
                Owner.position,
                data.AverageCollisionPoint() + data.collisionNormal * (Owner.Collider is CircleCollider circle ? circle.Radius : 1),
                Mathf.Clamp01(data.timeOfImpact)
            );
            Owner.SetXY(POI.x, POI.y);
        }

        if (!otherRigidbody.IsStatic)
        {
            Vec2 POI = Vec2.Lerp
            (
                data.other.position,
                data.AverageCollisionPoint() - data.collisionNormal * (data.other.Collider is CircleCollider circle ? circle.Radius : 1),
                1 - Mathf.Clamp01(data.timeOfImpact)
            );
            data.other.SetXY(POI.x, POI.y);
        }

        selfRigidbody.ApplyFriction(Friction + otherRigidbody.Friction);
        selfRigidbody.ApplyAngularFriction(Friction + otherRigidbody.Friction);
        otherRigidbody.ApplyFriction(Friction + otherRigidbody.Friction);
        otherRigidbody.ApplyAngularFriction(Friction + otherRigidbody.Friction);

        Vec2 relativeVelocity = selfRigidbody.Velocity - otherRigidbody.Velocity;

        Vec2 collisionNormal = data.collisionNormal.normalized;
        float e = Mathf.Min(selfRigidbody.Bounciness, otherRigidbody.Bounciness);
        float j = -(1 + e) * Vec2.Dot(relativeVelocity, collisionNormal) / (1 / selfRigidbody.Mass + 1 / otherRigidbody.Mass);

        Vec2 impulse = j * collisionNormal;
        selfRigidbody.AddForce(impulse);
        otherRigidbody.AddForce(0 - impulse);
    }
    private void ResolveAllCollision(CollisionData[] datas)
    {
        Array.Sort(datas);
        foreach (CollisionData data in datas)
            if (!data.isEmpty)
                ResolveCollision(data);
    }

    public void AddCollisionDebugToStack(Action collisionDebug) 
    { 
        CollisionDebugStack.Add(collisionDebug); 
    }
    private void CollisionDebugUpdate()
    {;
        SyncedCollisionDebugs.Clear();
        for (int i = 0; i < CollisionDebugStack.Count; i++)
            SyncedCollisionDebugs.Add(CollisionDebugStack[i]);
        
        List<Action> invokedCollisionDebugs = new List<Action>();
        for(int i = 0; i < SyncedCollisionDebugs.Count; i++) 
        {
            if (SyncedCollisionDebugs[i] is null) continue;
            SyncedCollisionDebugs[i].Invoke();

            for (int j = 0; j < CollisionDebugStack.Count; j++)
                if (CollisionDebugStack[j] == SyncedCollisionDebugs[i])
                    invokedCollisionDebugs.Add(CollisionDebugStack[j]);
        }

        foreach (Action invokedCollisionDebug in invokedCollisionDebugs)
            CollisionDebugStack.Remove(invokedCollisionDebug);
    }
    private void CollisionDebug(CollisionData[] collisionDatas)
    {
        if (Settings.CollisionDebug && collisionDatas != null)
        {
            bool collided = false;
            foreach (CollisionData collisionData in collisionDatas)
                if (!collisionData.isEmpty)
                    collided = true;

            if (Owner is Sprite sprite)
                sprite.color = (uint)(collided ? 0xFF0000 : 0xFFFFFF);
        }
    }
}
