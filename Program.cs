using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaMusic {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("LambdaMusic ver 1.0");
            var c = new Compile.Compile();
            c.CompileFile("testsong.mml");
            // OutputTest();
            Console.WriteLine("done.");
            Console.ReadKey();
        }
    }
}
