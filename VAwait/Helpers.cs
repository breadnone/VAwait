using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections;

namespace VAwait
{
    public interface IVSignal
    {
        public void AssignEnumerator(IEnumerator enumerator);
        public int GetSetId {get;set;}
    }
    public class SignalAwaiter : INotifyCompletion, IVSignal
    {
        private Action _continuation;
        private bool _result;
        public bool cancelled{get;private set;}
        public IEnumerator enumerator { get; private set; }
        int IVSignal.GetSetId{get;set;} = -1;

        public bool IsCompleted
        {
            get;
            private set;
        }

        void IVSignal.AssignEnumerator(System.Collections.IEnumerator enumerator)
        {
            this.enumerator = enumerator;
        }

        public bool GetResult()
        {
            return _result;
        }

        public void OnCompleted(Action continuation)
        {
            if (Check)
            {
                return;
            }

            if (_continuation != null)
                throw new InvalidOperationException("VAwait Error : Is already being awaited");

            _continuation = continuation;
        }
        bool Check
        {
            get{return cancelled || Wait.vawaitTokenSource == null || Wait.vawaitTokenSource.IsCancellationRequested;}
        }

        /// <summary>
        /// Attempts to transition the completion state.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TrySetResult(bool result)
        {
            if (Check)
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
            Cancel();
            this._result = false;
            this._continuation = null;
            this.IsCompleted = false;
            this.enumerator = null;
            this.cancelled = false;

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

        public void Cancel()
        {
            cancelled = true;

            if(enumerator != null)
            {
                Wait.GetRuntimeInstance().component.CancelCoroutine(enumerator);
                enumerator = null;
            }            
        }
    }
}