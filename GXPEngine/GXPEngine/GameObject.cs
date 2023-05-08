using GXPEngine.Core;
using System;
using System.Collections.Generic;

namespace GXPEngine
{
    /// <summary>
    /// GameObject is the base class for all display objects. 
    /// </summary>
    [Serializable] public abstract class GameObject : Transformable, ICloneable, IRefreshable
    {
        public string name;
        public string LayerMask { get; private set; }
        public HashSet<string> ExcludedLayerMasks { get; private set; }

        public delegate void BeforeDestroyHandler();
        public BeforeDestroyHandler BeforeDestroy;

        public delegate void ComponentUpdateHandler();
        public ComponentUpdateHandler ComponentUpdate;

        private readonly List<GameObject> _children = new List<GameObject>();
        private GameObject _parent = null;

        public bool visible = true;
        private bool destroyed = false;
        public Rigidbody Rigidbody { get; private set; }
        public Collider Collider { get; private set; }
        private readonly Dictionary<string, Component> _components = new Dictionary<string, Component>();

        public Vec2 position => new Vec2(x, y);

        //------------------------------------------------------------------------------------------------------------------------
        //														GameObject()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Initializes a new instance of the <see cref="GXPEngine.GameObject"/> class.
        /// Since GameObjects contain a display hierarchy, a GameObject can be used as a container for other objects.
        /// Other objects can be added using child commands as AddChild.
        /// </summary>
        /// <param name="addCollider">
        /// If <c>true</c>, then the virtual function createCollider will be called, which can be overridden to create a collider that 
        /// will be added to the collision manager. 
        /// </param> 
        public GameObject(string layerMask = "noMask")
        {
            LayerMask = layerMask == "noMask" ? Physics.GetLayerByID(0) : layerMask;
            ExcludedLayerMasks = new HashSet<string>();
            ResetParameters();
        }

        public void ResetParameters() { }
        public void SetLayerMask(string layer) => LayerMask = layer;
        public void ExcludeLayerMask(string layer)
        {
            if (ExcludedLayerMasks.Contains(layer)) return;
            ExcludedLayerMasks.Add(layer);
        }
        public void ExcludeLayerMasks(string[] layers)
        {
            foreach (string layer in layers)
            {
                if (ExcludedLayerMasks.Contains(layer)) continue;
                ExcludedLayerMasks.Add(layer);
            }
        }
        public bool CompareLayerMask(string layer) => Physics.CompareLayers(LayerMask, layer);
        public bool CompareLayerMaskByID(int ID) => Physics.CompareLayers(LayerMask, Physics.GetLayerByID(ID));
        //------------------------------------------------------------------------------------------------------------------------
        //														Index
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Gets the index of this object in the parent's hierarchy list.
        /// Returns -1 if no parent is defined.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        public int Index
        {
            get
            {
                if (parent == null) return -1;
                return parent._children.IndexOf(this);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														game
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Gets the game that this object belongs to. 
        /// This is a unique instance throughout the runtime of the game.
        /// Use this to access the top of the displaylist hierarchy, and to retreive the width and height of the screen.
        /// </summary>
        public Game game
        {
            get
            {
                return Game.main;
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														Render
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// This function is called by the renderer. You can override it to change this object's rendering behaviour.
        /// When not inside the GXPEngine package, specify the parameter as GXPEngine.Core.GLContext.
        /// This function was made public to accomodate split screen rendering. Use SetViewPort for that.
        /// </summary>
        /// <param name='glContext'>
        /// Gl context, will be supplied by internal caller.
        /// </param>
        public virtual void Render(GLContext glContext)
        {
            if (visible)
            {
                glContext.PushMatrix(matrix);

                RenderSelf(glContext);
                foreach (GameObject child in GetChildren())
                {
                    child.Render(glContext);
                }

                glContext.PopMatrix();
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														RenderSelf
        //------------------------------------------------------------------------------------------------------------------------
        protected virtual void RenderSelf(GLContext glContext)
        {
            if (visible == false) return;
            glContext.PushMatrix(matrix);
            glContext.PopMatrix();
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														parent
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Gets or sets the parent GameObject.
        /// When the parent moves, this object moves along.
        /// </summary>
        public GameObject parent
        {
            get { return _parent; }
            set
            {
                bool wasActive = InHierarchy();
                if (_parent != null)
                {
                    _parent.removeChild(this);
                    _parent = null;
                }
                _parent = value;
                if (value != null)
                {
                    if (destroyed)
                    {
                        throw new Exception("Destroyed game objects cannot be added to the game!");
                    }
                    _parent.addChild(this);
                }
                bool isActive = InHierarchy();
                if (wasActive && !isActive)
                {
                    UnSubscribe();
                }
                else if (!wasActive && isActive)
                {
                    Subscribe();
                }
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														OnDestroy()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Subclasses can implement this method to clean up resources once on destruction. 
        /// Will be called by the engine when the game object is destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            //empty
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														Destroy()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Destroy this instance, and removes it from the game. To complete garbage collection, you must nullify all 
        /// your own references to this object.
        /// </summary>
        public virtual void Destroy()
        {
            if (BeforeDestroy != null)
                BeforeDestroy.Invoke();

            destroyed = true;
            // Detach from parent (and thus remove it from the managers):
            if (parent != null) parent = null;
            ClearComponents();
            OnDestroy();
            DestroyChildren();
        }

        public void DestroyChildren()
        {
            while (_children.Count > 0)
            {
                GameObject child = _children[0];
                if (child != null) child.Destroy();
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														LateDestroy()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Destroy this instance, and removes it from the game, *after* finishing the current Update + OnCollision loops.
        /// To complete garbage collection, you must nullify all your own references to this object.
        /// </summary>
        public void LateDestroy()
        {
            HierarchyManager.Instance.LateDestroy(this);
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														AddChild()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Adds the specified GameObject as a child to this one.
        /// </summary>
        /// <param name='child'>
        /// Child object to add.
        /// </param>
        public virtual void AddChild(GameObject child)
        {
            child.parent = this;
        }

        public void AddChildren(GameObject[] children)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                    AddChild(children[i]);
            }
        }
        public void LateAddChildren(GameObject[] children)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                    LateAddChild(children[i]);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														LateAddChild()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Adds the specified GameObject as a child to this one, *after* finishing the current Update + OnCollision loops.
        /// </summary>
        /// <param name='child'>
        /// Child object to add.
        /// </param>
        public void LateAddChild(GameObject child)
        {
            HierarchyManager.Instance.LateAdd(this, child);
        }

        /// <summary>
        /// Removes this GameObject from the hierarchy (=sets the parent to null).
        /// </summary>
        public void Remove()
        {
            parent = null;
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														LateDestroy()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Removes this GameObject from the hierarchy, *after* finishing the current Update + OnCollision loops.
        /// </summary>
        public void LateRemove()
        {
            HierarchyManager.Instance.LateRemove(this);
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														RemoveChild()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Removes the specified child GameObject from this object.
        /// </summary>
        /// <param name='child'>
        /// Child object to remove.
        /// </param>
        public void RemoveChild(GameObject child)
        {
            if (child.parent == this)
            {
                child.parent = null;
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														removeChild()
        //------------------------------------------------------------------------------------------------------------------------
        private void removeChild(GameObject child)
        {
            _children.Remove(child);

        }

        //------------------------------------------------------------------------------------------------------------------------
        //														addChild()
        //------------------------------------------------------------------------------------------------------------------------
        private void addChild(GameObject child)
        {
            if (child.HasChild(this)) return; //no recursive adding
            _children.Add(child);
            return;
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														AddChildAt()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Adds the specified GameObject as a child to this object at an specified index. 
        /// This will alter the position of other objects as well.
        /// You can use this to determine the draw order of child objects.
        /// </summary>
        /// <param name='child'>
        /// Child object to add.
        /// </param>
        /// <param name='index'>
        /// Index in the child list where the object should be added.
        /// </param>
        public void AddChildAt(GameObject child, int index)
        {
            if (child.parent != this)
            {
                AddChild(child);
            }
            if (index < 0) index = 0;
            if (index >= _children.Count) index = _children.Count - 1;
            _children.Remove(child);
            _children.Insert(index, child);
        }

        public void AddChildrenAt(GameObject[] children, int index)
        {
            for (int i = children.Length - 1; i >= 0; i--)
            {
                if (children[i].parent != this)
                {
                    AddChild(children[i]);
                }
                if (index < 0) index = 0;
                if (index >= _children.Count) index = _children.Count - 1;
                _children.Remove(children[i]);
                _children.Insert(index, children[i]);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														LateAddChild()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Adds the specified GameObject as a child to this one, at the specified index,
        /// *after* finishing the current Update + OnCollision loops.
        /// </summary>
        public void LateAddChildAt(GameObject child, int index)
        {
            HierarchyManager.Instance.LateAdd(this, child, index);
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														HasChild()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Returns 'true' if the specified object is a descendant of this object.
        /// </summary>
        /// <param name='gameObject'>
        /// The GameObject that should be tested.
        /// </param>
        public bool HasChild(GameObject gameObject)
        {
            // for compatibility reasons, the name of this method is not changed - but it is very confusing!
            GameObject par = gameObject;
            while (par != null)
            {
                if (par == this) return true;
                par = par.parent;
            }
            return false;
        }

        /// <summary>
        /// Returns whether this game object is currently active, or equivalently, a descendant of Game.
        /// </summary>
        public bool InHierarchy()
        {
            GameObject current = parent;
            while (current != null)
            {
                if (current is Game)
                    return true;
                current = current.parent;
            }
            return false;
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														GetChildren()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Returns a list of all children that belong to this object.
        /// The function returns System.Collections.Generic.List<GameObject>.
        /// (If safe=false, then the method is slightly faster, but modifying the list will break the engine!)
        /// </summary>
        public List<GameObject> GetChildren(bool safe = true)
        {
            if (safe)
            {
                return new List<GameObject>(_children);
            }
            else
            {
                return _children;
            }
        }

        /// <summary>
        /// Returns the number of children of this game object.
        /// </summary>
        /// <returns>The number of children of this game object.</returns>
        public int GetChildCount()
        {
            return _children.Count;
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														SetChildIndex()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Inserts the specified object in this object's child list at given location.
        /// This will alter the position of other objects as well.
        /// You can use this to determine the drawing order of child objects.
        /// </summary>
        /// <param name='child'>
        /// Child.
        /// </param>
        /// <param name='index'>
        /// Index.
        /// </param>
        public void SetChildIndex(GameObject child, int index)
        {
            if (child.parent != this) AddChild(child);
            if (index < 0) index = 0;
            if (index >= _children.Count) index = _children.Count - 1;
            _children.Remove(child);
            _children.Insert(index, child);
        }

        private void Subscribe()
        {
            game.Add(this);
            foreach (GameObject child in _children)
            {
                child.Subscribe();
            }
        }

        private void UnSubscribe()
        {
            game.Remove(this);
            foreach (GameObject child in _children)
            {
                child.UnSubscribe();
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														TransformPoint()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Transforms a point from local to global space.
        /// If you insert a point relative to the object, it will return that same point relative to the game.
        /// </summary>
        /// <param name='x'>
        /// The x coordinate to transform.
        /// </param>
        /// <param name='y'>
        /// The y coordinate to transform.
        /// </param>
        public override Vec2 TransformPoint(float x, float y)
        {
            Vec2 ret = base.TransformPoint(x, y);
            if (parent == null)
            {
                return ret;
            }
            else
            {
                return parent.TransformPoint(ret.x, ret.y);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														TransformPoint()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Transforms a point from this object's local space to the space of a given ancestor.
        /// If you insert a point relative to this game object, this method will return that same point relative to the given ancestor.
        /// </summary>
        /// <param name='x'>
        /// The x coordinate to transform.
        /// </param>
        /// <param name='y'>
        /// The y coordinate to transform.
        /// </param>
        /// <param name='targetParentSpace'>
        /// This method will return the coordinates of the given point relative to this given game object. 
        /// If the given game object is not an ancestor of [this] game object, then 
        /// this argument is ignored, and the returned point will be in screen space.
        /// </param>
        public Vec2 TransformPoint(float x, float y, GameObject targetParentSpace)
        {
            // Implementation note: since the original TransformPoint is a core engine method,
            // efficiency (avoiding an extra method call) is preferred here at the cost of some code duplication.
            Vec2 ret = base.TransformPoint(x, y);
            if (parent == null || parent == targetParentSpace)
            {
                return ret;
            }
            else
            {
                return parent.TransformPoint(ret.x, ret.y, targetParentSpace);
            }
        }

        /// <summary>
        /// Transforms a direction vector from local to global space.
        /// If you insert a vector relative to the object, it will return that same vector relative to the game.
        /// Note: only scale and rotation information are taken into account, not translation (coordinates).
        /// </summary>
        /// <param name='x'>
        /// The x coordinate to transform.
        /// </param>
        /// <param name='y'>
        /// The y coordinate to transform.
        /// </param>
        public override Vec2 TransformDirection(float x, float y)
        {
            Vec2 ret = base.TransformDirection(x, y);
            if (parent == null)
            {
                return ret;
            }
            else
            {
                return parent.TransformDirection(ret.x, ret.y);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //												InverseTransformPoint()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Transforms the point from global into local space.
        /// If you insert a point relative to the game, it will return that same point relative to this GameObject.
        /// </summary>
        /// <param name='x'>
        /// The x coordinate to transform.
        /// </param>
        /// <param name='y'>
        /// The y coordinate to transform.
        /// </param>
        public override Vector2 InverseTransformPoint(float x, float y)
        {
            return InverseTransformPoint(x, y, null);
        }

        //------------------------------------------------------------------------------------------------------------------------
        //												InverseTransformPoint()
        //------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Transforms the point from the space of a given ancestor into this object's local space.
        /// If you insert a point relative to the given ancestor, it will return that same point relative to this GameObject.
        /// </summary>
        /// <param name='x'>
        /// The x coordinate to transform.
        /// </param>
        /// <param name='y'>
        /// The y coordinate to transform.
        /// </param>
        /// <param name='fromParentSpace'>
        /// The coordinates x and y should be given relative to this game object. If the given game object is not an ancestor of 
        /// [this] game object, then this argument is ignored and x and y are assumed to be in screen space.
        /// </param>
        public Vector2 InverseTransformPoint(float x, float y, GameObject fromParentSpace)
        {
            if (parent == null || parent == fromParentSpace)
            {
                return base.InverseTransformPoint(x, y);
            }
            else
            {
                Vector2 ret = parent.InverseTransformPoint(x, y, fromParentSpace);
                return base.InverseTransformPoint(ret.x, ret.y);
            }
        }

        /// <summary>
        /// Transforms the vector from global into local space.
        /// If you insert a vector relative to the game, it will return that same vector relative to this GameObject.
        /// Note: only scale and rotation information are taken into account, not translation (coordinates).
        /// </summary>
        /// <param name='x'>
        /// The x coordinate to transform.
        /// </param>
        /// <param name='y'>
        /// The y coordinate to transform.
        /// </param>
        public override Vector2 InverseTransformDirection(float x, float y)
        {
            if (parent == null)
            {
                return base.InverseTransformDirection(x, y);
            }
            else
            {
                Vector2 ret = parent.InverseTransformDirection(x, y);
                return base.InverseTransformDirection(ret.x, ret.y);
            }
        }

        /// <summary>
        /// Returns the first object of the given type, found within the descendants of this game object
        /// (including this game object itself).
        /// If there's no descendant of the given type, returns null.
        /// For example, if you have made a Player class, call this method like this: 
        ///  game.FindObjectOfType(typeof(Player));
        /// </summary>
        /// <param name="type">The object type you're looking for (must inherit from GameObject)</param>
        /// <returns>A descendant of the given type, if it exists.</returns>
        public GameObject FindObjectOfType(Type type)
        {
            if (GetType() == type)
            {
                return this;
            }
            foreach (GameObject child in _children)
            {
                GameObject result = child.FindObjectOfType(type);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the first object of the given type, found within the descendants of this game object
        /// (including this game object itself).
        /// If there's no descendant of the given type, returns null.
        /// The given type must inherit from GameObject.
        /// </summary>
        /// <returns>A descendant of the given type, if it exists.</returns>
        public T FindObjectOfType<T>() where T : GameObject
        {
            if (this is T t)
            {
                return t;
            }
            foreach (GameObject child in _children)
            {
                T result = child.FindObjectOfType<T>();
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the all objects of the given type, found within the descendants of this game object
        /// (including this game object itself).
        /// For example, if you have made a Player class, call this like this: 
        ///  game.FindObjectsOfType(typeof(Player));
        /// </summary>
        /// <param name="type">The object type you're looking for (must inherit from GameObject)</param>
        /// <returns>All descendants of the given type.</returns>
        public GameObject[] FindObjectsOfType(Type type)
        {
            List<GameObject> results = new List<GameObject>();
            FindObjectsOfType(type, results);
            return results.ToArray();
        }
        private void FindObjectsOfType(Type type, List<GameObject> results)
        {
            if (GetType() == type)
            {
                results.Add(this);
            }
            foreach (GameObject child in _children)
            {
                child.FindObjectsOfType(type, results);
            }
        }

        /// <summary>
        /// Returns the all objects of the given type, found within the descendants of this game object
        /// (including this game object itself).
        /// The type must inherit from GameObject.
        /// </summary>
        /// <returns>All descendants of the given type.</returns>
        public T[] FindObjectsOfType<T>() where T : GameObject
        {
            List<T> results = new List<T>();
            FindObjectsOfType<T>(results);
            return results.ToArray();
        }
        private void FindObjectsOfType<T>(List<T> results) where T : GameObject
        {
            if (this is T t)
            {
                results.Add(t);
            }
            foreach (GameObject child in _children)
            {
                child.FindObjectsOfType<T>(results);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														Clone()
        //------------------------------------------------------------------------------------------------------------------------
        public abstract object Clone();
        
        //------------------------------------------------------------------------------------------------------------------------
        //														Serialization()
        //------------------------------------------------------------------------------------------------------------------------
        public void OnSerialized()
        {
            if(this is IRefreshable refreshableGameObject)
                refreshableGameObject.Refresh();

            foreach (GameObject child in GetChildren())
                child.OnSerialized();
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														Refresh()
        //------------------------------------------------------------------------------------------------------------------------

        public virtual void Refresh()
        {
            foreach (Component component in _components.Values)
            {
                Physics.Add(component);
                if (component is IRefreshable refreshableComponent)
                    refreshableComponent.Refresh();
            }
        }
        //------------------------------------------------------------------------------------------------------------------------
        //														Components
        //------------------------------------------------------------------------------------------------------------------------
        /// <param name="args"> 
        /// Component parameters given on addition(creation), order depends on component type.
        /// Example: Rigidbody parameters are {Friction, Bounciness, Mass ...}
        /// </param>
        public void AddComponent(Type type, params string[] args)
        {
            if (!_components.ContainsKey(type.Name))
            {
                Component component = (Component)Activator.CreateInstance(type, new object[] { this, args });
                Physics.Add(component);
                _components.Add(type.Name, component);

                if (component is Rigidbody rigidbody)
                    Rigidbody = rigidbody;
                else if (component is Collider collider)
                    Collider = collider;
            }
        }
        public void RemoveComponent(Component component)
        {
            Physics.Remove(component);
            if (component is Rigidbody)
                Rigidbody = null;

            else if (component is Collider)
                Collider = null;

            if (_components.ContainsKey(component.GetType().Name))
                _components.Remove(component.GetType().Name);
        }
        public void ClearComponents()
        {
            foreach (Component component in _components.Values)
            {
                Physics.Remove(component);
            }

            if (Collider != null)
            {
                Physics.Colliders.Remove(Collider);
                Collider = null;
            }

            if (Rigidbody != null)
            {
                Physics.Rigidbodies.Remove(Rigidbody);
                Rigidbody = null;
            }

            _components.Clear();
        }

        public void TryGetComponent(Type type, out Component component)
        {
            component = null;
            if (_components.ContainsKey(type.Name))
                component = _components[type.Name];
        }
        public void TryGetComponentsAssignableFrom(Type type, out Component[] components)
        {
            components = null;

            if (_components.Count == 0)
                return;

            List<Component> componentBuffer = new List<Component>();
            foreach (object component in _components.Values)
            {
                if (type.IsAssignableFrom(component.GetType()))
                    componentBuffer.Add(component as Component);
            }
            components = componentBuffer.ToArray();
        }
        public Component[] GetAllComponents()
        {
            Component[] components = new Component[_components.Count];
            _components.Values.CopyTo(components, 0);
            return components;
        }
        //------------------------------------------------------------------------------------------------------------------------
        //														ToString()
        //------------------------------------------------------------------------------------------------------------------------
        public override string ToString() => "[" + GetType().Name + "::" + name + "]";   
    }
}