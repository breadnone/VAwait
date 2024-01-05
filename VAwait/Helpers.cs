using System;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Threading;

namespace VAwait
{
    /// <summary>
    /// Awaiter class.
    /// </summary>/.
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
        /// <param name="result"></param>
        /// <returns></returns>
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
        /// <returns></returns>
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
    /// </summary>/.
    public class SignalAwaiterReusable : ICriticalNotifyCompletion
    {
        private Action _continuation;
        private bool _result;
        readonly CancellationToken token;
        public IEnumerator enumerator;
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
        /// <param name="result"></param>
        /// <returns></returns>
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
        /// <returns></returns>
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
                Wait.runtimeInstance.component.TriggerFrameCoroutineReusable(this);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _continuation = continuation;
        }
    }
}