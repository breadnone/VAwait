using System;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace VAwait
{
    public enum VWaitType
    {
        Frame,
        WaitSeconds,
        WaitSecondsRealtime
    }
    /// <summary>
    /// Awaiter class.
    /// </summary>
    public class SignalAwaiter : ICriticalNotifyCompletion
    {
        private Action _continuation;
        private bool _result;
        readonly CancellationToken token;
        public bool cancelled { get; private set; }
        public IEnumerator enumerator { get; private set; }
        public int GetSetId { get; set; } = -1;
        public int frameIn {get;set;}
        public bool IsCompleted
        {
            get;
            private set;
        }

        public SignalAwaiter(CancellationTokenSource cts)
        {
            token = cts.Token;
        }

        public void AssignEnumerator(System.Collections.IEnumerator enumerator)
        {
            this.enumerator = enumerator;
        }

        public bool GetResult()
        {
            return _result;
        }
 
        public void OnCompleted(Action continuation)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (_continuation != null)
                throw new InvalidOperationException("VAwait Error : Is already being awaited");
            _continuation = continuation;
        }

        /// <summary>
        /// Attempts to transition the completion state.
        /// </summary>
        public bool TrySetResult(bool result)
        {
            if (cancelled || token.IsCancellationRequested)
            {
                return false;
            }

            if (!this.IsCompleted)
            {
                this.IsCompleted = true;
                this._result = result;

                _continuation?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset the awaiter to initial status
        /// </summary>
        public SignalAwaiter Reset()
        {
            if (token.IsCancellationRequested)
            {
                return this;
            }

            Cancel();
            this._result = false;
            this._continuation = null;
            this.IsCompleted = false;
            this.cancelled = false;
            return this;
        }

        public SignalAwaiter GetAwaiter()
        {           
            return this;
        }

        public void Cancel()
        {
            cancelled = true;

            if (enumerator != null)
            {
                Wait.GetRuntimeInstance().component.CancelCoroutine(enumerator);
                enumerator = null;
            }

            if (GetSetId > -1)
            {
                Wait.RemoveIDD(GetSetId);
                GetSetId = -1;
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _continuation = continuation;
        }
    }

    public class SignalAwaiterReusableFrame : ICriticalNotifyCompletion
    {
        private Action _continuation;
        private bool _result;
        readonly CancellationToken token;
        public int frameIn {get;set;}
        public bool IsCompleted
        {
            get;
            private set;
        }

        public SignalAwaiterReusableFrame(CancellationTokenSource cts)
        {
            token = cts.Token;
        }

        public bool GetResult()
        {
            return _result;
        }

        public void OnCompleted(Action continuation)
        {
            if (_continuation != null)
                throw new InvalidOperationException("VAwait Error : Is already being awaited");

            _continuation = continuation;
        }

        /// <summary>
        /// Attempts to transition the completion state.
        /// </summary>
        public bool TrySetResult(bool result)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }

            if (!this.IsCompleted)
            {
                this.IsCompleted = true;
                this._result = result;
                _continuation?.Invoke();
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset the awaiter to initial status
        /// </summary>
        public void Reset()
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            this._result = false;
            this._continuation = null;
            this.IsCompleted = false;
        }
        public SignalAwaiterReusableFrame GetAwaiter()
        {            
            if(frameIn != PlayerLoopUpdate.playerLoopUtil.GetCurrentFrame())
            {
                Reset();
                PlayerLoopUpdate.playerLoopUtil.QueueReusableNextFrame(this);
            }

            return this;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _continuation = continuation;
        }
        public void Cancel()
        {
            Reset();
        }
    }
}