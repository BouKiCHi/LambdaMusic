﻿using LambdaMusic.Compile;
using System;
using System.Linq;

namespace LambdaMusic {
    class Test {
        private static Random random = new Random();

        public static void ShowTestTable() {
            var tbl = new TextTable();

            var wt = Enumerable.Range(0, 4).Select(x => GetRandomValue(25, 10)).ToArray();

            for (var rc = 0; rc < 15; rc++) {
                var row = new TableRow();
                tbl.AddRow(row);
                for (var i = 0; i < wt.Length; i++) {
                    var tl = GetRandomValue(wt[i], 5);
                    row.AddCell(RandomString(tl));
                }
            }

            tbl.ShowTable();
        }

        private static int GetRandomValue(int max, int min) {
            var r = random.Next(max);
            return r < min ? min : r;
        }

        public static string RandomString(int length) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static void ShowError() {
            var err = new ErrorData();
            var m = new MmlFileReader();
            err.SetFileReader(m);
            err.ShowMessage();
        }
    }
}
