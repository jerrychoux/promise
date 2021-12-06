using System;
using System.Threading.Tasks;

using cn.jerrychoux.promise;

namespace example {
    public partial class Test {
        async public static void Catch() {
            Console.WriteLine("> start <");

            await Promise
                .Resolve()
                .Then(() => Console.WriteLine("> then 1"))
                .Then(() => {
                    Console.WriteLine("> then 2");
                    return Task.FromException(new Exception("task ex 2"));
                })
                .Then(() => {
                    Console.WriteLine("> then 3");
                    return Promise.Reject(new Exception("promise ex 3"));
                })
                .Then(() => Console.WriteLine("> then 4"))
                .Catch(ex => Console.WriteLine("> catch {0}", ex.Message))
                .Finally(() => Console.WriteLine("> finally"));

            Console.WriteLine("> finish <");
        }
    }
}
