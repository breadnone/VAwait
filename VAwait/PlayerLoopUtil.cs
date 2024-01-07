using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;
using System.Threading;

namespace VAwait
{
    public sealed class AwaitUpdate
    {

    }
    public sealed class AwaitEndOfFrame
    {

    }
    public class PlayerLoopUpdate
    {
        PlayerLoopSystem playerLoop;
        PlayerLoopSystem updateLoop;
        public static PlayerLoopUpdate playerLoopUtil { get; private set; }
        static ConcurrentQueue<SignalAwaiter> signalQueue = new();
        static ConcurrentQueue<SignalAwaiter> signalEndOfFrameQueue = new();
        static ConcurrentQueue<SignalAwaiterReusableFrame> signalQueueReusableFrame = new();
        public static Thread IsMainthread {get;private set;}
        public PlayerLoopUpdate()
        {
            if(playerLoopUtil == null)
            {
                Application.wantsToQuit -= OnQuit;
                Application.wantsToQuit += OnQuit;
                playerLoopUtil = this;
                AssignPlayerLoop(true);
            }
        }
        void AssignPlayerLoop(bool addElseRemove)
        {
            playerLoop = PlayerLoop.GetDefaultPlayerLoop();
            var update = GetUpdate(playerLoop);

            if(addElseRemove)
            {
                var customUpdate = new PlayerLoopSystem()
                {
                    updateDelegate = UpdateRun,
                    type = typeof(AwaitUpdate)
                };

                var customEndOfFrameUpdate = new PlayerLoopSystem()
                {
                    updateDelegate = EndFrameUpdateRun,
                    type = typeof(AwaitEndOfFrame)
                };

                var copy = ReplaceUpdateRoot(ref playerLoop, ref customUpdate, true);
                var end = ReplaceEndOfFrameRoot(ref copy, ref customEndOfFrameUpdate, true);
                PlayerLoop.SetPlayerLoop(end);
            }
            else
            {
                var dummy = new PlayerLoopSystem();
                var dummyEnd = new PlayerLoopSystem();
                var copy = ReplaceUpdateRoot(ref playerLoop, ref dummy, false);
                var end = ReplaceEndOfFrameRoot(ref copy, ref dummyEnd, false);
                PlayerLoop.SetPlayerLoop(end);
            }
        }
        bool OnQuit()
        {
            AssignPlayerLoop(false);
            Debug.Log("WE QUIT HERE BOII");
            return true;
        }
        PlayerLoopSystem GetUpdate(PlayerLoopSystem loopSystem)
        {
            for (int i = 0; i < loopSystem.subSystemList.Length; i++)
            {
                if (loopSystem.subSystemList[i].type == typeof(Update))
                {
                    for (int j = 0; j < loopSystem.subSystemList[i].subSystemList.Length; j++)
                    {
                        if (loopSystem.subSystemList[i].subSystemList[j].type == typeof(Update.ScriptRunBehaviourUpdate))
                        {
                            return loopSystem.subSystemList[i].subSystemList[j];
                        }
                    }
                }
            }

            return default;
        }
        PlayerLoopSystem ReplaceUpdateRoot(ref PlayerLoopSystem root, ref PlayerLoopSystem custom, bool addCustomUpdateElseClear)
        {
            var lis = root.subSystemList.ToList();
            int? index = null;

            for (int i = 0; i < root.subSystemList.Length; i++)
            {
                if (lis[i].type == typeof(Update))
                {
                    index = i;
                    break;
                }
            }

            if (index.HasValue)
            {
                var tmp = root.subSystemList[index.Value].subSystemList.ToList();

                for (int i = tmp.Count; i-- > 0;)
                {
                    if (tmp[i].type == typeof(AwaitUpdate))
                    {
                        tmp.Remove(tmp[i]);
                    }
                }

                if (addCustomUpdateElseClear)
                {
                    //1 is after script Update.
                    tmp.Insert(1, custom);
                }

                root.subSystemList[index.Value].subSystemList = tmp.ToArray();
            }

            return root;
        }
        PlayerLoopSystem ReplaceEndOfFrameRoot(ref PlayerLoopSystem root, ref PlayerLoopSystem custom, bool addCustomUpdateElseClear)
        {
            var lis = root.subSystemList.ToList();
            int? index = null;

            for (int i = 0; i < root.subSystemList.Length; i++)
            {
                if (lis[i].type == typeof(PostLateUpdate))
                {
                    index = i;
                    break;
                }
            }

            if (index.HasValue)
            {
                var tmp = root.subSystemList[index.Value].subSystemList.ToList();

                for (int i = tmp.Count; i-- > 0;)
                {
                    if (tmp[i].type == typeof(AwaitEndOfFrame))
                    {
                        tmp.Remove(tmp[i]);
                    }
                }

                if (addCustomUpdateElseClear)
                {
                    //1 is after script Update.
                    tmp.Add(custom);
                }

                root.subSystemList[index.Value].subSystemList = tmp.ToArray();
            }

            return root;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (PlayerLoopUpdate.playerLoopUtil == null)
            {
                PlayerLoopUpdate.playerLoopUtil = new PlayerLoopUpdate();
            }
        }
        bool queueIsRunning = false;
        bool queueEndFrameRunning = false;
        Queue<SignalAwaiter> sigs = new(15);
        Queue<SignalAwaiter> sigsEndOfFrame = new(15);
        Queue<SignalAwaiterReusableFrame> sigsReusableFrame = new(15);
        void EndFrameUpdateRun()
        {
            if(queueEndFrameRunning)
            {
                return;
            }

            queueEndFrameRunning = true;

            while(signalEndOfFrameQueue.TryDequeue(out var signal))
            {
                if(signal.frameIn < Time.frameCount)
                {
                    signal.TrySetResult(true);
                    Wait.ReturnAwaiterToPool(signal);
                }
                else
                {
                    sigsEndOfFrame.Enqueue(signal);
                }
            }

            while(sigsEndOfFrame.Count > 0)
            {
                signalEndOfFrameQueue.Enqueue(sigsEndOfFrame.Dequeue());
            }

            queueEndFrameRunning = false;
        }
        void UpdateRun()
        {
            if(queueIsRunning)
            {
                return;
            }

            queueIsRunning = true;

            while(signalQueue.TryDequeue(out var signal))
            {
                if(signal.frameIn < Time.frameCount)
                {
                    signal.TrySetResult(true);
                    Wait.ReturnAwaiterToPool(signal);
                }
                else
                {
                    sigs.Enqueue(signal);
                }
            }

            while(signalQueueReusableFrame.TryDequeue(out var signal))
            {
                if(signal.frameIn < Time.frameCount)
                {
                    signal.TrySetResult(true);
                }
                else
                {
                    sigsReusableFrame.Enqueue(signal);
                }
            }
            
            while(sigs.Count > 0)
            {
                signalQueue.Enqueue(sigs.Dequeue());
            }

            while(sigsReusableFrame.Count > 0)
            {
                signalQueueReusableFrame.Enqueue(sigsReusableFrame.Dequeue());
            }

            queueIsRunning = false;
        }
        public void QueueNextFrame(SignalAwaiter signal)
        {
            signal.frameIn = Time.frameCount;

            if(!queueIsRunning)
            {
                signalQueue.Enqueue(signal);
            }
            else
            {
                sigs.Enqueue(signal);
            }
        }
        public void QueueEndOfFrame(SignalAwaiter signal)
        {
            signal.frameIn = Time.frameCount;

            if(!queueEndFrameRunning)
            {
                signalEndOfFrameQueue.Enqueue(signal);
            }
            else
            {
                sigsEndOfFrame.Enqueue(signal);
            }
        }
        public void QueueReusableNextFrame(SignalAwaiterReusableFrame signal)
        {
            signal.frameIn = Time.frameCount;

            if(!queueIsRunning)
            {
                signalQueueReusableFrame.Enqueue(signal);
            }
            else
            {
                sigsReusableFrame.Enqueue(signal);
            }
        }
        private static PlayerLoopSystem FindSubSystem<T>(PlayerLoopSystem def)
        {
            if (def.type == typeof(T))
            {
                return def;
            }
            if (def.subSystemList != null)
            {
                foreach (var s in def.subSystemList)
                {
                    var system = FindSubSystem<Update.ScriptRunBehaviourUpdate>(s);
                    if (system.type == typeof(T))
                    {
                        return system;
                    }
                }
            }
            return default(PlayerLoopSystem);
        }
    }
}