#if UNITY_WEBGL
#define USE_COROUTINES
#elif UNITY_WSA && !UNITY_EDITOR
#define USE_TASKS
#else
#define USE_THREADS
#endif

using System;
using System.Collections;
using System.Collections.Generic;
#if USE_THREADS
using System.Threading;
#endif
#if USE_TASKS
using System.Threading.Tasks;
#endif

using UnityEngine;
using UnityEngine.Profiling;



namespace MixedWorld.Utility
{
    /// <summary>
    /// Allows to execute code in the background. Depending on target platform, it may
    /// run in a coroutine, a background thread or a task. The worker delegate should be
    /// designed to allow for either.
    /// </summary>
    public class BackgroundWorker : IDisposable
    {
        /// <summary>
        /// Main function of a background worker, implemented as a C# yield enumerator method.
        /// Each yield return specifies a delay in seconds to wait until work should be resumed.
        /// </summary>
        /// <param name="arg">Work parameter that was passed via <see cref="Run"/>.</param>
        /// <returns></returns>
        public delegate IEnumerable<float> WorkerMain(object arg);

        private string name = "BackgroundWorker";
        private WorkerMain workerDelegate = null;
        private object workerParameter = null;
        private volatile bool shutdownWorker = false;
        private volatile bool isRunning = false;
        private IEnumerator<float> workEnumerator = null;
        private object stateLock = new object();
        private CustomSampler profileWork = null;

#if USE_THREADS
        private Thread workerThread = null;
#elif USE_TASKS
        private Task workerTask = null;
#elif USE_COROUTINES
        private GameObject workerObject = null;
#endif

        /// <summary>
        /// Instantiates a new background worker. Call <see cref="Run"/> in order to execute the specified 
        /// <see cref="WorkerMain"/> function in the background.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mainFunc"></param>
        public BackgroundWorker(string name, WorkerMain mainFunc)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (mainFunc == null) throw new ArgumentNullException("mainFunc");

            this.name = name;
            this.workerDelegate = mainFunc;
        }
        /// <summary>
        /// Aborts the execution of the background function at the next possible (yield) breaking point.
        /// </summary>
        public void Dispose()
        {
            if (this.isRunning)
                this.shutdownWorker = true;

            lock (this.stateLock)
            {
                this.workerParameter = null;
                this.workerDelegate = null;
            }

#if USE_THREADS
            this.workerThread = null;
#elif USE_TASKS
            this.workerTask = null;
#elif USE_COROUTINES
            if (this.workerObject != null)
            {
                GameObject.Destroy(this.workerObject);
                this.workerObject = null;
            }
#endif
        }

        /// <summary>
        /// Runs the previously specified <see cref="WorkerMain"/> function in the background.
        /// </summary>
        /// <param name="arg"></param>
        public void Run(object arg = null)
        {
            if (this.isRunning)
            {
                throw new InvalidOperationException(string.Format(
                    "Cannot run {0} '{1}' because it is already running.",
                    typeof(BackgroundWorker).Name,
                    this.name));
            }

            this.profileWork = CustomSampler.Create("WorkerThreadMain");
            this.workerParameter = arg;
            this.isRunning = true;

#if USE_THREADS
            this.workerThread = new Thread(WorkerThreadMain);
            this.workerThread.IsBackground = true;
            this.workerThread.Name = this.name;
            this.workerThread.Start(this);
#elif USE_TASKS
            this.workerTask = Task.Run(() =>
            {
                WorkerThreadMain(this);
            });
            this.workerTask.ConfigureAwait(false);
#elif USE_COROUTINES
            this.workerObject = new GameObject(string.Format("BackgroundWorker: {0}", this.name));
            this.workerObject.DontDestroyOnLoad();
            BackgroundWorkerHost workerHost = this.workerObject.AddComponent<BackgroundWorkerHost>();
            workerHost.StartCoroutine(WorkerThreadMain(this).GetEnumerator());
#endif
        }

