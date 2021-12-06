using System;

namespace example {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello Promise!");
            Test.Then();
            // Test.Catch();
            // Test.All();
            // Test.Any();
            // Test.Race();
            Console.ReadLine();
        }
    }
}
