using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace cn.jerrychoux.promise {
    public class Promise<T> : Promise, IPromise<T> {
        #region Field
        Task<T>? BackingTask { get; set; }
        #endregion

        #region Constructor
        public Promise(Task<T> task) => BackingTask = task ?? throw new ArgumentNullException(nameof(task), "Must provide a task for a new promise.");
        public Promise(Action<Action<T>, Action<Exception>> callback) {
            ValidNonNull(callback, nameof(callback));

            var builder = new AsyncTaskMethodBuilder<T>();
            BackingTask = builder.Task;

            try {
                callback(arg => builder.SetResult(arg), ex => builder.SetException(ex));
            } catch (System.Exception ex) {
                builder.SetException(ex);
            }
        }
        public Promise(Action<Action<T>> callback) : this(callback == null ? null! : (resolve, _) => callback.Invoke(resolve)) { }
        #endregion

        #region Utils
        public static IPromise<T> Resolve(T arg) => new Promise<T>(Task<T>.FromResult(arg));
        public new static IPromise<T> Reject(Exception ex) => new Promise<T>(Task<T>.FromException<T>(ex));
        TaskAwaiter<T> IPromise<T>.GetAwaiter() => BackingTask!.GetAwaiter();
        public static implicit operator Task<T>(Promise<T> promise) => promise.BackingTask!;
        public static implicit operator Promise<T>(Task<T> task) => new Promise<T>(task);
        #endregion

        #region All
        public static IPromise<T[]> All(params Task<T>[] tasks) {
            ValidNonNull(tasks, nameof(tasks));

            if (tasks.Length == 0) return Promise<T[]>.Resolve(Enumerable.Empty<T>().ToArray());

            return new Promise<T[]>(Task<T>.WhenAll(tasks));
        }
        public static IPromise<T[]> All(IEnumerable<Task<T>> tasks) => All(tasks == null ? null! : tasks.ToArray());
        public static IPromise<T[]> All(params IPromise<T>[] promises) {
            ValidNonNull(promises, nameof(promises));

            if (promises.Length == 0) return Promise<T[]>.Resolve(Enumerable.Empty<T>().ToArray());

            return All(promises.Select(async p => await p));
        }
        public static IPromise<T[]> All(IEnumerable<IPromise<T>> promises) => All(promises == null ? null! : promises.ToArray());
        #endregion

        #region Any
        public static IPromise<T> Any(params Task<T>[] tasks) {
            ValidNonNull(tasks, nameof(tasks));

            if (tasks.Length == 0) return Resolve(default!);

            var builder = new TaskCompletionSource<T>();
            var builders = new List<TaskCompletionSource<T>>(tasks.Length);
            foreach (var task in tasks) {
                builders.Add(new TaskCompletionSource<T>());
            }

            int index = -1;
            foreach (var task in tasks) {
                task.ContinueWith(t => {
                    int i = Interlocked.Increment(ref index);
                    if (t.IsFaulted) {
                        builders[i].SetException(t.Exception!);
                    } else {
                        builders[i].SetResult(t.Result);
                        builder.TrySetResult(t.Result);
                    }
                });
            }

            Task.WhenAll(builders.Select(s => s.Task)).ContinueWith(t => {
                var faultedBuilder = builders.FirstOrDefault(s => s.Task.IsFaulted);
                if (faultedBuilder != null) {
                    builder.SetException(faultedBuilder.Task.Exception!);
                }
            });

            return new Promise<T>(builder.Task);
        }
        public static IPromise<T> Any(IEnumerable<Task<T>> tasks) => Any(tasks == null ? null! : tasks.ToArray());
        public static IPromise<T> Any(params IPromise<T>[] promises) {
            ValidNonNull(promises, nameof(promises));

            return Any(promises.Select(async p => await p));
        }
        public static IPromise<T> Any(IEnumerable<IPromise<T>> promises) => Any(promises == null ? null! : promises.ToArray());
        #endregion

        #region Race
        public static IPromise<T> Race(params Task<T>[] tasks) {
            ValidNonNull(tasks, nameof(tasks));

            if (tasks.Length == 0) return Resolve(default!);

            var builder = new TaskCompletionSource<T>();

            foreach (Task<T> task in tasks) {
                task.ContinueWith(t => {
                    if (t.IsFaulted) {
                        builder.TrySetException(t.Exception!);
                    } else {
                        builder.TrySetResult(t.Result);
                    }
                });
            }

            return new Promise<T>(builder.Task);
        }
        public static IPromise<T> Race(IEnumerable<Task<T>> tasks) => Race(tasks == null ? null! : tasks.ToArray());
        public static IPromise<T> Race(params IPromise<T>[] promises) {
            ValidNonNull(promises, nameof(promises));

            return Race(promises.Select(async p => await p));
        }
        public static IPromise<T> Race(IEnumerable<IPromise<T>> promises) => Race(promises == null ? null! : promises.ToArray());
        #endregion

        #region Then
        public IPromise Then(Action<T> onFulfilled, Action<Exception> onRejected) {
            ValidNonNull(onFulfilled, nameof(onFulfilled));

            var builder = new TaskCompletionSource();

            BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    if (onRejected != null) {
                        onRejected(UnwrapException(t.Exception!));
                        builder.SetResult();
                    } else {
                        builder.SetException(t.Exception!);
                    }
                } else {
                    try {
                        onFulfilled(t.Result);
                        builder.SetResult();
                    } catch (System.Exception ex) {
                        builder.SetException(ex);
                    }
                }
            });

            return new Promise(builder.Task);
        }
        public IPromise Then(Action<T> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise Then(Func<T, Task> onFulfilled, Action<Exception> onRejected) {
            ValidNonNull(onFulfilled, nameof(onFulfilled));

            var builder = new TaskCompletionSource();

            BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    if (onRejected != null) {
                        onRejected(UnwrapException(t.Exception!));
                        builder.SetResult();
                    } else {
                        builder.SetException(t.Exception!);
                    }
                } else {
                    Task task = onFulfilled(t.Result);
                    if (task != null) {
                        task.ContinueWith(t => {
                            if (t.IsFaulted) {
                                builder.SetException(t.Exception!);
                            } else {
                                builder.SetResult();
                            }
                        });
                    } else {
                        builder.SetResult();
                    }
                }
            });

            return new Promise(builder.Task);
        }
        public IPromise Then(Func<T, Task> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise Then(Func<T, IPromise> onFulfilled, Action<Exception> onRejected) {
            ValidNonNull(onFulfilled, nameof(onFulfilled));

            var builder = new TaskCompletionSource();

            BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    if (onRejected != null) {
                        onRejected(UnwrapException(t.Exception!));
                        builder.SetResult();
                    } else {
                        builder.SetException(t.Exception!);
                    }
                } else {
                    try {
                        IPromise promise = onFulfilled(t.Result);
                        if (promise != null) {
                            promise
                                .Then(() => builder.SetResult())
                                .Catch(ex => builder.SetException(ex));
                        } else {
                            builder.SetResult();
                        }
                    } catch (System.Exception ex) {
                        builder.SetException(ex);
                    }
                }
            });

            return new Promise(builder.Task);
        }
        public IPromise Then(Func<T, IPromise> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise<TResult> Then<TResult>(Func<T, TResult> onFulfilled, Action<Exception> onRejected) {
            ValidNonNull(onFulfilled, nameof(onFulfilled));

            var builder = new TaskCompletionSource<TResult>();

            BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    if (onRejected != null) {
                        onRejected(UnwrapException(t.Exception!));
                        builder.SetResult(default!);
                    } else {
                        builder.SetException(t.Exception!);
                    }
                } else {
                    try {
                        builder.SetResult(onFulfilled(t.Result));
                    } catch (System.Exception ex) {
                        builder.SetException(ex);
                    }
                }
            });

            return new Promise<TResult>(builder.Task);
        }
        public IPromise<TResult> Then<TResult>(Func<T, TResult> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise<TResult> Then<TResult>(Func<T, Task<TResult>> onFulfilled, Action<Exception> onRejected) {
            ValidNonNull(onFulfilled, nameof(onFulfilled));

            TaskCompletionSource<TResult> builder = new TaskCompletionSource<TResult>();

            BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    if (onRejected != null) {
                        onRejected(UnwrapException(t.Exception!));
                        builder.SetResult(default!);
                    } else {
                        builder.SetException(t.Exception!);
                    }
                } else {
                    Task<TResult> task = onFulfilled(t.Result);
                    if (task != null) {
                        task.ContinueWith(t => {
                            if (t.IsFaulted) {
                                builder.SetException(t.Exception!);
                            } else {
                                builder.SetResult(t.Result);
                            }
                        });
                    } else {
                        builder.SetResult(default!);
                    }
                }
            });

            return new Promise<TResult>(builder.Task);
        }
        public IPromise<TResult> Then<TResult>(Func<T, Task<TResult>> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise<TResult> Then<TResult>(Func<T, IPromise<TResult>> onFulfilled, Action<Exception> onRejected) {
            ValidNonNull(onFulfilled, nameof(onFulfilled));

            var builder = new TaskCompletionSource<TResult>();

            BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    if (onRejected != null) {
                        onRejected(UnwrapException(t.Exception!));
                        builder.SetResult(default!);
                    } else {
                        builder.SetException(t.Exception!);
                    }
                } else {
                    try {
                        IPromise<TResult> promise = onFulfilled(t.Result);
                        if (promise != null) {
                            promise
                                .Then(result => builder.SetResult(result))
                                .Catch(ex => builder.SetException(ex));
                        } else {
                            builder.SetResult(default!);
                        }
                    } catch (System.Exception ex) {
                        builder.SetException(ex);
                    }
                }
            });

            return new Promise<TResult>(builder.Task);
        }
        public IPromise<TResult> Then<TResult>(Func<T, IPromise<TResult>> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        #endregion

        #region Finally
        public new IPromise<T> Finally(Action onFinal) {
            ValidNonNull(onFinal, nameof(onFinal));

            var builder = new AsyncTaskMethodBuilder<T>();

            BackingTask!.ContinueWith(t => {
                onFinal();

                if (t.IsFaulted) {
                    builder.SetException(t.Exception!);
                } else {
                    builder.SetResult(t.Result);
                }
            });

            return new Promise<T>(builder.Task);
        }

        #endregion
    }
}