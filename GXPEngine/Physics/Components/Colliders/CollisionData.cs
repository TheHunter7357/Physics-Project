using GXPEngine;
using System;

public struct CollisionData : IComparable
{
	public GameObject self, other;
	public Vec2 collisionNormal;
	public float timeOfImpact;
	public bool isInside;

	public bool IsDisabled;
	public bool isEmpty;
	public static CollisionData Empty = new CollisionData(true);

	public CollisionData(bool _)
	{
		self = null;
		other = null;
		collisionPoints = null;
		collisionNormal = Vec2.Zero;
		timeOfImpact = -1;
		isInside = false;
		isEmpty = true;
		IsDisabled = false;
	}
	public CollisionData(bool collided, GameObject other)
	{
		self = null;
		this.other = other;
		collisionNormal = Vec2.Zero;
		timeOfImpact = -1;
		isInside = false;
		isEmpty = !collided;
		IsDisabled = false;
	}
	public CollisionData(GameObject self, GameObject other, float timeOfImpact, Vec2 collisionNormal, bool isInside)
	{
		this.self = self;
		this.other = other;
		this.collisionNormal = collisionNormal;
		this.timeOfImpact = timeOfImpact;
		this.isInside = isInside;
		isEmpty = false;
		IsDisabled = false;
	}

	public Vec2 AverageCollisionPoint()
    {
		Vec2 averageCollisionPoint = Vec2.Zero;
        foreach (Vec2 point in collisionPoints)
			averageCollisionPoint += point;

		return averageCollisionPoint / collisionPoints.Length;
    }
	public int CompareTo(object obj) 
	{
		if (timeOfImpact < ((CollisionData)obj).timeOfImpact) return 1;
		else if (timeOfImpact == ((CollisionData)obj).timeOfImpact) return 0;
		else return -1;
	}
}
