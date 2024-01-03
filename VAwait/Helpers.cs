using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace VAwait
{
    public interface IVSignal
    {
        public void AssignTokenSource(CancellationTokenSource tokenSource);
    }
    public sealed class SignalAwaiter<T> : INotifyCompletion, IVSignal
    {
        private Action _continuation = null;
        private T _result = default(T);
        private Exception _exception = null;
        public CancellationTokenSource tokenSource { get; private set; }
        public bool IsCompleted
        {
            get;
            private set;
        }
        void IVSignal.AssignTokenSource(CancellationTokenSource tokenSource)
        {
            this.tokenSource = tokenSource;
        }

        public T GetResult()
        {
            if (tokenSource.IsCancellationRequested)
            {
                return default(T);
            }

            if (_exception != null)
                throw _exception;
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
        public bool TrySetResult(T result)
        {
            if (tokenSource.IsCancellationRequested)
            {
                return false;
            }

            if (!this.IsCompleted)
            {
                this.IsCompleted = true;
                this._result = result;

                if (_continuation != null)
                {
                    _continuation();
                }

                tokenSource = new();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to transition the exception state.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TrySetException(Exception exception)
        {
            if(tokenSource.IsCancellationRequested)
            {
                return false;
            }
            
            if (!this.IsCompleted)
            {
                this.IsCompleted = true;
                this._exception = exception;

                if (_continuation != null)
                    _continuation();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Reset the awaiter to initial status
        /// </summary>
        /// <returns></returns>
        public SignalAwaiter<T> Reset(bool cancelToken, bool dispose)
        {
            this._result = default(T);
            this._continuation = null;
            this._exception = null;
            this.IsCompleted = false;
            Cancel(cancelToken, dispose);

            return this;
        }

        public SignalAwaiter<T> GetAwaiter()
        {
            return this;
        }
        public void Signal(T state)
        {
            this.TrySetResult(state);
        }
        public SignalAwaiter<T> WaitForSignal(Action func)
        {
            if(tokenSource.IsCancellationRequested)
            {
                return this;
            }

            func.Invoke();
            return this;
        }
        public void Cancel(bool renewTokenSource, bool dispose)
        {
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