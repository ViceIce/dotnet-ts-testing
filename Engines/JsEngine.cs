﻿using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace dotnet_ts_testing.Engines
{
    abstract class JsEngine : IJsEngine
    {
        public string Type { get; set; } = "tsc";
        public bool Minimize { get; set; } = false;

        protected abstract string Engine { get; }

        protected virtual bool NonWindows => true;

        protected string Compiler => Read($"scripts/{Type}{(Minimize ? ".min" : string.Empty)}.js");


        protected abstract string Compile(string code);

        private string Read(string file) => File.ReadAllText($"{file}");



        public void Test(string test)
        {
            var expected = Read($"tests/{test}.js");
            var code = Read($"tests/{test}.ts");

            Console.WriteLine($"[{Engine}] Test {test} started ...");
            PrintLine();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !NonWindows)
            {
                Console.WriteLine($"[{Engine}] not supported on platform: {RuntimeInformation.OSDescription}");
                PrintLine();
                return;
            }
            try
            {
                var start = Stopwatch.StartNew();
                var actual = Compile(code);

                PrintLine();

                if (actual != expected)
                {
                    Console.Error.WriteLine($"[{Engine}] Test {test} failed in {start.Elapsed}");
                    PrintLine();
                    PrintDiff(expected, actual);
                }
                else
                    Console.WriteLine($"[{Engine}] Test {test} succeeded in {start.Elapsed}");
            }
            catch (Exception ex)
            {
                PrintLine();
                Console.Error.WriteLine($"[{Engine}] Test {test} failed\n{ex}");
            }
            PrintLine();
        }

        private static void PrintDiff(string expected, string actual)
        {
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(expected, actual, false, false, new LineEndingsPreservingChunker());

            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("+ ");
                        break;
                    case ChangeType.Deleted:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("- ");
                        break;
                    case ChangeType.Imaginary:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("* ");
                        break;
                    case ChangeType.Modified:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("* ");
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("  ");
                        break;
                }

                Console.WriteLine(line.Text.Replace("\n", "\\n").Replace("\r", "\\r"));
            }

            Console.ResetColor();
        }

        private static void PrintLine()
        {
            Console.WriteLine($"----------------------------------");
        }
    }
}
