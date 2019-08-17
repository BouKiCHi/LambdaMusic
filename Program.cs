using System;
using LambdaMusic.Compile;

namespace LambdaMusic {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("LambdaMusic ver 0.1");
            var op = new OptParse();
            op.Parse(args);
            if (op.Usage()) return;

            var c = new MmlCompile();
            c.Verbose = op.Verbose;
            c.Play = op.PlayFlag;
            c.CompileFile(op.FileList[0]);
            // OutputTest();
            Console.WriteLine("done.");
        }
    }
}
