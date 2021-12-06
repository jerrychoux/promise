using System;
using System.Threading.Tasks;

using cn.jerrychoux.promise;

namespace example {
    public partial class Test {
        async public static void Then() {
            Console.WriteLine("> start <");

            await Promise
                .Resolve()
                .Then(() => Console.WriteLine("> then 1"))
                .Then(() => {
                    Console.WriteLine("> then 2");
                    return 99;
                })
                .Then(num => {
                    Console.WriteLine("> then 3 num={0}", num);
                    return 88;
                })
                .Then(num => {
                    Console.WriteLine("> then 4 num={0}", num);
                    return Task.Delay(TimeSpan.FromSeconds(3));
                })
                .Then(async () => {
                    Console.WriteLine("> then 5");
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    return 77;
                })
                .Then((num) => {
                    return Promise<int>
                             .Resolve(num)
                             .Then(num => {
                                 Console.WriteLine("> then 6 then 1 num={0}", num);
                             })
                             .Then(() => {
                                 Console.WriteLine("> then 6 then 2");
                             });
                })
                .Catch(ex => Console.WriteLine("> catch {0}", ex.Message))
                .Finally(() => Console.WriteLine("> finally"));

            Console.WriteLine("> finish <");
        }
    }
}
