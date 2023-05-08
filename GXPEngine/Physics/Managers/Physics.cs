using System.Collections.Generic;
using System.Threading;
using GXPEngine.PhysicsCore;
using GXPEngine;
using System.Reflection;
using System;

public static class Physics
{
    private delegate void OnCollisionHandler(CollisionData collisionData);
    private static Dictionary<GameObject, List<OnCollisionHandler>> _onCollisionReferences = new Dictionary<GameObject, List<OnCollisionHandler>>();

    public static readonly Collision Collision = new Collision();

    public static readonly List<Collider> Colliders = new List<Collider>();
    private static readonly List<Collider> _pendingCollidersAddition = new List<Collider>();
    private static readonly List<Collider> _pendingCollidersRemoving = new List<Collider>();

    public static readonly List<Rigidbody> Rigidbodies = new List<Rigidbody>();
    private static readonly List<Rigidbody> _pendingRigidbodiesAddition = new List<Rigidbody>();
    private static readonly List<Rigidbody> _pendingRigidbodiesRemoving = new List<Rigidbody>();

    private static bool _componentChanging;

    public static readonly Dictionary<GameObject, CollisionData[]> CurrentCollisions = new Dictionary<GameObject, CollisionData[]>();

    private static readonly ManualResetEventSlim _resetEvent = new ManualResetEventSlim(false);
    private static readonly Queue<Rigidbody> _queue = new Queue<Rigidbody>();
    private static Thread[] _threads;

    private static HashSet<string> _layerMasks = new HashSet<string>();
    public static void AddLayer(string layer)
    {
        if (_layerMasks.Contains(layer)) return;
        _layerMasks.Add(layer);
    }
    public static void AddLayers(string[] layers)
    {
        foreach (string layer in layers)
        {
            if (_layerMasks.Contains(layer)) continue;
            _layerMasks.Add(layer);
        }
    }
    public static string[] GetLayers()
    {
        string[] layers = new string[_layerMasks.Count];
        int i = 0;
        foreach (string layer in _layerMasks)
        {
            layers[i] = layer;
            i++;
        }
        return layers;
    }
    public static string GetLayerByID(int ID)
    {
        string layerByID = "";
        int i = 0;
        foreach (string layer in _layerMasks)
        {
            if (i == ID) layerByID = layer;
            i++;
        }
        return layerByID;
    }
    public static bool CompareLayers(string a, string b)
    {
        if (!_layerMasks.Contains(a))
            AddLayer(a);
        if (!_layerMasks.Contains(b))
            AddLayer(b);

        return a == b;
    }

    public static int GetLayerID(string layerName)
    {
        int i = 0;
        foreach (string layer in _layerMasks)
        {
            if (layer.Equals(layerName))
            {
                return i;
            }
            i++;
        }
        return -1;
    }

    public static void Start()
    {
        Debug.Log("\n-----Thread usage debug-----\n");
        _threads = new Thread[Settings.ThreadCount];
        for (int i = 0; i < Settings.ThreadCount; i++)
        {
            Debug.Log("Thread " + (i + 1) + " 's state: true");
            _threads[i] = new Thread(PhysicsUpdate);
            _threads[i].Start();
        }
    }
    public static void Stop()
    {
        _resetEvent.Set();

        foreach (Thread thread in _threads)
            thread.Join();

        _threads = null;
    }
    public static void Step()
    {
        ChangeComponents();
        if (_componentChanging)
            return;
        for (int i = 0; i < Rigidbodies.Count; i++)
        {
            if (Rigidbodies[i] is null) continue;

            lock (_queue)
                _queue.Enqueue(Rigidbodies[i]);

            _resetEvent.Set();
        }
        CurrentCollisions.Clear();
    }
    private static void ChangeComponents()
    {
        _componentChanging = true;
        Colliders.AddRange(_pendingCollidersAddition);
        _pendingCollidersAddition.Clear();

        Rigidbodies.AddRange(_pendingRigidbodiesAddition);
        _pendingRigidbodiesAddition.Clear();

        foreach (Collider collider in _pendingCollidersRemoving)
        {
            Colliders.Remove(collider);
        }
        _pendingCollidersRemoving.Clear();

        foreach (Rigidbody rigidbody in _pendingRigidbodiesRemoving)
        {
            Rigidbodies.Remove(rigidbody);
        }
        _pendingRigidbodiesRemoving.Clear();

        _componentChanging = false;
    }
    private static void PhysicsUpdate()
    {
        while (true)
        {
            if (_componentChanging) continue;

            Rigidbody rigidbody = null;

            lock (_queue)
                if (_queue.Count > 0)
                    rigidbody = _queue.Dequeue();

            if (rigidbody != null)
                rigidbody.PhysicsUpdate();

            else
            {
                _resetEvent.Wait();
                _resetEvent.Reset();
            }
        }
    }

    public static void Add(Component component)
    {
        if (Settings.ComponentRegistrationBlock)
            return;

        if (component is Rigidbody rigidbody)
            _pendingRigidbodiesAddition.Add(rigidbody);
        else if (component is Collider collider)
            _pendingCollidersAddition.Add(collider);

        MethodInfo collisionInfo = component.GetType().GetMethod("OnCollision", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (collisionInfo != null)
        {
            OnCollisionHandler onCollision = (OnCollisionHandler)Delegate.CreateDelegate(typeof(OnCollisionHandler), component, collisionInfo, false);

            if (!_onCollisionReferences.ContainsKey(component.Owner))
                _onCollisionReferences.Add(component.Owner, new List<OnCollisionHandler>());

            if (onCollision != null)
                _onCollisionReferences[component.Owner].Add(onCollision);
        }
    }
    public static void Remove(Component component)
    {
        if (component is Rigidbody rigidbody)
            _pendingRigidbodiesRemoving.Add(rigidbody);
        else if (component is Collider collider)
            _pendingCollidersRemoving.Add(collider);

        MethodInfo collisionInfo = component.GetType().GetMethod("OnCollision", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (collisionInfo != null)
        {
            OnCollisionHandler onCollision = (OnCollisionHandler)Delegate.CreateDelegate(typeof(OnCollisionHandler), component, collisionInfo, false);

            if (_onCollisionReferences.ContainsKey(component.Owner))
                _onCollisionReferences.Remove(component.Owner);
        }
    }

    public static void OnCollision(CollisionData collisionData)
    {
        if (!_onCollisionReferences.ContainsKey(collisionData.self))
            return;

        foreach (OnCollisionHandler onCollision in _onCollisionReferences[collisionData.self])
        {
            onCollision?.Invoke(collisionData);
        }
    }
}