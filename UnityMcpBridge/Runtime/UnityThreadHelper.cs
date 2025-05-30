using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Windsurf.UnityMcp
{
    /// <summary>
    /// Helper class to run operations on the Unity main thread
    /// </summary>
    public static class UnityThreadHelper
    {
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static readonly object _lock = new object();
        private static MonoBehaviour _runner;
        
        /// <summary>
        /// Initialize the thread helper with a MonoBehaviour to run coroutines
        /// </summary>
        public static void Initialize(MonoBehaviour runner)
        {
            if (_runner == null)
            {
                _runner = runner;
            }
        }
        
        /// <summary>
        /// Run an action on the main thread
        /// </summary>
        public static void RunOnMainThread(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            
            lock (_lock)
            {
                _executionQueue.Enqueue(action);
            }
        }
        
        /// <summary>
        /// Run an action on the main thread and wait for it to complete
        /// </summary>
        public static Task RunOnMainThreadAsync(Action action)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            
            RunOnMainThread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            return tcs.Task;
        }
        
        /// <summary>
        /// Run a function on the main thread and return its result
        /// </summary>
        public static Task<T> RunOnMainThreadAsync<T>(Func<T> function)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            
            RunOnMainThread(() =>
            {
                try
                {
                    T result = function();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            return tcs.Task;
        }
        
        /// <summary>
        /// Process all queued actions
        /// </summary>
        public static void Update()
        {
            lock (_lock)
            {
                while (_executionQueue.Count > 0)
                {
                    Action action = _executionQueue.Dequeue();
                    action();
                }
            }
        }
        
        /// <summary>
        /// Coroutine to wait for a task to complete
        /// </summary>
        public static IEnumerator WaitForTask(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }
            
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }
    }
}
