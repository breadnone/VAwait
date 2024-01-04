using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

//Async await helper.
namespace VAwait.Editor
{
    public static class WaitEditor
    {
        public static CancellationTokenSource vawaitTokenSource {get; set;}
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
        public static async Task<SignalAwaiter> NextFrame()
        {
            var ins = GetPooled();
            await Task.Delay(TimeSpan.FromMilliseconds(EditorUtilV.EditorDeltaTM.Time), ins.tokenSource.Token);
            
            if(ins.tokenSource.IsCancellationRequested)
            {
                return ins;
            }

            ins.TrySetResult(true);
            return ins;
        }

        /// <summary>
        /// Waits for n duration in seconds.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static async Task<SignalAwaiter> Seconds (float duration)
        {
            var ins = GetPooled();
            await Task.Delay(TimeSpan.FromSeconds(duration), ins.tokenSource.Token);
            ins.TrySetResult(true);
            return ins;
        }
        /// <summary>
        /// Waits for n duration in seconds.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static async Task<SignalAwaiter> Seconds (float duration, int setId)
        {
            if(setId < 0)
            {
                throw new Exception("VAwait Error : Id can't be negative number");
            }

            var ins = GetPooled();
            (ins as IVSignal).GetSetId = setId;

            setIdd.TryAdd(setId, ins);
            await Task.Delay(TimeSpan.FromSeconds(duration), ins.tokenSource.Token);
            return ins;
        }

        /// <summary>
        /// Init.
        /// </summary>
        public static void StartAwait()
        {
            PrepareAsyncHelper();
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
    }
}