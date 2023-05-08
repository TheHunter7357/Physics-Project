using GXPEngine;
using System;

[Serializable]
public class PlayerController : Component, IRefreshable
{
    public float MaxSpeed { get; protected set; }
    public float Acceleration { get; protected set; }
    public PlayerController(GameObject owner, params string[] args) : base(owner)
    {
        SubscribeButtons();
        MaxSpeed = args.Length > 0 ? float.Parse(args[0]) : 120f;
        Acceleration = args.Length > 1 ? float.Parse(args[1]) : 6f;
    }
    protected void SubscribeButtons()
    {
        if (Owner.Rigidbody is null) return;

        InputManager.OnUpButtonPressed += () => Owner.Rigidbody.AddForce(Owner.Rigidbody.Velocity.length <= MaxSpeed ? Vec2.Up * Acceleration : Vec2.Zero);
        InputManager.OnDownButtonPressed += () => Owner.Rigidbody.AddForce(Owner.Rigidbody.Velocity.length <= MaxSpeed ? Vec2.Down * Acceleration : Vec2.Zero);
        InputManager.OnLeftButtonPressed += () => Owner.Rigidbody.AddForce(Owner.Rigidbody.Velocity.length <= MaxSpeed ? Vec2.Left * Acceleration : Vec2.Zero);
        InputManager.OnRightButtonPressed += () => Owner.Rigidbody.AddForce(Owner.Rigidbody.Velocity.length <= MaxSpeed ? Vec2.Right * Acceleration : Vec2.Zero);
    }

    public override void Refresh()
    {
        base.Refresh();
        SubscribeButtons();
    }
}
