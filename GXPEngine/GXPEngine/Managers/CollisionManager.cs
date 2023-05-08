using System;
using System.Reflection;
using System.Collections.Generic;

namespace GXPEngine.Deprecated
{
	[Serializable] public class CollisionManager
	{
		public static bool SafeCollisionLoop = false;
		public static bool TriggersOnlyOnCollision = false;
		
		private delegate void CollisionDelegate(GameObject gameObject);

		private struct ColliderInfo {
			public Collider collider;
			public CollisionDelegate onCollision;
			public ColliderInfo(Collider collider, CollisionDelegate onCollision) {
				this.collider = collider;
				this.onCollision = onCollision;
			}
		}
	
		private List<Collider> _colliderList = new List<Collider>();
		private List<ColliderInfo> _activeColliderList = new List<ColliderInfo>();
		private Dictionary<GameObject, ColliderInfo> _collisionReferences = new Dictionary<GameObject, ColliderInfo>();
			
		private bool collisionLoopActive = false;

		public CollisionManager () { }	
		public void Step() { }

		public CollisionData[] GetCurrentCollisions (Collider self, bool includeTriggers = true, bool includeSolid = true)
		{
			bool collided = false; 
			List<CollisionData> collisionList = new List<CollisionData>();
			for (int j = _colliderList.Count - 1; j >= 0; j--) 
			{
				if (j >= _colliderList.Count) 
					continue;	
				
				Collider other = _colliderList[j];

				if (other == null || (other.IsTrigger && !includeTriggers) || (!other.IsTrigger && !includeSolid)) 
					continue;

				if (self != other) {
					//Collision.Check(self, other, out CollisionData collisionData);
					//if (!collisionData.isEmpty)
					//{
					//	collided = true;
					//	collisionList.Add(collisionData);
					//}
				}
			}
			if (Settings.CollisionDebug)
			{
				if (self.Owner is Sprite sprite)
					sprite.color = collided ? sprite.color = 0xFF0000 : sprite.color = 0xFFFFFF;
			}

			return collisionList.ToArray();
		}

		public void Add(GameObject gameObject) {
			if (collisionLoopActive) 
				throw new Exception ("Cannot call AddChild for gameobjects during OnCollision - use LateAddChild instead.");
			
			Debug.Log("Added " + gameObject.name + " with components:");
			gameObject.TryGetComponentsAssignableFrom(typeof(Component), out Component[] components);
			if (components != null)
				foreach (Component component in components) 
					Debug.Log(component); 
			
			gameObject.TryGetComponentsAssignableFrom(typeof(Collider), out Component[] colliders);
			if (colliders is null || colliders.Length == 0)
				return;

			Collider collider = colliders[0] as Collider;
			if (!_colliderList.Contains(collider)) 
				_colliderList.Add(collider);

			MethodInfo info = gameObject.GetType().GetMethod("OnCollision", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			if (info != null) {

				CollisionDelegate onCollision = (CollisionDelegate)Delegate.CreateDelegate(typeof(CollisionDelegate), gameObject, info, false);
				if (onCollision != null && !_collisionReferences.ContainsKey (gameObject)) {
					ColliderInfo colliderInfo = new ColliderInfo(collider, onCollision);
					_collisionReferences[gameObject] = colliderInfo;
					_activeColliderList.Add(colliderInfo);
				}

			} else {
				validateCase();
			}
			void validateCase()
			{
				if (gameObject.GetType().GetMethod("OnCollision", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null)
					throw new Exception("'OnCollision' function was not binded. Please check its case (capital O?)");
			}
		}

		public void Remove(GameObject gameObject) 
		{
			if (collisionLoopActive)
				return;

			gameObject.TryGetComponentsAssignableFrom(typeof(Collider), out Component[] components);
			Collider collider = components[0] as Collider;

			_colliderList.Remove(collider);
			if (_collisionReferences.ContainsKey(gameObject)) {
				ColliderInfo colliderInfo = _collisionReferences[gameObject];
				_activeColliderList.Remove(colliderInfo);
				_collisionReferences.Remove(gameObject);
			}
		}

		public void Clear()
		{
			List<GameObject> gameObjects = new List<GameObject>();
			foreach (Collider collider in _colliderList)
				gameObjects.Add(collider.Owner);

			foreach (GameObject gameObject in gameObjects)
				Remove(gameObject);
		}

		public string GetDiagnostics() {
			string output = "\n";
			output += "Number of colliders: " + _colliderList.Count + " \n";
			output += "Number of active colliders: " + _activeColliderList.Count + " \n";
			return output;
		}

		public GameObject[] GetMouseCollisions() => new List<GameObject>().ToArray();
	}
}

