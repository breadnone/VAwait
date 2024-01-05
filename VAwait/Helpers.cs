using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections;

namespace VAwait
{
    public interface IVSignal
    {
        public void AssignTokenSource(CancellationTokenSource tokenSource);
        public void AssignEnumerator(IEnumerator enumerator);
        public int GetSetId {get;set;}
    }
    public class SignalAwaiter : INotifyCompletion, IVSignal
    {
        private Action _continuation;
        private bool _result;
        public CancellationTokenSource tokenSource { get; private set; }
        public IEnumerator enumerator { get; private set; }
        int IVSignal.GetSetId{get;set;} = -1;

        public bool IsCompleted
        {
            get;
            private set;
        }

        void IVSignal.AssignTokenSource(CancellationTokenSource tokenSource)
        {
            this.tokenSource = tokenSource;
        }

        void IVSignal.AssignEnumerator(System.Collections.IEnumerator enumerator)
        {
            this.enumerator = enumerator;
        }

        public bool GetResult()
        {
            if (tokenSource.IsCancellationRequested)
            {
                return false;
            }

            return _result;
        }

        public void OnCompleted(Action continuation)
        {
            if (tokenSource.IsCancellationRequested)
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
            if (tokenSource.IsCancellationRequested)
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
        public SignalAwaiter Reset(bool cancelToken, bool dispose)
        {
            Cancel(cancelToken, dispose);
            this._result = false;
            this._continuation = null;
            this.IsCompleted = false;
            this.enumerator = null;

            var isig = this as IVSignal;

            if(isig.GetSetId > -1)
            {
                Wait.RemoveIDD(isig.GetSetId);
                isig.GetSetId = -1;
            }
            
            return this;
        }

        public SignalAwaiter GetAwaiter()
        {
            return this;
        }

        public void Cancel(bool renewTokenSource, bool dispose, bool reset = false)
        {
            if(enumerator != null)
            {
                Wait.GetRuntimeInstance().component.CancelCoroutine(enumerator);
                enumerator = null;
            }

            if (dispose)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }

            if (renewTokenSource)
            {
                tokenSource = CancellationTokenSource.CreateLinkedTokenSource(Wait.vawaitTokenSource.Token);
            }
        }
    }
}