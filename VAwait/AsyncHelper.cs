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
        static CancellationTokenSource vawaitTokenSource { get; set; }
        public static (VWaitComponent component, GameObject gameObject) runtimeInstance;
        static ConcurrentQueue<SignalAwaiter> signalPool;
        static Dictionary<int, SignalAwaiter> setIdd = new();
        public static UPlayStateMode playMode { get; set; } = UPlayStateMode.None;
        public static int poolLength { get; set; } = 15;
        public static SynchronizationContext unityContext { get; set; }

        public static void RemoveIDD(int id)
        {
            setIdd.Remove(id);
        }
        /// <summary>
        ///Triggers reinitialization.
        /// </summary>
        public static void ReInit()
        {
            StartAwait();
        }
        static void PrepareAsyncHelper()
        {
            if (vawaitTokenSource != null)
            {
                vawaitTokenSource.Cancel();
                vawaitTokenSource.Dispose();
            }

            if (signalPool != null && signalPool.Count > 0)
            {
                for (int i = 0; i < signalPool.Count; i++)
                {
                    if (signalPool.TryDequeue(out var func))
                    {
                        func.Cancel();
                    }
                }
            }

            vawaitTokenSource = new();
            signalPool = new ConcurrentQueue<SignalAwaiter>();

            for (int i = 0; i < poolLength; i++)
            {
                var ins = new SignalAwaiter(vawaitTokenSource);
                signalPool.Enqueue(ins);
            }
        }
        /// <summary>
        /// Gets an instance of SignalAwaiter from the pool.
        /// </summary>
        /// <returns></returns>
        static SignalAwaiter GetPooled()
        {
            if (signalPool.TryDequeue(out var ins))
            {
                return ins;
            }

            var nins = new SignalAwaiter(vawaitTokenSource);
            return nins;
        }
        /// <summary>
        /// Returns back to pool.
        /// </summary>
        /// <param name="signal"></param>
        public static void ReturnAwaiterToPool(SignalAwaiter signal)
        {
            signal.Reset();

            if (signalPool.Count < poolLength)
            {
                signalPool.Enqueue(signal);
            }
        }
        /// <summary>
        /// Wait for next frame. NCan't be awaited more than once.
        /// </summary>
        public static SignalAwaiter NextFrame()
        {
            var ins = GetPooled();
            runtimeInstance.component.TriggerFrameCoroutine(ins);
            return ins;
        }
        /// <summary>
        /// Reusable awaiter, can be awaited multiple times.
        /// </summary>
        public static SignalAwaiterReusable NextFrameReusable()
        {
            return new SignalAwaiterReusable(vawaitTokenSource);
        }
        /// <summary>
        /// Reusable awaiter that can be awaited multiple times.Unlike NextFrameReusable, a CancellationTokenSource must be provided.
        /// </summary>
        /// <param name="cts">Token source.</param>
        public static SignalAwaiterReusable SecondsReusable(float time, CancellationTokenSource cancellationTokenSource)
        {
            var ins = new SignalAwaiterReusable(cancellationTokenSource);
            ins.wait = new WaitForSeconds(time);
            return ins;
        }
        /// <summary>
        /// Awaits for the next FixedUpdate.
        /// </summary>
        /// <returns></returns>
        public static SignalAwaiter FixedUpdate()
        {
            var ins = GetPooled();
            runtimeInstance.component.TriggerFixedUpdateCoroutine(ins);
            return ins;
        }
        /// <summary>
        /// Fixed 1 frame value meant to be used for frame waiting while in edit-mode. This is not accurate,\njust a very rough estimation based on screen's refresh rate.
        /// </summary>
        /// <returns>Double value.</returns>
        public static double OneFrameFixed
        {
            get
            {
                var refValue = Screen.currentResolution.refreshRateRatio.value;
                return (1d / refValue) * 1000;
            }
        }
        /// <summary>
        /// Waits for n duration in seconds.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static SignalAwaiter Seconds(float duration)
        {
            var ins = GetPooled();
            _ = WaitSeconds(duration, ins);
            return ins;
        }
        /// <summary>
        /// Waits for n duration in seconds.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static SignalAwaiter Seconds(float duration, int setId)
        {
            if (setId < 0)
            {
                throw new Exception("VAwait Error : Id can't be negative number");
            }

            var ins = GetPooled();
            ins.GetSetId = setId;

            setIdd.TryAdd(setId, ins);
            _ = WaitSeconds(duration, ins);
            return ins;
        }
        /// <summary>
        /// Waits for coroutine.
        /// </summary>
        /// <param name="coroutine"></param>
        /// <returns></returns>
        public static SignalAwaiter Coroutine(IEnumerator coroutine)
        {
            var ins = GetPooled();

            ins.AssignEnumerator(coroutine);
            runtimeInstance.component.TriggerCoroutine(coroutine, ins);
            return ins;
        }
        /// <summary>
        /// Init.
        /// </summary>
        public static void StartAwait()
        {
            PrepareAsyncHelper();

            if (runtimeInstance.gameObject == null)
            {
                var go = new GameObject();
                go.name = "VAwait-instance";
                go.AddComponent<VWaitComponent>();
                runtimeInstance = (go.GetComponent<VWaitComponent>(), go);
            }
        }
        /// <summary>
        /// Internal use for wait for Seconds.
        /// </summary>
        static async ValueTask WaitSeconds(float duration, SignalAwaiter signal)
        {
            await Task.Delay(TimeSpan.FromSeconds(duration), vawaitTokenSource.Token);
            runtimeInstance.component.TriggerFrameCoroutine(signal);
        }
        /// <summary>
        /// Waits until Predicate<bool> is True.
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="tokenSource"></param>
        /// <returns></returns>
        static async ValueTask WaitUntil(Predicate<bool> predicate , CancellationTokenSource tokenSource)
        {
            var frame = NextFrameReusable();

            while(predicate.Invoke(false))
            {
                await frame;
                
                if(tokenSource.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Cancels an await.
        /// </summary>
        public static void TryCancel(int id)
        {
            if (setIdd.TryGetValue(id, out var func))
            {
                func.Cancel();
                ReturnAwaiterToPool(func);
            }
        }
        /// <summary>
        /// Destroy and cancel awaits, will re-initialize.
        /// </summary>
        public static void ForceCancelAll()
        {
            DestroyAwaits();
            ReInit();
        }
        /// <summary>
        /// Do this everytime getting out of playmode while in edit-mode or 
        /// </summary>
        public static void DestroyAwaits()
        {
            if (vawaitTokenSource != null)
            {
                vawaitTokenSource.Cancel();
                vawaitTokenSource.Dispose();
                vawaitTokenSource = null;
            }
        }

        public static (VWaitComponent component, GameObject gameObject) GetRuntimeInstance()
        {
            return runtimeInstance;
        }
        //The life time of this token is based on your application's lifetime and can't be cancelled.
        public static CancellationToken GetToken
        {
            get
            {
                return vawaitTokenSource.Token;
            }
        }
    }

    public enum UPlayStateMode
    {
        PlayMode,
        None
    }
}