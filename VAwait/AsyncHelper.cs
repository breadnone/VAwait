/*
MIT License

Copyright 2023 Stevphanie Ricardo

Permission is hereby granted, free of charge, to any person obtaining a copy of this
software and associated documentation files (the "Software"), to deal in the Software
without restriction, including without limitation the rights to use, copy, modify,
merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using UnityEditor;
using System.Runtime.CompilerServices;

//Async await helper.
namespace VAwait
{
    public static class Wait
    {
        static CancellationTokenSource awaitTokenSource { get; set; }
        public static (VWaitComponent component, GameObject gameObject) runtimeInstance;
        static ConcurrentQueue<SignalAwaiter> signalPool;
        static Dictionary<int, SignalAwaiter> setIdd = new();
        public static UPlayStateMode playMode { get; set; } = UPlayStateMode.None;
        public static int poolLength { get; set; } = 15;
        static SynchronizationContext unityContext;

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
            if (unityContext == null)
            {
                unityContext = SynchronizationContext.Current;
            }

            if (awaitTokenSource != null)
            {
                awaitTokenSource.Cancel();
                awaitTokenSource.Dispose();
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

            awaitTokenSource = new();
            signalPool = new ConcurrentQueue<SignalAwaiter>();

            for (int i = 0; i < poolLength; i++)
            {
                var ins = new SignalAwaiter(awaitTokenSource);
                signalPool.Enqueue(ins);
            }
        }
        /// <summary>
        /// Gets an instance of SignalAwaiter from the pool.
        /// </summary>
        static SignalAwaiter GetPooled()
        {
            if (signalPool.TryDequeue(out var ins))
            {
                return ins;
            }

            var nins = new SignalAwaiter(awaitTokenSource);
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
        /// Pooled awaiter, awaits for next frame. Can't be awaited more than once. This awaiter won't be affected by Time.timeScale, use Wait.Null or Wait.FixedUpdate instead.
        /// </summary>
        public static SignalAwaiter NextFrame()
        {
            var ins = GetPooled();

            try
            {
                return ins;
            }
            finally
            {
                PlayerLoopUpdate.playerLoopUtil.QueueNextFrame(ins);
            }
        }
        /// <summary>
        /// Wait until end of frame. Can't be awaited more than once. This awaiter will not be affected by Time.timeScale. 
        /// </summary>
        public static SignalAwaiter EndOfFrame()
        {
            var ins = GetPooled();

            try
            {
                return ins;
            }
            finally
            {
                //runtimeInstance.component.TriggerEndFrame(ins);
                PlayerLoopUpdate.playerLoopUtil.QueueEndOfFrame(ins);
            }
        }
        /// <summary>
        /// Reusable awaiter, can be awaited multiple times. This awaiter won't be affected by Time.timeScale, use Wait.Null or Wait.FixedUpdate instead.
        /// </summary>
        public static SignalAwaiterReusable NextFrameReusable()
        {
            return new SignalAwaiterReusable(awaitTokenSource);
        }
        /// <summary>
        /// Equals to NextFrame but respects the Time.timeScale.
        /// </summary>
        /// <returns></returns>
        public static SignalAwaiterReusable Null()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                throw new Exception("VAwait Error : NullAlloc can't be used for edit mode.");
            }
#endif

            var ins = new SignalAwaiterReusable(awaitTokenSource);

            try
            {
                return ins;
            }
            finally
            {
                PlayerLoopUpdate.playerLoopUtil.QueueFixedUpdate(ins);
            }
        }
        /// <summary>
        /// This calls Unity's coroutine.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static SignalAwaiter SecondsScaled(float time)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                throw new Exception("VAwait Error : SecondsScaled can't be used for edit mode.");
            }
#endif

            var ins = GetPooled();

            try
            {
                return ins;
            }
            finally
            {
                runtimeInstance.component.TriggerSecondsCoroutine(ins, time);
            }
        }
        /// <summary>
        /// Awaits for the next FixedUpdate.
        /// </summary>
        public static SignalAwaiterReusable FixedUpdate()
        {
            var ins = new SignalAwaiterReusable(awaitTokenSource);

            try
            {
                return ins;
            }
            finally
            {
                PlayerLoopUpdate.playerLoopUtil.QueueFixedUpdate(ins);
                //runtimeInstance.component.TriggerFixedUpdateCoroutine(ins);
            }
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
        public static SignalAwaiter Seconds(float duration)
        {
            var ins = GetPooled();

            try
            {
                return ins;
            }
            finally
            {
                _ = WaitSeconds(duration, ins, false);
            }
        }
        /// <summary>
        /// Waits for n duration in seconds.
        /// </summary>
        /// <param name="duration"></param>
        public static SignalAwaiter SecondsRealtime(float duration)
        {
            var ins = GetPooled();

            try
            {
                return ins;
            }
            finally
            {
                _ = WaitSeconds(duration, ins, true);
            }
        }
        /// <summary>
        /// Waits for n duration in seconds.
        /// </summary>
        /// <param name="duration"></param>
        public static SignalAwaiter Seconds(float duration, int setId)
        {
            if (setId < 0)
            {
                throw new Exception("VAwait Error : Id can't be negative number");
            }

            var ins = GetPooled();
            ins.GetSetId = setId;
            setIdd.TryAdd(setId, ins);

            try
            {
                return ins;
            }
            finally
            {
                _ = WaitSeconds(duration, ins, false);
            }
        }
        /// <summary>
        /// Waits for n duration in unscaledTime in seconds.
        /// </summary>
        /// <param name="duration"></param>
        public static SignalAwaiter SecondsRealtime(float duration, int setId)
        {
            if (setId < 0)
            {
                throw new Exception("VAwait Error : Id can't be negative number");
            }

            var ins = GetPooled();
            ins.GetSetId = setId;
            setIdd.TryAdd(setId, ins);

            try
            {
                return ins;
            }
            finally
            {
                _ = WaitSeconds(duration, ins, true);
            }
        }
        /// <summary>
        /// Waits for coroutine.
        /// </summary>
        /// <param name="coroutine"></param>
        public static SignalAwaiter Coroutine(IEnumerator coroutine)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                throw new Exception("VAwait Error : Coroutine can't be used for edit mode.");
            }
#endif

            var ins = GetPooled();
            ins.AssignEnumerator(coroutine);

            try
            {
                return ins;
            }
            finally
            {
                runtimeInstance.component.TriggerCoroutine(coroutine, ins);
            }
        }
        /// <summary>
        /// Init.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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

        static async ValueTask WaitSeconds(float duration, SignalAwaiter signal, bool realtime)
        {
            if (!realtime)
            {
                var timeScale = Timing(out var val);
                var frame = NextFrame();

                if (timeScale)
                {
                    if (Mathf.Approximately(1, val))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(duration - 0.051f), GetToken);
                    }
                    else
                    {
                        var calcTime = duration + (duration * (1 - val));

                        if (Mathf.Approximately(0, val) || val < 0)
                        {
                            var reuse = NextFrameReusable();

                            await Task.Run(async () =>
                            {
                                while (Mathf.Approximately(0, Time.timeScale))
                                {
                                    await reuse;

                                    if (awaitTokenSource.IsCancellationRequested)
                                    {
                                        return;
                                    }
                                }
                            });
                        }
                        else if(val > 1)
                        {
                            var calc = duration - (duration * (val - 1f));
                            bool invalid = false;

                            if(calc == 0 || calc < 0)
                            {
                                invalid = true;
                            }

                            if(!invalid)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(calc), GetToken);

                                if (awaitTokenSource.IsCancellationRequested)
                                {
                                    return;
                                }
                            }

                            PlayerLoopUpdate.playerLoopUtil.QueueFixedUpdate(new SignalAwaiterReusable(awaitTokenSource));
                            return;
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromSeconds(calcTime), GetToken);

                            if (awaitTokenSource.IsCancellationRequested)
                            {
                                return;
                            }
                        }
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(duration - 0.051f), GetToken);
                }
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(duration - 0.051f), GetToken);
            }

            PlayerLoopUpdate.playerLoopUtil.QueueEndOfFrame(signal);
        }
        static bool Timing(out float value)
        {
            var time = Time.timeScale;

            if (Mathf.Approximately(time, 1f))
            {
                value = time;
                return false;
            }
            else
            {
                value = time;
                return true;
            }

        }
        public static double NormalizeTime(double value, double min, double max)
        {
            if (max == min)
            {
                // Handle edge case where all values are identical
                return 0.5;
            }

            // Apply normalization formula
            double normalizedValue = (value - min) / (max - min);

            // Ensure normalized value stays within 0 to 1 range
            return Math.Max(0, Math.Min(1, normalizedValue));
        }
        /// <summary>
        /// Waits until Predicate<bool> is True. Can't be awaited multiple times.
        /// </summary>
        /// <param name="predicate">Condition.</param>
        /// <param name="tokenSource">The token source.</param>
        static async Task<bool> WaitUntil(Predicate<bool> predicate, CancellationTokenSource tokenSource)
        {
            var frame = NextFrameReusable();

            while (predicate.Invoke(false))
            {
                await frame;

                if (tokenSource.IsCancellationRequested)
                {
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// Behaves similar to PeriodicTimer in c#. Tick count will increase the next frame.
        /// </summary>
        static async ValueTask PeriodicTimer(float interval, int maxTickCount, Action<int> tick, CancellationTokenSource tokenSource)
        {
            if (maxTickCount < 1)
            {
                throw new Exception("VAwait Error : maxTickCount can't be less than 1.");
            }

            int count = 0;
            var frame = NextFrameReusable();

            while (maxTickCount != count)
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), tokenSource.Token);

                if (tokenSource.IsCancellationRequested)
                {
                    var ins = GetPooled();

                    PlayerLoopUpdate.playerLoopUtil.QueueEndOfFrame(ins);
                    break;
                }

                await frame;

                if (awaitTokenSource.IsCancellationRequested)
                {
                    return;
                }

                count++;
                tick.Invoke(count);
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
            if (awaitTokenSource != null)
            {
                awaitTokenSource.Cancel();
                awaitTokenSource.Dispose();
                awaitTokenSource = null;
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
                return awaitTokenSource.Token;
            }
        }
        public static async Task<AwaitChain> TaskChain(Func<Task> func, CancellationTokenSource cts)
        {
            var chain = new AwaitChain();
            chain.cts = cts;
            await func();

            if (cts.IsCancellationRequested)
            {
                chain.completed = false;
                return chain;
            }

            chain.completed = true;
            return chain;
        }
        public static async Task<AwaitChain> TaskChain(SignalAwaiter func, CancellationTokenSource cts)
        {
            var chain = new AwaitChain();
            chain.cts = cts;
            await func;

            if (cts.IsCancellationRequested)
            {
                chain.completed = false;
                return chain;
            }

            chain.completed = true;
            return chain;
        }
        public static async Task<AwaitChain> Next(this Task<AwaitChain> signal, Func<Task> func)
        {
            await func();

            if (signal.Result.cts.IsCancellationRequested)
            {
                signal.Result.completed = false;
            }

            signal.Result.completed = true;
            return signal.Result;
        }
        public static async Task<AwaitChain> Next(this Task<AwaitChain> signal, Func<SignalAwaiter> func)
        {
            await func();

            if (signal.Result.cts.IsCancellationRequested)
            {
                signal.Result.completed = false;
            }

            signal.Result.completed = true;
            return signal.Result;
        }
        static async ValueTask Test()
        {
            await TaskChain(GG, new CancellationTokenSource()).Next(GG).Next(GG).Next(NextFrame);
        }
        static async Task GG()
        {
            await Task.Yield();
        }
        public static async Task<T> Await<T>(this T signal, T wait) where T : Task<T>
        {
            await wait;
            return wait;
        }
        /// <summary>
        /// Invokes on threadPool.
        /// </summary>
        /// <param name="func"></param>
        public static void InvokeOnThreadpool(Action func)
        {
            Task.Run(func);
        }
        /// <summary>
        /// Invokes on threadPool and await for completion. 
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static SignalAwaiter AwaitOnThreadpool(Func<Task> func)
        {
            var ins = GetPooled();

            try
            {
                return ins;
            }
            finally
            {
                Task.Run(async () =>
                {
                    await func.Invoke();
                    await EndOfFrame();
                    ins.TrySetResult(true);
                    ReturnAwaiterToPool(ins);
                });
            }
        }
    }

    public enum UPlayStateMode
    {
        PlayMode,
        None
    }
}