using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;

//Async await helper.
namespace VAwait
{
    public static class Wait
    {
        public static CancellationTokenSource vawaitTokenSource {get; set;}
        static (VWaitComponent component, GameObject gameObject) runtimeInstance;
        static ConcurrentQueue<SignalAwaiter<bool>> signalPool;
        public static UPlayStateMode playMode {get;set;} = UPlayStateMode.None;
        public static int poolLength {get;set;} = 10;
        /// <summary>
        ///Triggers reinitialization.
        /// </summary>
        public static void ReInit()
        {
            StartAwait();
        }
        static void PrepareAsyncHelper()
        {
            if(signalPool != null && signalPool.Count > 0)
            {
                for(int i = 0; i < signalPool.Count; i++)
                {
                    if(signalPool.TryDequeue(out var func))
                    {
                        func.Reset(true, true);
                        (func as IVSignal).AssignTokenSource(null);
                    }
                }
            }

            if(vawaitTokenSource != null)
            {
                vawaitTokenSource.Cancel();
                vawaitTokenSource.Dispose();
            }

            vawaitTokenSource = new();
            signalPool = new ConcurrentQueue<SignalAwaiter<bool>>();

            for(int i = 0; i < poolLength; i++)
            {
                var ctoken = CancellationTokenSource.CreateLinkedTokenSource(vawaitTokenSource.Token);
                var ins = new SignalAwaiter<bool>();
                (ins as IVSignal).AssignTokenSource(ctoken);
                signalPool.Enqueue(ins);
            }
        }
        /// <summary>
        /// Gets an instance of SignalAwaiter from the pool.
        /// </summary>
        /// <returns></returns>
        static SignalAwaiter<bool> GetPooled()
        {
            if(signalPool.TryDequeue(out var ins))
            {
                return ins;
            }

            var nins = new SignalAwaiter<bool>();
            var ctoken = CancellationTokenSource.CreateLinkedTokenSource(vawaitTokenSource.Token);
            (ins as IVSignal).AssignTokenSource(ctoken);
            return nins;
        }
        /// <summary>
        /// Returns back to pool.
        /// </summary>
        /// <param name="signal"></param>
        public static void ReturnAwaiterToPool(SignalAwaiter<bool> signal)
        {
            if(signalPool.Count < poolLength)
            {
                signal.Reset(false, false);
                signalPool.Enqueue(signal);
            }
            else
            {
                signal.Cancel(false, true);
            }
        }
        /// <summary>
        /// Wait for next frame.
        /// </summary>
        /// <returns></returns>
        public static SignalAwaiter<bool> NextFrame()
        {
            var ins = GetPooled();
            runtimeInstance.component.TriggerFrameCoroutine(ins, ins.tokenSource);
            return ins;
        }
        /// <summary>
        /// Waits for n duration in seconds.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static SignalAwaiter<bool> Seconds (float duration)
        {
            var ins = GetPooled();
            WaitSeconds(duration, ins);
            return ins;
        }
        /// <summary>
        /// Waits for coroutine.
        /// </summary>
        /// <param name="coroutine"></param>
        /// <returns></returns>
        public static SignalAwaiter<bool> Coroutine (IEnumerator coroutine)
        {
            var ins = GetPooled();
            runtimeInstance.component.TriggerCoroutine(coroutine, ins, ins.tokenSource);
            return ins;
        }
        /// <summary>
        /// Init.
        /// </summary>
        public static void StartAwait()
        {
            PrepareAsyncHelper();

            if(runtimeInstance.gameObject == null)
            {
                var go = new GameObject();
                go.name = "VAwait-instance";
                go.AddComponent<VWaitComponent>();
                runtimeInstance = (go.GetComponent<VWaitComponent>(), go);
            }
        }
        static async void WaitSeconds(float duration, SignalAwaiter<bool> signal)
        {
            await Task.Delay(TimeSpan.FromSeconds(duration), signal.tokenSource.Token);

            if(signal.tokenSource.IsCancellationRequested)
            {
                return;
            }
            
            runtimeInstance.component.TriggerFrameCoroutine(signal, signal.tokenSource);
        }
        /// <summary>
        /// Cancels an await.
        /// </summary>
        /// <param name="signal"></param>
        public static void Cancel(this SignalAwaiter<bool> signal)
        {
            ReturnAwaiterToPool(signal);
        }
        /// <summary>
        /// Do this everytime getting out of playmode while in edit-mode or 
        /// </summary>
        public static void DestroyAwaits()
        {
            if(vawaitTokenSource != null)
            {
                vawaitTokenSource.Cancel();
                vawaitTokenSource.Dispose();
                vawaitTokenSource = null;
            }
        }
    }

    public enum UPlayStateMode
    {
        PlayMode,
        None
    }
}