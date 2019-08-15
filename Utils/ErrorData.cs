using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LambdaMusic {
    class ErrorData {
        List<string> Message = new List<string>();

        public bool HasError { get { return Message.Count > 0; } }

        public void Add(int LineNo, string Text) {
            var s = $"{LineNo} : Error {Text}";
            Message.Add(s);
        }

        public void ShowMessage() {
            foreach (var t in Message) Console.WriteLine(t);
        }
    }
}
