using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace cn.jerrychoux.promise {
    public class Promise : IPromise {
        #region Field
        Task? BackingTask { get; set; }
        #endregion

        #region Constructor
        protected Promise() { }
        public Promise(Task task) => BackingTask = task ?? throw new ArgumentNullException(nameof(task), "Must provide a task for a new promise.");
        public Promise(Action<Action, Action<Exception>> callback) {
            ValidNonNull(callback, nameof(callback));

            var builder = new AsyncTaskMethodBuilder();
            BackingTask = builder.Task;

            try {
                callback(() => builder.SetResult(), ex => builder.SetException(ex));
            } catch (System.Exception ex) {
                builder.SetException(ex);
            }
        }
        public Promise(Action<Action> callback) : this(callback == null ? (Action<Action, Action<Exception>>)null! : (resolve, _) => callback!.Invoke(resolve)) { }
        #endregion

        #region Utils
        protected static bool ValidNonNull(object param, string paramName) {
            if (param == null) throw new ArgumentNullException(paramName, "Cannot be null.");

            return true;
        }
        protected static Exception UnwrapException(AggregateException aggregate) {
            Exception ex = aggregate;

            while (ex is AggregateException ae) {
                if (ae.InnerException == null) break;

                ex = ae.InnerException;
            }

            return ex;
        }
        public static IPromise Resolve() => new Promise(Task.CompletedTask);
        public static IPromise Reject(Exception ex) => new Promise(Task.FromException(ex));
        public TaskAwaiter GetAwaiter() => BackingTask!.GetAwaiter();
        public static implicit operator Task(Promise promise) => promise.BackingTask!;
        public static implicit operator Promise(Task task) => new Promise(task);
        #endregion

        #region All
        public static IPromise All(params Task[] tasks) {
            ValidNonNull(tasks, nameof(tasks));

            if (tasks.Length == 0) return Resolve();

            return new Promise(Task.WhenAll(tasks));
        }
        public static IPromise All(IEnumerable<Task> tasks) => All(tasks == null ? null! : tasks.ToArray());
        public static IPromise All(params IPromise[] promises) {
            ValidNonNull(promises, nameof(promises));

            if (promises.Length == 0) return Resolve();

            return All(promises.Select(async p => await p));
        }
        public static IPromise All(IEnumerable<IPromise> promises) => All(promises == null ? null! : promises.ToArray());
        #endregion

        #region Any
        public static IPromise Any(params Task[] tasks) {
            ValidNonNull(tasks, nameof(tasks));

            if (tasks.Length == 0) return Resolve();

            var builder = new TaskCompletionSource<int>();
            var builders = new List<TaskCompletionSource<int>>(tasks.Length);
            foreach (var task in tasks) {
                builders.Add(new TaskCompletionSource<int>());
            }

            int index = -1;
            foreach (var task in tasks) {
                task.ContinueWith(t => {
                    int i = Interlocked.Increment(ref index);
                    if (t.IsFaulted) {
                        builders[i].SetException(t.Exception!);
                    } else {
                        builders[i].SetResult(default);
                        builder.TrySetResult(default);
                    }
                });
            }

            Task.WhenAll(builders.Select(s => s.Task)).ContinueWith(t => {
                var faultedBuilder = builders.FirstOrDefault(s => s.Task.IsFaulted);
                if (faultedBuilder != null) {
                    builder.SetException(faultedBuilder.Task.Exception!);
                }
            });

            return new Promise(builder.Task);
        }
        public static IPromise Any(IEnumerable<Task> tasks) => Any(tasks == null ? null! : tasks.ToArray());
        public static IPromise Any(params IPromise[] promises) {
            ValidNonNull(promises, nameof(promises));

            return Any(promises.Select(async p => await p));
        }
        public static IPromise Any(IEnumerable<IPromise> promises) => Any(promises == null ? null! : promises.ToArray());
        #endregion

        #region Race
        public static IPromise Race(params Task[] tasks) {
            ValidNonNull(tasks, nameof(tasks));

            if (tasks.Length == 0) return Resolve();

            var builder = new TaskCompletionSource<int>();

            foreach (Task task in tasks) {
                task.ContinueWith(t => {
                    if (t.IsFaulted) {
                        builder.TrySetException(t.Exception!);
                    } else {
                        builder.TrySetResult(default);
                    }
                });
            }

            return new Promise(builder.Task);
        }
        public static IPromise Race(IEnumerable<Task> tasks) => Race(tasks == null ? null! : tasks.ToArray());
        public static IPromise Race(params IPromise[] promises) {
            ValidNonNull(promises, nameof(promises));

            return Race(promises.Select(async p => await p));
        }
        public static IPromise Race(IEnumerable<IPromise> promises) => Race(promises == null ? null! : promises.ToArray());
        #endregion

        #region Then
        public IPromise Then(Action onFulfilled, Action<Exception> onRejected) {
            ValidNonNull(onFulfilled, nameof(onFulfilled));

            var builder = new TaskCompletionSource<int>();

            BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    if (onRejected != null) {
                        onRejected(UnwrapException(t.Exception!));
                        builder.SetResult(default);
                    } else {
                        builder.SetException(t.Exception!);
                    }
                } else {
                    try {
                        onFulfilled();
                        builder.SetResult(default);
                    } catch (System.Exception ex) {
                        builder.SetException(ex);
                    }
                }
            });

            return new Promise(builder.Task);
        }
        public IPromise Then(Action onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise Then(Func<Task> onFulfilled, Action<Exception> onRejected) {
            ValidNonNull(onFulfilled, nameof(onFulfilled));

            var builder = new TaskCompletionSource<int>();

            BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    if (onRejected != null) {
                        onRejected(UnwrapException(t.Exception!));
                        builder.SetResult(default);
                    } else {
                        builder.SetException(t.Exception!);
                    }
                } else {
                    Task task = onFulfilled();
                    if (task != null) {
                        task.ContinueWith(t => {
                            if (t.IsFaulted) {
                                builder.SetException(t.Exception!);
                            } else {
                                builder.SetResult(default);
                            }
                        });
                    } else {
                        builder.SetResult(default);
                    }
                }
            });

            return new Promise(builder.Task);
        }
        public IPromise Then(Func<Task> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise Then(Func<IPromise> onFulfilled, Action<Exception> onRejected) {
            ValidNonNull(onFulfilled, nameof(onFulfilled));

            var builder = new TaskCompletionSource<int>();

            BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    if (onRejected != null) {
                        onRejected(UnwrapException(t.Exception!));
                        builder.SetResult(default);
                    } else {
                        builder.SetException(t.Exception!);
                    }
                } else {
                    try {
                        IPromise promise = onFulfilled();
                        if (promise != null) {
                            promise
                                .Then(() => builder.SetResult(default))
                                .Catch(ex => builder.SetException(ex));
                        } else {
                            builder.SetResult(default);
                        }
                    } catch (System.Exception ex) {
                        builder.SetException(ex);
                    }
                }
            });

            return new Promise(builder.Task);
        }
        public IPromise Then(Func<IPromise> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise<TResult> Then<TResult>(Func<TResult> onFulfilled, Action<Exception> onRejected) {
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
                        builder.SetResult(onFulfilled());
                    } catch (System.Exception ex) {
                        builder.SetException(ex);
                    }
                }
            });

            return new Promise<TResult>(builder.Task);
        }
        public IPromise<TResult> Then<TResult>(Func<TResult> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise<TResult> Then<TResult>(Func<Task<TResult>> onFulfilled, Action<Exception> onRejected) {
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
                    Task<TResult> task = onFulfilled();
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
        public IPromise<TResult> Then<TResult>(Func<Task<TResult>> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        public IPromise<TResult> Then<TResult>(Func<IPromise<TResult>> onFulfilled, Action<Exception> onRejected) {
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
                        IPromise<TResult> promise = onFulfilled();
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
        public IPromise<TResult> Then<TResult>(Func<IPromise<TResult>> onFulfilled) => Then(onFulfilled, (Action<Exception>)null!);
        #endregion

        #region Catch
        public IPromise Catch(Action<Exception> onError) {
            ValidNonNull(onError, nameof(onError));

            Task task = BackingTask!.ContinueWith(t => {
                if (t.IsFaulted) {
                    onError(UnwrapException(t.Exception!));
                }
            });

            return new Promise(task);
        }
        #endregion

        #region Finally
        public IPromise Finally(Action onFinal) {
            ValidNonNull(onFinal, nameof(onFinal));

            return new Promise(BackingTask!.ContinueWith(t => onFinal()));
        }
        #endregion
    }
}