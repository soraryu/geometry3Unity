using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;

#if !UNITY_EDITOR && UNITY_METRO
using System.Threading.Tasks;
#endif

namespace g3
{
    [ExecuteInEditMode]
	public class Loom : MonoBehaviour
	{ 
		/// <summary>
		/// Should be called my MonoBehaviours using Loom  on Awake if no Singleton is in the scene.
		/// </summary>
		public static void Check()
		{
            // if (!Application.isPlaying) return;

			if (_current == null || !_current.isActiveAndEnabled)
			{
                _current = GameObject.FindObjectOfType<Loom>();
                if (_current == null)
                {
                    var g = new GameObject("__g3:EditorThreadingHelper");
                    g.hideFlags = HideFlags.DontSave;
                    _current = g.AddComponent<Loom>();
                };
			}
		}

		private static Loom _current;
		private int _count;

		public static Loom Current
		{
			get
			{
				return _current;
			}
		}

		void Awake()
		{
            Init();
        }

    private void Start()
    {
        Init();
    }

    void Init()
    {
        if (Loom._current != null)
            DestroyImmediate(Loom._current.gameObject);
        Loom._current = this;

        Check();
    }

    private void OnDestroy()
    {
        _actions.Clear();
        _delayed.Clear();
    }

    private List<Action> _actions = new List<Action>();
		public class DelayedQueueItem
		{
			public float time;
			public Action action;
		}
		private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

		public static void QueueOnMainThread(Action action, float time = 0f)
		{
			if (time != 0)
			{
				lock (Current._delayed)
				{
					Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action });
				}
			}
			else
			{
				lock (Current._actions)
				{
					Current._actions.Add(action);
				}
			}
		}

		public static void RunAsync(Action a)
		{
            Check();

#if !UNITY_EDITOR && UNITY_METRO
			Task.Run(() => RunAction(a));
#else
            var t = new Thread(RunAction);
            t.Priority = System.Threading.ThreadPriority.Normal;
            t.Start(a);
#endif
    }

		private static void RunAction(object action)
		{
			((Action)action)();
		}

		void OnDisable()
		{
			if (_current == this)
			{

				_current = null;
			}
		}

		// Update is called once per frame
		void Update()
		{
            Check();

			var actions = new List<Action>();
			lock (_actions)
			{
				actions.AddRange(_actions);
				_actions.Clear();
            }
            // Debug.Log("updating loom : " + _actions.Count);
            foreach (var a in actions)
			{
				a();
			}
			var delayList = new List<DelayedQueueItem>();
			lock (_delayed)
			{
				delayList.AddRange(_delayed);
			}
			foreach (var delayed in delayList.Where(d => d.time <= Time.time).ToList())
			{
				lock (_delayed)
				{
					_delayed.Remove(delayed);
				}
				delayed.action();
			}
		}
	}
}