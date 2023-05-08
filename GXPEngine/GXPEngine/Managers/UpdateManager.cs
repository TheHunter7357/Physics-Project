using System;
using System.Reflection;
using System.Collections.Generic;

namespace GXPEngine.Managers
{
	[Serializable] public class UpdateManager
	{
		private delegate void UpdateDelegate();

        private readonly float fixedTimeStep = 20;
		private float timeSinceLastFixedUpdate;

		private UpdateDelegate _awakeDelegates;
		private UpdateDelegate _updateDelegates;
		private UpdateDelegate _fixedUpdateDelegates;

		private readonly Dictionary<GameObject, UpdateDelegate> _awakeReferences = new Dictionary<GameObject, UpdateDelegate>();
		private readonly Dictionary<GameObject, UpdateDelegate> _updateReferences = new Dictionary<GameObject, UpdateDelegate>();
        private readonly Dictionary<GameObject, UpdateDelegate> _fixedUpdateReferences = new Dictionary<GameObject, UpdateDelegate>();

        //------------------------------------------------------------------------------------------------------------------------
        //														UpdateManager()
        //------------------------------------------------------------------------------------------------------------------------
        public UpdateManager ()
		{
		}
		//------------------------------------------------------------------------------------------------------------------------
		//														Awake()
		//------------------------------------------------------------------------------------------------------------------------
		public void AwakeStep() => _awakeDelegates?.Invoke();
		
		//------------------------------------------------------------------------------------------------------------------------
		//														Step()
		//------------------------------------------------------------------------------------------------------------------------
		public void Step ()
		{
            _updateDelegates?.Invoke();
            /**/
            if (_fixedUpdateDelegates == null)
				return;
			timeSinceLastFixedUpdate += Time.deltaTime;	
			while(timeSinceLastFixedUpdate >= fixedTimeStep)
			{
                _fixedUpdateDelegates?.Invoke();
                timeSinceLastFixedUpdate -= fixedTimeStep;
			}/**/
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Add()
		//------------------------------------------------------------------------------------------------------------------------
		public void Add(GameObject gameObject) {
			MethodInfo awakeInfo = gameObject.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (awakeInfo != null)
			{
				UpdateDelegate onAwake = (UpdateDelegate)Delegate.CreateDelegate(typeof(UpdateDelegate), gameObject, awakeInfo, false);
				if (onAwake != null && !_fixedUpdateReferences.ContainsKey(gameObject))
				{
					_awakeReferences[gameObject] = onAwake;
					_awakeDelegates += onAwake;
				}
			}
			MethodInfo fixedInfo = gameObject.GetType().GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (fixedInfo != null)
            {
                UpdateDelegate onFixedUpdate = (UpdateDelegate)Delegate.CreateDelegate(typeof(UpdateDelegate), gameObject, fixedInfo, false);
                if (onFixedUpdate != null && !_fixedUpdateReferences.ContainsKey(gameObject))
                {
                    _fixedUpdateReferences[gameObject] = onFixedUpdate;
                    _fixedUpdateDelegates += onFixedUpdate;
                }
            }
            MethodInfo info = gameObject.GetType().GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (info != null)
            {
                UpdateDelegate onUpdate = (UpdateDelegate)Delegate.CreateDelegate(typeof(UpdateDelegate), gameObject, info, false);
                if (onUpdate != null && !_updateReferences.ContainsKey(gameObject))
                {
                    _updateReferences[gameObject] = onUpdate;
                    _updateDelegates += onUpdate;
                }
            }
            else
            {
                validateCase(gameObject);
            }
            /**/
        }

        //------------------------------------------------------------------------------------------------------------------------
        //														validateCase()
        //------------------------------------------------------------------------------------------------------------------------
        private void validateCase(GameObject gameObject) {
			MethodInfo info = gameObject.GetType().GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (info != null) {
				throw new Exception("'Update' function was not binded for '" + gameObject + "'. Please check its case. (capital U?)");
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Contains()
		//------------------------------------------------------------------------------------------------------------------------
		public bool Contains (GameObject gameObject) => _updateReferences.ContainsKey (gameObject) && _fixedUpdateReferences.ContainsKey(gameObject);

		//------------------------------------------------------------------------------------------------------------------------
		//														Remove()
		//------------------------------------------------------------------------------------------------------------------------
		public void Remove(GameObject gameObject) 
		{
			(Dictionary<GameObject, UpdateDelegate> references, UpdateDelegate delegates)[] delegateInfo = 
			{ 
				(_awakeReferences, _awakeDelegates), 
				(_updateReferences, _updateDelegates), 
				(_fixedUpdateReferences, _fixedUpdateDelegates) 
			};
            for (int i = 0; i < delegateInfo.Length; i++)
			{
				if (delegateInfo[i].references.ContainsKey(gameObject))
				{
					UpdateDelegate onAwake = delegateInfo[i].references[gameObject];
					if (onAwake != null) delegateInfo[i].delegates -= onAwake;
					delegateInfo[i].references.Remove(gameObject);
				}
			}
        }

		public void Clear()
		{
			List<GameObject> gameObjects = new List<GameObject>();
			Dictionary<GameObject, UpdateDelegate>[] referencesStack = { _awakeReferences, _updateReferences, _fixedUpdateReferences };
            foreach (Dictionary<GameObject, UpdateDelegate> references in referencesStack)
            {
				foreach (GameObject gameObject in references.Keys)
					gameObjects.Add(gameObject);

				foreach (GameObject gameObject in gameObjects)
				{
					Console.WriteLine("Removing: " + gameObject.GetType());
					if (gameObject.GetType() != typeof(Setup))
						Remove(gameObject);

				}
				gameObjects.Clear();
			}
        }

		public string GetDiagnostics() {
			string output = "";
			output += "Number of update delegates: " + _updateReferences.Count+'\n';
			return output;
		}
	}
}

