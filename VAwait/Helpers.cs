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

    /// <summary>
    /// Awaiter class.
    /// </summary>
    public class SignalAwaiterReusable : ICriticalNotifyCompletion
    {
        private Action _continuation;
        private bool _result;
        readonly CancellationToken token;
        public IEnumerator enumerator;
        public WaitForSeconds wait {get;set;}
        public WaitForSecondsRealtime waitRealtime {get;set;}
        public VWaitType waitType {get;set;}
        public void AssignEnumerator(System.Collections.IEnumerator enumerator)
        {
            this.enumerator = enumerator;
        }
        public bool IsCompleted
        {
            get;
            private set;
        }

        public SignalAwaiterReusable(CancellationTokenSource cts)
        {
            token = cts.Token;
            this.waitType = waitType;
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
            if (token.IsCancellationRequested)
            {
                return false;
            }

            if (!this.IsCompleted)
            {
                this.IsCompleted = true;
                this._result = result;
                enumerator = null;
                _continuation?.Invoke();
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset the awaiter to initial status
        /// </summary>
        public SignalAwaiterReusable Reset()
        {
            if (token.IsCancellationRequested)
            {
                return this;
            }

            this._result = false;
            this._continuation = null;
            this.IsCompleted = false;
            
            if(enumerator != null)
            {
                Wait.runtimeInstance.component.StopCoroutine(enumerator);
                enumerator = null;
            }
            
            return this;
        }
        public SignalAwaiterReusable GetAwaiter()
        {            
            try
            {
                return Reset();
            }
            finally
            {
                if(!token.IsCancellationRequested)
                {
                    if(waitType == VWaitType.WaitSeconds)
                    {
                        Wait.runtimeInstance.component.TriggerSecondsCoroutineReusable(wait, this);
                    }
                    else
                    {
                        Wait.runtimeInstance.component.TriggerSecondsCoroutineReusableRealtime(waitRealtime, this);
                    }
                }
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _continuation = continuation;
        }
        public void Cancel()
        {
            if(enumerator != null)
            {
                Wait.runtimeInstance.component.CancelCoroutine(enumerator);
            }

            Reset();
        }
    }

    public class SignalAwaiterReusableFrame : ICriticalNotifyCompletion
    {
        private Action _continuation;
        private bool _result;
        readonly CancellationToken token;
        public IEnumerator enumerator;
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
            if (token.IsCancellationRequested)
            {
                return false;
            }

            if (!this.IsCompleted)
            {
                this.IsCompleted = true;
                this._result = result;
                enumerator = null;
                _continuation?.Invoke();
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset the awaiter to initial status
        /// </summary>
        public SignalAwaiterReusableFrame Reset()
        {
            if (token.IsCancellationRequested)
            {
                return this;
            }

            this._result = false;
            this._continuation = null;
            this.IsCompleted = false;
            enumerator = null;
            return this;
        }
        public SignalAwaiterReusableFrame GetAwaiter()
        {            
            try
            {
                return Reset();
            }
            finally
            {
                if(!token.IsCancellationRequested)
                {
                    Wait.runtimeInstance.component.TriggerFrameCoroutineReusable(this);
                }
            }
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