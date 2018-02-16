using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_EDITOR && UNITY_METRO
using System.Threading.Tasks;
#endif

//namespace pfc
//{
	public class LoomThreadedJob
	{
		private bool m_IsDone = false;
		private object m_Handle = new object();
		
	#if !UNITY_EDITOR && UNITY_METRO
		private Task m_Task = null;
	#else
		private System.Threading.Thread m_Thread = null;
	#endif

		bool _isDoneExternal;
		public bool IsDone
		{
			get
			{
				return _isDoneExternal;
			}
		}
		bool _isDoneInternal
		{
			get
			{
				bool tmp;
				lock (m_Handle)
				{
					tmp = m_IsDone;
				}
				return tmp;
			}
			set
			{
				lock (m_Handle)
				{
					m_IsDone = value;
				}
			}
		}

		public virtual void Start()
		{
	#if !UNITY_EDITOR && UNITY_METRO
			m_Task = Task.Run(() => Run());
	#else
			m_Thread = new System.Threading.Thread(Run);
			m_Thread.Start();
	#endif
		}

		public virtual void Abort()
		{
	#if !UNITY_EDITOR && UNITY_METRO
			m_Task.Wait();
	#else
			m_Thread.Abort();
	#endif
		}

		protected virtual void ThreadFunction() { }

		protected virtual void OnFinished() { }

		public virtual bool Update()
		{
			if (_isDoneInternal)
			{
				_isDoneExternal = true;
				OnFinished();
				return true;
			}
			return false;
		}

		public IEnumerator WaitFor()
		{
			while (!Update())
			{
				yield return null;
			}
		}

		private void Run()
		{
			try { 
				ThreadFunction();
			}
			catch(System.Exception e)
			{
				Debug.Log(e.ToString());
			}
			_isDoneInternal = true;
		}
	}
//}