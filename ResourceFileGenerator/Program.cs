using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace GoogleEmailUploader {
  class Program {
    const string EqualTo = "=";
    const string NumText = "NUM_";

    static void PrintUsage() {
        Console.WriteLine("Usage: ResourceFileGenerator <inputStringsFilePath> <outputResxFilePath>");
    }

    /// <summary>
    /// Converts the *.strings file into corresponding .resx files.
    /// </summary>
    [STAThread]
    static void Main(string[] args) {
      if (args.Length != 2) {
        Program.PrintUsage();
        return;
      }
      string inputPath = args[0];
      if (!File.Exists(inputPath)) {
        Program.PrintUsage();
        return;
      }
      string outputPath = args[1];
      try {
        if (File.Exists(outputPath)) {
          File.Delete(outputPath);
        }
      } catch {
        Console.WriteLine("Unable to delete the output file. Please make sure file is writable");
        return;
      }
      using (ResXResourceWriter resourceWriter =
          new ResXResourceWriter(outputPath)) {
        using (FileStream fileStream = new FileStream(inputPath,
                                                      FileMode.Open,
                                                      FileAccess.Read,
                                                      FileShare.Read)) {
          using (StreamReader reader = new StreamReader(fileStream)) {
            while (reader.Peek() != -1) {
              string line = reader.ReadLine();
              int equalToIndex = line.IndexOf(Program.EqualTo);
              string key = line.Substring(1, equalToIndex - 3);
              string value = line.Substring(equalToIndex + 3,
                                            line.Length - key.Length - 8);

              if (value.IndexOf(Program.NumText) != -1) {
                // Check if the value obtained contains a text which matches
                // "NUM_*". If it does, replace the NUM_* by {*}.
                int count = Regex.Matches(value, @"NUM_\d").Count;
                for (int i = 0; i < count; ++i) {
                  value = value.Replace(Program.NumText + i, "{" + i + "}");
                }
              }
              resourceWriter.AddResource(key, value);
            }
          }
        }
        resourceWriter.Generate();
      }
    }
  }
}
