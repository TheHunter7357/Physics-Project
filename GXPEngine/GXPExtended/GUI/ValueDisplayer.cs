using GXPEngine;
using System;

[Serializable]
public class ValueDisplayer<T> : EasyDraw 
{
    [NonSerialized] private Func<T> _valueGetter;
    public ValueDisplayer(Func<T> valueGetter, int width, int height, string layerMask = "noMask") 
        : base(width, height, layerMask: layerMask)
        => _valueGetter = valueGetter;
    private void Update() => Text(_valueGetter()?.ToString(), clear: true);
}
