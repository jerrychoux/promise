using System;
using System.Threading.Tasks;

using cn.jerrychoux.promise;

namespace example {
    public partial class Test {
        async public static void Any() {
            Console.WriteLine("> start <");

            IPromise<int> promise1 = Promise.Resolve().Then(async () => {
                await Task.Delay(TimeSpan.FromSeconds(3));
                Console.WriteLine("> then 1");
                // throw new Exception("promise ex 1");
                return 99;
            });
            IPromise<int> promise2 = Promise.Resolve().Then(async () => {
                await Task.Delay(TimeSpan.FromSeconds(2));
                Console.WriteLine("> then 2");
                // throw new Exception("promise ex 2");
                return 88;
            });
            IPromise<int> promise3 = Promise.Resolve().Then(async () => {
                await Task.Delay(TimeSpan.FromSeconds(1));
                Console.WriteLine("> then 3");
                // throw new Exception("promise ex 3");
                return 77;
            });

            await Promise<int>
                .Any(promise1, promise2, promise3)
                .Then((int res) => Console.WriteLine("> then last {0}", res))
                .Catch(ex => Console.WriteLine("> catch {0}", ex.Message))
                .Finally(() => Console.WriteLine("> finally"));

            Console.WriteLine("> finish <");
        }
    }
}
