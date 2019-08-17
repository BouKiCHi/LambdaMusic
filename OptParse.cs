using System;
using System.Collections.Generic;

namespace LambdaMusic {
    class OptParse {
        public bool PlayFlag = false;
        public bool Verbose = false;
        public bool ShowUsageFlag = false;
        public List<string> FileList = new List<string>();
        public void Parse(string[] args) {
            for(var i=0; i < args.Length; i++) {
                var t = args[i];
                switch(t) {
                    case "-p":
                        PlayFlag = true;
                        break;
                    case "-?":
                    case "-h":
                        ShowUsageFlag = true;
                        break;
                    case "-v":
                    case "--verbose":
                        Verbose = true;
                        break;
                    default:
                        FileList.Add(t);
                        break;
                }
            }
        }

        public bool Usage() {
            if (!ShowUsageFlag && FileList.Count > 0) return false;
            Console.WriteLine("Usage LambdaMusic <mmlfile>");
            Console.WriteLine("Options");
            Console.WriteLine("-v,--verbose : Verbose mode");
            Console.WriteLine("-p : Play ");
            Console.WriteLine("-?,-h : This Help");
            return true;
        }
    }
}
