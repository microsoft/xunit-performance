using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Xunit.Performance.Api.Table
{
    internal sealed class CSVReader : IDisposable
    {
        private StreamReader _Reader;
        private Dictionary<string, int> titlePositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private List<string[]> lines = new List<string[]>();

        public CSVReader(string filePath)
        {
            var fs = new FileStream(filePath, FileMode.Open);
            this._Reader = new StreamReader(fs);

            // Get the title line
            string line = this._Reader.ReadLine();
            if (line == null)
            {
                throw new InvalidDataException("No lines in CSV file.");
            }

            string[] titles = FormatLine(line);
            for (int i = 0; i < titles.Length; ++i)
            {
                this.titlePositions.Add(titles[i], i);
            }

            while ((line = this._Reader.ReadLine()) != null)
            {
                this.lines.Add(FormatLine(line));
            }
        }

        public Dictionary<string, int> TitlePositions
        {
            get { return titlePositions; }
        }

        public string GetValue(int line, int pos)
        {
            if (line > this.lines.Count || pos > this.lines[line].Length)
            {
                throw new ArgumentException();
            }

            return this.lines[line][pos];
        }

        public string GetValue(int line, string name)
        {
            int pos;
            if (line > this.lines.Count || !this.titlePositions.TryGetValue(name, out pos))
            {
                throw new ArgumentException();
            }

            return this.GetValue(line, pos);
        }

        public int Length()
        {
            return this.lines.Count;
        }

        public int LineLength(int lineNumber)
        {
            if (lineNumber > this.lines.Count)
            {
                throw new ArgumentException();
            }

            return this.lines[lineNumber].Length;
        }

        private static string[] FormatLine(string line)
        {
            string[] s = line.Split(',');

            for (int i = 0; i < s.Length; ++i)
            {
                s[i] = s[i].Trim('\"');
            }

            return s;
        }

        void IDisposable.Dispose()
        {
            if (_Reader != null)
            {
                _Reader.Dispose();
                _Reader = null;
            }
        }
    }

    public sealed class CSVFile : IDisposable
    {
        private StreamWriter _Writer;

        public CSVFile(string filePath)
        {
            var fs = new FileStream(filePath, FileMode.OpenOrCreate);
            _Writer = new StreamWriter(fs);
        }

        void IDisposable.Dispose()
        {
            if (_Writer != null)
            {
                _Writer.Dispose();
                _Writer = null;
            }
        }

        private const char Quote = '"';
        private const char Comma = ',';

        public void WriteLine(string[] values)
        {
            // Iterate through the values, writing them out.
            for (int i = 0; i < values.Length; i++)
            {
                _Writer.Write(Quote);
                _Writer.Write(values[i]);
                _Writer.Write(Quote);

                // Write a comma to separate all values.  Don't write a comma after the last value.
                if (i < values.Length - 1)
                {
                    _Writer.Write(Comma);
                }
            }

            // Write the newline.
            _Writer.WriteLine();
        }
    }
}
