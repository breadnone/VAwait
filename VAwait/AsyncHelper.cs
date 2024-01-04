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
        static ConcurrentQueue<SignalAwaiter> signalPool;
        static Dictionary<int, SignalAwaiter> setIdd = new();
        public static UPlayStateMode playMode {get;set;} = UPlayStateMode.None;
        public static int poolLength {get;set;} = 15;
        public static SynchronizationContext unityContext{get;set;}
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
            signalPool = new ConcurrentQueue<SignalAwaiter>();

            for(int i = 0; i < poolLength; i++)
            {
                var ctoken = CancellationTokenSource.CreateLinkedTokenSource(vawaitTokenSource.Token);
                var ins = new SignalAwaiter();
                (ins as IVSignal).AssignTokenSource(ctoken);
                signalPool.Enqueue(ins);
            }
        }
        /// <summary>
        /// Gets an instance of SignalAwaiter from the pool.
        /// </summary>
        /// <returns></returns>
        static SignalAwaiter GetPooled()
        {
            if(signalPool.TryDequeue(out var ins))
            {
                return ins;
            }

            var nins = new SignalAwaiter();
            var ctoken = CancellationTokenSource.CreateLinkedTokenSource(vawaitTokenSource.Token);
            (ins as IVSignal).AssignTokenSource(ctoken);
            return nins;
        }
        /// <summary>
        /// Returns back to pool.
        /// </summary>
        /// <param name="signal"></param>
        public static void ReturnAwaiterToPool(SignalAwaiter signal)
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
        public static SignalAwaiter NextFrame()
        {
            var ins = GetPooled();
            runtimeInstance.component.TriggerFrameCoroutine(ins, ins.tokenSource);
            return ins;
        }
        /// <summary>
        /// Fixed 1 frame value meant to be used for frame waiting while in edit-mode. This is not accurate, just a very rough estimation based on screen's refresh rate.
        /// </summary>
        /// <returns></returns>
        public static double OneFrameFixed()
        {
            var refValue = Screen.currentResolution.refreshRateRatio.value;
            return (1d / refValue) * 1000;
        }
        /// <summary>
        /// Waits for n duration in seconds.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static SignalAwaiter Seconds (float duration)
        {
            var ins = GetPooled();
            _= WaitSeconds(duration, ins);
            return ins;
        }
        /// <summary>
        /// Waits for n duration in seconds.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static SignalAwaiter Seconds (float duration, int setId)
        {
            if(setId < 0)
            {
                throw new Exception("VAwait Error : Id can't be negative number");
            }

            var ins = GetPooled();
            (ins as IVSignal).GetSetId = setId;

            setIdd.TryAdd(setId, ins);
            _= WaitSeconds(duration, ins);
            return ins;
        }
        /// <summary>
        /// Waits for coroutine.
        /// </summary>
        /// <param name="coroutine"></param>
        /// <returns></returns>
        public static SignalAwaiter Coroutine (IEnumerator coroutine)
        {
            var ins = GetPooled();
            
            (ins as IVSignal).AssignEnumerator(coroutine);
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
        static async ValueTask WaitSeconds(float duration, SignalAwaiter signal)
        {
            await Task.Delay(TimeSpan.FromSeconds(duration), signal.tokenSource.Token);

            if(signal.tokenSource.IsCancellationRequested)
            {
                return;
            }

            runtimeInstance.component.TriggerFrameCoroutine(signal, signal.tokenSource);
        }
        /// <summary>
        /// Runs and switch to threadPool.
        /// </summary>
        /// <param name="func">Delegate.</param>
        public static void RunOnThreadpool(Action func)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                func.Invoke();
            });
        }
        /// <summary>
        /// Runs and awaits from threadPool.
        /// </summary>
        /// <param name="func">Delegate.</param>
        public static SignalAwaiter RunOnThreadpool(Action<bool> func)
        {
            var ins = GetPooled();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                func?.Invoke(true);
                runtimeInstance.component.TriggerFrameCoroutine(ins, ins.tokenSource);
            });

            return ins;
        }
        /// <summary>
        /// Invokes on mainthread. Can be used to get out of threadPool.
        /// </summary>
        /// <param name="func">Delegate.</param>
        public static SignalAwaiter BeginInvokeOnMainthread(Action func)
        {
            var ins = GetPooled();

            // Send a callback to the main thread
            unityContext.Post(_ =>
            {
                func?.Invoke();
                runtimeInstance.component.TriggerFrameCoroutine(ins, ins.tokenSource);
            }, null);
            return ins;
        }

        /// <summary>
        /// Cancels an await.
        /// </summary>
        public static void Cancel(this SignalAwaiter signal, int id)
        {
            bool wasFound = false;

            foreach(var ins in signalPool)
            {
                if(id > -1 && (ins as IVSignal).GetSetId == id)
                {
                    wasFound = true;
                    break;
                }
            }
            
            if(!wasFound)
            {
                ReturnAwaiterToPool(signal);
            }
        }
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
            if(vawaitTokenSource != null)
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
    }

    public enum UPlayStateMode
    {
        PlayMode,
        None
    }
}