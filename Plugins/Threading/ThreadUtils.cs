using System.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThreadUtils
{
    public class Job : IAsyncResult
    {
        Action jobTask;

        Exception exception = null;

        bool isCompleted;
        public bool IsCompleted
        {
            get
            {
                return isCompleted;
            }
        }

        AsyncCallback callback = null;

        object asyncState;
        public object AsyncState
        {
            get
            {
                return asyncState;
            }
        }

        ManualResetEvent waitHandle = null;
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                return GetWaitHandle();
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return false;
            }
        }

        public Job(Action workToDo, AsyncCallback asyncCallback = null, object stateObject = null)
        {
            jobTask = workToDo;
            callback = asyncCallback;
            asyncState = stateObject;
        }

        public void Run()
        {
            try
            {
                jobTask();
            }
            catch (Exception e)
            {
                exception = e;
            }
        }

        public void Completed()
        {
            if (exception != null)
            {
                throw exception;
            }
            if (callback != null)
            {
                callback(this);
            }
            isCompleted = true;
            if (waitHandle != null)
            {
                waitHandle.Set();
            }
        }

        object waitHandleLock = new object();

        WaitHandle GetWaitHandle()
        {
            lock (waitHandleLock)
            {
                if (waitHandle == null)
                {
                    waitHandle = new ManualResetEvent(false);
                }
                if (isCompleted)
                {
                    waitHandle.Set();
                }
            }
            return waitHandle;
        }
    }

    [ExecuteInEditMode]
    public class JobScheduler : MonoBehaviour
    {
        Queue<Job> workToBeDone = new Queue<Job>();
        Queue<Job> completedWork = new Queue<Job>();

        Thread workerThread;

        ManualResetEvent workerThreadResetEvent = new ManualResetEvent(false);

        static JobScheduler instance;
        public static JobScheduler Instance
        {
            get
            {
                if(instance == null)
                {
                    var go = new GameObject("JobScheduler");
                    go.hideFlags = HideFlags.DontSave;
                    instance = go.AddComponent<JobScheduler>();
                }
                return instance;
            }
        }

        IEnumerator coroutine;
        void Awake()
        {
            if(Application.isPlaying) {
                StartCoroutine(Run());
            }
            else {
#if UNITY_EDITOR
                // If you want this to run in Editor use this instead
                coroutine = Run();
                UnityEditor.EditorApplication.update += Progress;
#endif
            }
        }

        void Progress()
        {
            coroutine.MoveNext();
        }

        void OnDestroy()
        {
            if (workerThread != null)
            {
                workerThread.Abort();
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= Progress;
#endif
        }

        public Job AddJob(Action workToDo, AsyncCallback callback = null, object asyncState = null)
        {
            Job job = new Job(workToDo, callback, asyncState);
            lock (workToBeDone)
            {
                workToBeDone.Enqueue(job);
            }
            workerThreadResetEvent.Set();
            return job;
        }

        IEnumerator Run()
        {
            workerThread = new Thread(new ThreadStart(ProcessWork));
            workerThread.IsBackground = true;
            workerThread.Start();
            while (true)
            {
                while (completedWork.Count > 0)
                {
                    Job doneWork = null;
                    lock (completedWork)
                    {
                        doneWork = completedWork.Dequeue();
                    }
                    doneWork.Completed();
                }
                yield return null;
            }
        }

        void ProcessWork()
        {
            while (true)
            {
                if (workToBeDone.Count == 0)
                {
                    workerThreadResetEvent.Reset();
                    workerThreadResetEvent.WaitOne();
                }
                Job currentWork = null;
                lock (workToBeDone)
                {
                    currentWork = workToBeDone.Dequeue();
                }
                currentWork.Run();
                lock (completedWork)
                {
                    completedWork.Enqueue(currentWork);
                }
            }
        }
    }
}