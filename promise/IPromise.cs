using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace cn.jerrychoux.promise {
    public enum PromiseState {
        Pending,
        Fulfilled,
        Rejected,
    }

    public interface IPromise {
        TaskAwaiter GetAwaiter();

        IPromise Then(Action onFulfilled, Action<Exception> onRejected);
        IPromise Then(Action onFulfilled);
        IPromise Then(Func<Task> onFulfilled, Action<Exception> onRejected);
        IPromise Then(Func<Task> onFulfilled);
        IPromise Then(Func<IPromise> onFulfilled, Action<Exception> onRejected);
        IPromise Then(Func<IPromise> onFulfilled);
        IPromise<TResult> Then<TResult>(Func<TResult> onFulfilled, Action<Exception> onRejected);
        IPromise<TResult> Then<TResult>(Func<TResult> onFulfilled);
        IPromise<TResult> Then<TResult>(Func<Task<TResult>> onFulfilled, Action<Exception> onRejected);
        IPromise<TResult> Then<TResult>(Func<Task<TResult>> onFulfilled);
        IPromise<TResult> Then<TResult>(Func<IPromise<TResult>> onFulfilled, Action<Exception> onRejected);
        IPromise<TResult> Then<TResult>(Func<IPromise<TResult>> onFulfilled);

        IPromise Catch(Action<Exception> onError);
        IPromise Finally(Action onFinal);
    }
}