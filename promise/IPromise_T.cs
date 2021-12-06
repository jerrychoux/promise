using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace cn.jerrychoux.promise {
    public interface IPromise<T> : IPromise {
        new TaskAwaiter<T> GetAwaiter();

        IPromise Then(Action<T> onFulfilled, Action<Exception> onRejected);
        IPromise Then(Action<T> onFulfilled);
        IPromise Then(Func<T, Task> onFulfilled, Action<Exception> onRejected);
        IPromise Then(Func<T, Task> onFulfilled);
        IPromise Then(Func<T, IPromise> onFulfilled, Action<Exception> onRejected);
        IPromise Then(Func<T, IPromise> onFulfilled);
        IPromise<TResult> Then<TResult>(Func<T, TResult> onFulfilled, Action<Exception> onRejected);
        IPromise<TResult> Then<TResult>(Func<T, TResult> onFulfilled);
        IPromise<TResult> Then<TResult>(Func<T, Task<TResult>> onFulfilled, Action<Exception> onRejected);
        IPromise<TResult> Then<TResult>(Func<T, Task<TResult>> onFulfilled);
        IPromise<TResult> Then<TResult>(Func<T, IPromise<TResult>> onFulfilled, Action<Exception> onRejected);
        IPromise<TResult> Then<TResult>(Func<T, IPromise<TResult>> onFulfilled);

        new IPromise<T> Finally(Action onFinal);
    }
}