using GXPEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class VerticalList : Sprite
{
    public float DistanceXBetweenElements { get; protected set; } = 32;
    public float DistanceYBetweenElements { get; protected set; } = 32;

    private List<GameObject> _elements = new List<GameObject>();
    private float[] _destinationsY;
    private float[] _destinationsX;
    private int _cols;

    public VerticalList(float deltaY, float deltaX = 0, int cols = 0, string layerMask = "noMask") : base("Empty", false, layerMask: layerMask)
    {
        DistanceXBetweenElements = deltaX;
        DistanceYBetweenElements = deltaY;
        _cols = cols;
        RefreshPositions();
    }

    public void Add(GameObject element)
    {
        _elements.Add(element);
        AddChild(element);

        RefreshPositions();
    }
    public void Add(GameObject[] elements)
    {
        foreach (GameObject element in elements)
        {
            _elements.Add(element);
            AddChild(element);
        }
        RefreshPositions();
    }
    public void Clear()
    {
        for (int i = 0; i < _elements.Count; i++)
            _elements[i].Destroy();

        _elements.Clear();
    }

    private void RefreshPositions()
    {
        _destinationsY = new float[_elements.Count];
        _destinationsX = new float[_elements.Count];
        int xCounter = _cols != 0 ? 0 : - 1;
        int yCounter = -1;
        for (int i = 0; i < _elements.Count; i++)
        {
            if (_cols != 0 && i % _cols == 0)
            {
                xCounter++;
                yCounter = 1;
            }

            yCounter++;

            float newDestinationY = yCounter * DistanceYBetweenElements;
            float newDestinationX = xCounter * DistanceXBetweenElements;

            if (_destinationsY[i] != newDestinationY)
            {
                _destinationsY[i] = newDestinationY;
            }
            if (_destinationsX[i] != newDestinationX)
            {
                _destinationsX[i] = newDestinationX;
            }
        }
    }
    private void Update() => InterpolateToDestinations();
    private void InterpolateToDestinations()
    {
        for (int i = 0; i < _elements.Count; i++)
        {
            if (Mathf.Abs(_elements[i].y - _destinationsY[i]) > 1)
                _elements[i].y = Mathf.Lerp(_elements[i].y, _destinationsY[i], 0.2f);
            if (Mathf.Abs(_elements[i].x - _destinationsX[i]) > 1)
                _elements[i].x = Mathf.Lerp(_elements[i].x, _destinationsX[i], 0.2f);
        }
    }
}