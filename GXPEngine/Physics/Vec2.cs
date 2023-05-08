using System;
using System.Collections.Generic;

[Serializable] public struct Vec2 : IEquatable<Vec2>
{
    #region Fields
    public float x;
    public float y;
    #endregion

    #region Properties
    public Vec2 normalized => this == Zero? Zero : this * (1 / length);
    public float length => Mathf.Sqrt(x * x + y * y);
    public float lengthSquared => x * x + y * y;
    public float angleInDeg => RadToDeg(angleInRad);
    public float angleInRad => (float)(Math.Atan2(y, x) - Math.Atan2(1, 0));
    public Vec2 normal => new Vec2(-y, x).normalized;
    public Vec2 inverted => new Vec2(-x, -y);
    public Vec2 Abs => new Vec2(Mathf.Abs(x), Mathf.Abs(y));
    #endregion

    #region Constructor
    public Vec2(float x, float y) { this.x = x; this.y = y; }
    #endregion

    #region Angles
    public static float DegToRad(float degrees) => degrees * Mathf.PI / 180;
    public static float RadToDeg(float radians) => radians * 180 / Mathf.PI;
    public void SetAngleDegrees(float degrees) => RotateRadians(DegToRad(degrees) - angleInRad);
    public void SetAngleRadians(float radians) => RotateRadians(radians - angleInRad);
    public void RotateDegrees(float degrees) => RotateRadians(DegToRad(degrees));
    public void RotateRadians(float radians) => SetXY(Mathf.Cos(radians) * x - Mathf.Sin(radians) * y, Mathf.Cos(radians) * y + Mathf.Sin(radians) * x);
    public void RotateAroundDegrees(float degrees, Vec2 center) => RotateAroundRadians(DegToRad(degrees), center);
    public void RotateAroundRadians(float radians, Vec2 center) => SetXY(Mathf.Cos(radians) * (x - center.x) - Mathf.Sin(radians) * (y - center.y) + center.x , Mathf.Sin(radians) * (x - center.x) + Mathf.Cos(radians) * (y - center.y) + center.y);
    
    public static Vec2 GetUnitVectorDegrees(float degrees) { Vec2 unit = new Vec2(1, 0); unit.SetAngleDegrees(degrees); return unit; }
    public static Vec2 GetUnitVectorRadians(float radians) { Vec2 unit = new Vec2(1, 0); unit.SetAngleRadians(radians); return unit; }
    public static Vec2 GetRandomUnitVector() { Random rand = new Random(); Vec2 unit = new Vec2(1, 0); unit.SetAngleDegrees(rand.Next(0, 361)); return unit; }
    #endregion

    #region Presets
    public static Vec2 Zero => new Vec2(0, 0);
    public static Vec2 Up => new Vec2(0, 1);
    public static Vec2 Down => new Vec2(0, -1);
    public static Vec2 Left => new Vec2(-1, 0);
    public static Vec2 Right => new Vec2(1, 0);
    #endregion

    #region Operators
    public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
    public static Vec2 operator +(Vec2 a, float b) => new Vec2(a.x + b, a.y + b);
    public static Vec2 operator +(float a, Vec2 b) => new Vec2(a + b.x, a + b.y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
    public static Vec2 operator -(Vec2 a, float b) => new Vec2(a.x - b, a.y - b);
    public static Vec2 operator -(float a, Vec2 b) => new Vec2(a - b.x, a - b.y);
    public static Vec2 operator *(Vec2 a, Vec2 b) => new Vec2(a.x * b.x, a.y * b.y);
    public static Vec2 operator *(Vec2 a, float b) => new Vec2(a.x * b, a.y * b);
    public static Vec2 operator *(float a, Vec2 b) => new Vec2(a * b.x, a * b.y);
    public static Vec2 operator /(Vec2 a, Vec2 b) => new Vec2(a.x / b.x, a.y / b.y);
    public static Vec2 operator /(Vec2 a, float b) => new Vec2(a.x / b, a.y / b);
    public static Vec2 operator /(float a, Vec2 b) => new Vec2(a / b.x, a / b.y);
    public static Vec2 operator ^(float a, Vec2 b) => new Vec2(Mathf.Pow(a, b.x), Mathf.Pow(a, b.y));
    public static Vec2 operator ^(Vec2 a, float b) => new Vec2(Mathf.Pow(a.x, b), Mathf.Pow(a.y, b));
    public static Vec2 operator ^(Vec2 a, Vec2 b) => new Vec2(Mathf.Pow(a.x, b.x), Mathf.Pow(a.y, b.x));
    public static bool operator ==(Vec2 a, Vec2 b) => (a.x == b.x && a.y == b.y);
    public static bool operator !=(Vec2 a, Vec2 b) => (a.x != b.x || a.y != b.y);
    #endregion

    #region Controllers
    public void SetX(float newX) => x = newX;
    public void SetY(float newY) => y = newY;
    public void SetXY(float newX, float newY) { x = newX; y = newY; }
    public void Reflect(Vec2 normal, float bounciness = 1) => this -= (1 + bounciness) * (Dot(this, normal.normalized) * normal.normalized);
    public void Normalize() => this = length == 0? this : this * Mathf.Pow(length, -1);
    public static Vec2 Lerp(Vec2 origin, Vec2 destination, float factor) => ((1 - factor) * origin) + (factor * destination);
    public static float Cross(Vec2 a, Vec2 b) => (a.x * b.normalized.y) - (a.y * b.normalized.x);
    public static float Dot(Vec2 a, Vec2 b) => (a.x * b.normalized.x) + (a.y * b.normalized.y);
    public static float Distance(Vec2 a, Vec2 b) => Mathf.Sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y));
    public override string ToString() => $"( {x} ; {y} )";
    public override bool Equals(object obj) => 
               obj is Vec2 vec &&
               x == vec.x &&
               y == vec.y &&
               EqualityComparer<Vec2>.Default.Equals(normalized, vec.normalized) &&
               length == vec.length &&
               lengthSquared == vec.lengthSquared &&
               angleInDeg == vec.angleInDeg &&
               angleInRad == vec.angleInRad &&
               EqualityComparer<Vec2>.Default.Equals(normal, vec.normal);
    public bool Equals(Vec2 other) => 
               x == other.x &&
               y == other.y &&
               EqualityComparer<Vec2>.Default.Equals(normalized, other.normalized) &&
               length == other.length &&
               lengthSquared == other.lengthSquared &&
               angleInDeg == other.angleInDeg &&
               angleInRad == other.angleInRad &&
               EqualityComparer<Vec2>.Default.Equals(normal, other.normal);
    public override int GetHashCode()
    {
        int hashCode = -754224548;
        hashCode = hashCode * -1521134295 + x.GetHashCode();
        hashCode = hashCode * -1521134295 + y.GetHashCode();
        hashCode = hashCode * -1521134295 + normalized.GetHashCode();
        hashCode = hashCode * -1521134295 + length.GetHashCode();
        hashCode = hashCode * -1521134295 + lengthSquared.GetHashCode();
        hashCode = hashCode * -1521134295 + angleInDeg.GetHashCode();
        hashCode = hashCode * -1521134295 + angleInRad.GetHashCode();
        hashCode = hashCode * -1521134295 + normal.GetHashCode();
        return hashCode;
    }
    #endregion
}