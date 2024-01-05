using System.Collections;
using UnityEngine;
using System.Threading;
using System;
using System.Collections.Generic;

namespace VAwait
{
    [AddComponentMenu("")]
    public class VWaitComponent : MonoBehaviour
    {
        static VWaitComponent mono { get; set; }
        WaitForFixedUpdate waitFixed;
        WaitForEndOfFrame endOfFrame;
        WaitForSecondsRealtime realtime;
        void Awake()
        {
            waitFixed = new WaitForFixedUpdate();
            endOfFrame = new WaitForEndOfFrame();
            DontDestroyOnLoad(this.gameObject);
        }

        void Start()
        {
            if (mono == null)
            {
                mono = this;
            }
            else
            {
                GameObject.Destroy(this);
            }
        }
        IEnumerator InstanceEndFrame(SignalAwaiter signal)
        {
            yield return endOfFrame;
            signal.TrySetResult(true);
            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutineFrame(SignalAwaiter signal)
        {
            yield return null;
            signal.TrySetResult(true);
            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutineFixedUpdate(SignalAwaiter signal)
        {
            yield return waitFixed;
            signal.TrySetResult(true);
            Wait.ReturnAwaiterToPool(signal);
        }
        IEnumerator InstanceCoroutineSecondsReusable(WaitForSeconds wait, SignalAwaiterReusable signal)
        {
            yield return wait;
            signal.TrySetResult(true);
        }
        IEnumerator InstanceCoroutine(IEnumerator coroutine, SignalAwaiter signal)
        {
            yield return coroutine;
            signal.TrySetResult(true);    
            Wait.ReturnAwaiterToPool(signal);
        }
        public void TriggerFixedUpdateCoroutine(SignalAwaiter signal)
        {
            StartCoroutine(InstanceCoroutineFixedUpdate(signal));
        }
        public void TriggerFrameCoroutine(SignalAwaiter signal)
        {
            StartCoroutine(InstanceCoroutineFrame(signal));
        }
        public void TriggerEndFrame(SignalAwaiter signal)
        {
            StartCoroutine(InstanceEndFrame(signal));
        }
        public void TriggerSecondsCoroutineReusable(WaitForSeconds wait, SignalAwaiterReusable signal)
        {
            signal.AssignEnumerator(InstanceCoroutineSecondsReusable(wait, signal));
            StartCoroutine(signal.enumerator);
        }
        public void TriggerCoroutine(IEnumerator coroutine, SignalAwaiter signal)
        {
            StartCoroutine(InstanceCoroutine(coroutine, signal));
        }

        public void CancelCoroutines()
        {
            StopAllCoroutines();
        }
        public void CancelCoroutine(IEnumerator ienumerator)
        {
            StopCoroutine(ienumerator);
        }
        void OnApplicationQuit()
        {
            Wait.DestroyAwaits();
            CancelCoroutines();
        }
    }
}