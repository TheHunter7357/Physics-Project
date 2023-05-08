using GXPEngine;
using System.Collections.Generic;
using System;
public static class Camera
{
    private const float SHAKE_POWER = 5f;
    private const float SHAKE_TIME = 18f;

    private static float _interpolationFactor = 0.003f;

    private static bool _state = true;
    private static bool _shake = false;
    private static Transformable _level;
    private static readonly List<Transformable> _focuses = new List<Transformable>();

    public static Vec2 Position => _cameraPosition;

    private static Vec2 _cameraPosition = Vec2.Zero;
    private static Vec2 _destination = Vec2.Zero;

    private static float _shakeCounter;

    public static void Shake() => _shake = true;
    public static void SetFactor(float factor) => _interpolationFactor = factor;
    public static void SetLevel(Transformable level) => _level = level;
    public static void AddFocus(Transformable focus) => _focuses.Add(focus);
    public static void ClearFocuses() => _focuses.Clear();
    public static void SetState(bool state) => _state = state;

    public static void Interpolate()
    {
        if (!_state || _level is null || _focuses.Count == 0)
            return;

        _cameraPosition = new Vec2
        (
            _level.x,
            _level.y 
        );

        _destination = Vec2.Zero;
        foreach (Transformable focus in _focuses)
        {
            _destination = new Vec2
            (
                _destination.x - focus.x,
                _destination.y - focus.y
            );
        }
        _destination /= _focuses.Count;

        _destination = new Vec2
        (
            _destination.x + Game.main.width / 2,
            _destination.y + Game.main.height / 2
        );

        Vec2 interpolatedPosition = Vec2.Lerp
        (
            _cameraPosition,
            _destination,
            _interpolationFactor * Time.deltaTime
        );


        if (_shake)
        {
            _level.SetXY
            (
                interpolatedPosition.x + ((float)(new Random()).NextDouble() - 0.5f) * SHAKE_POWER,
                interpolatedPosition.y + ((float)(new Random()).NextDouble() - 0.5f) * -SHAKE_POWER
            );

            _shakeCounter += Time.deltaTime / 10;
            if (_shakeCounter >= SHAKE_TIME)
            {
                _shakeCounter = 0;
                _shake = false;
            }
        }
        else
        {
            _level.SetXY
            (
                interpolatedPosition.x,
                interpolatedPosition.y
            );
        }
    }
}

