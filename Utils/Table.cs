﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LambdaMusic {
    class Table {
        public List<TableRow> Rows = new List<TableRow>();

        public void ShowTable() {
            var MaxCell = Rows.Max(x => x.Cell.Count);
            var WidthData = Enumerable.Range(0, MaxCell).Select(x => CellWidth(x)).ToArray();
            var Width = WidthData.Aggregate((t, v) => t + v);
            Width = Width + 1 + MaxCell;
            WriteBorderText(MaxCell, WidthData);
            
            for(var rc = 0; rc < Rows.Count; rc++) {
                if (rc > 0) {
                    Console.WriteLine(new string('-', Width));
                }
                var r = Rows[rc];
                WriteRowText(MaxCell, WidthData, r);
            }
            WriteBorderText(MaxCell, WidthData);
        }

        public void AddRow(TableRow row) {
            Rows.Add(row);
        }


        private void WriteBorderText(int MaxCell, int[] WidthData) {
            for (var cc = 0; cc < MaxCell; cc++) {
                Console.Write("+");
                var cw = WidthData[cc];
                Console.Write(new string('-', cw));
            }
            Console.WriteLine("+");
        }


        private static void WriteRowText(int MaxCell, int[] WidthData, TableRow r) {
            for (var cc = 0; cc < MaxCell; cc++) {
                Console.Write("|");
                var t = r.GetCell(cc);
                var cw = WidthData[cc] - t.Length;
                if (cw > 0) t += new string(' ', cw);
                Console.Write(t);
            }
            Console.WriteLine("|");
        }


        private int CellWidth(int Index) {
            return Rows.Select(x => x.CellWidth(Index)).Max();
        }
    }
    
    class TableRow {
        public List<string> Cell= new List<string>();

        public string GetCell(int Index) {
            return Cell.Count <= Index ? "" : Cell[Index];
        }

        public int CellWidth(int Index) {
            return GetCell(Index).Length;
        }

        public void AddCell(string Text) {
            Cell.Add(Text);
        }

        public int RowWidth {
            get {
                return Cell.Select(x => x.Length).Aggregate((total, v) => total + v);
            }
        }
    }
}