        private float ExecuteSingleStep()
        {
            const float BreakExecution = -1.0f;
            
            // Acquire the internal state lock, so a main thread Dispose call can't hit
            // us while we're in the middle of proceeding from one work step to the next.
            lock (this.stateLock)
            {
                bool hasNext;
                float delayToNext = 0f;

                this.profileWork.Begin();
                try
                {
                    // Initially retrieve an enumerator for the worker main delegate
                    if (this.workEnumerator == null)
                    {
                        if (this.workerDelegate == null) return BreakExecution;

                        IEnumerable<float> enumerable = this.workerDelegate(this.workerParameter);
                        if (enumerable == null) return BreakExecution;

                        this.workEnumerator = enumerable.GetEnumerator();
                        if (this.workEnumerator == null) return BreakExecution;
                    }

                    // Execute a single work step
                    hasNext = this.workEnumerator.MoveNext();
                    if(hasNext)
                        delayToNext = this.workEnumerator.Current;
                }
                finally
                {
                    this.profileWork.End();
                }

                // Use the yield return value of the current step to determine a sleep delay
                if (!hasNext)
                    return BreakExecution;
                else
                    return delayToNext;
            }
        }
        private void BeginWorkerThread()
        {
            Debug.LogFormat("Background worker '{0}' started", this.name);
        }
        private void EndWorkerThread()
        {
            this.isRunning = false;
            this.workEnumerator = null;
            this.Dispose();
            Debug.LogFormat("Background worker '{0}' ended", this.name);
        }
        private void LogWorkerError(Exception error)
        {
            Debug.LogFormat(
                "Error executing work step in {0} '{1}': {2}",
                typeof(BackgroundWorker).Name,
                this.name,
                error);
        }

#if USE_THREADS
        private static void WorkerThreadMain(object arg)
        {
            BackgroundWorker worker = arg as BackgroundWorker;
            worker.BeginWorkerThread();
            try
            {
                Profiler.BeginThreadProfiling("BackgroundWorker", worker.name);
                while (!worker.shutdownWorker)
                {
                    float delay = worker.ExecuteSingleStep();
                    if (delay < 0.0f) break;

                    Thread.Sleep((int)(delay * 1000.0f));
                }
            }
            catch (Exception e)
            {
                worker.LogWorkerError(e);
            }
            finally
            {
                Profiler.EndThreadProfiling();
                worker.EndWorkerThread();
            }
        }
#elif USE_TASKS
        private static async void WorkerThreadMain(object arg)
        {
            BackgroundWorker worker = arg as BackgroundWorker;
            worker.BeginWorkerThread();
            Profiler.BeginThreadProfiling("BackgroundWorker", worker.name);
            while (!worker.shutdownWorker)
            {
                float delay;
                try
                {
                    delay = worker.ExecuteSingleStep();
                    if (delay < 0.0f) break;
                }
                catch (Exception e)
                {
                    worker.LogWorkerError(e);
                    break;
                }

                if (delay > 0.001f)
                    await Task.Delay((int)(delay * 1000.0f));
                else
                    await Task.Yield();
            }
            Profiler.EndThreadProfiling();
            worker.EndWorkerThread();
        }
#elif USE_COROUTINES
        private static IEnumerable WorkerThreadMain(object arg)
        {
            BackgroundWorker worker = arg as BackgroundWorker;
            worker.BeginWorkerThread();
            while (!worker.shutdownWorker)
            {
                float delay;
                try
                {
                    delay = worker.ExecuteSingleStep();
                    if (delay < 0.0f) break;
                }
                catch (Exception e)
                {
                    worker.LogWorkerError(e);
                    break;
                }

                if (delay > 0.016f)
                    yield return new WaitForSeconds(delay);
                else
                    yield return null;
            }
            worker.EndWorkerThread();
            yield break;
        }
#endif
    }
}