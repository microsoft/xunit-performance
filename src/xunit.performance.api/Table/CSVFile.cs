using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Xunit.Performance.Api.Table
{
    sealed class CSVFile : IDisposable
    {
        const char Comma = ',';

        const char Quote = '"';

        readonly Stream _stream;

        readonly StreamWriter _writer;

        bool _disposed;

        public CSVFile(string filePath)
        {
            try
            {
                _disposed = false;
                _stream = new FileStream(filePath, FileMode.Create);
                _writer = new StreamWriter(_stream);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void WriteLine(string[] values)
        {
            // Iterate through the values, writing them out.
            for (int i = 0; i < values.Length; i++)
            {
                var newValue = values[i].Replace("\"", "\"\"");
                _writer.Write($"{Quote}{newValue}{Quote}");

                // Write a comma to separate all values.  Don't write a comma after the last value.
                if (i < values.Length - 1)
                    _writer.Write(Comma);
            }

            _writer.WriteLine();
        }

        #region IDisposable implementation

        ~CSVFile()
        {
            Dispose(false);
        }

        public void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose() => Close();

        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    FreeManagedResources();
                _disposed = true;
            }
        }

        void FreeManagedResources()
        {
            if (_writer != null)
                _writer.Dispose();
            if (_stream != null)
                _stream.Dispose();
        }

        #endregion IDisposable implementation
    }

    sealed class CSVReader : IDisposable
    {
        readonly List<string[]> _lines;

        readonly StreamReader _reader;

        readonly Stream _stream;

        bool _disposed;

        public CSVReader(string filePath)
        {
            try
            {
                _disposed = false;
                _stream = new FileStream(filePath, FileMode.Open);
                _reader = new StreamReader(_stream);
                TitlePositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                _lines = new List<string[]>();

                // Get the title line
                var line = _reader.ReadLine();
                if (line == null)
                    throw new InvalidDataException("No lines in CSV file.");

                var titles = FormatLine(line);
                for (int i = 0; i < titles.Length; ++i)
                    TitlePositions.Add(titles[i], i);

                while ((line = _reader.ReadLine()) != null)
                    _lines.Add(FormatLine(line));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public Dictionary<string, int> TitlePositions { get; private set; }

        public string GetValue(int line, int pos)
        {
            if (line > _lines.Count || pos > _lines[line].Length)
            {
                throw new ArgumentException();
            }

            return _lines[line][pos];
        }

        public string GetValue(int line, string name)
        {
            if (line > _lines.Count || !TitlePositions.TryGetValue(name, out int pos))
            {
                throw new ArgumentException();
            }

            return GetValue(line, pos);
        }

        public int Length() => _lines.Count;

        public int LineLength(int lineNumber)
        {
            if (lineNumber > _lines.Count)
            {
                throw new ArgumentException();
            }

            return _lines[lineNumber].Length;
        }

        static string[] FormatLine(string line)
        {
            var s = line.Split(',');

            for (int i = 0; i < s.Length; ++i)
            {
                s[i] = s[i].Trim('\"');
            }

            return s;
        }

        #region IDisposable implementation

        ~CSVReader()
        {
            Dispose(false);
        }

        public void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose() => Close();

        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    FreeManagedResources();
                _disposed = true;
            }
        }

        void FreeManagedResources()
        {
            if (_reader != null)
                _reader.Dispose();
            if (_stream != null)
                _stream.Dispose();
        }

        #endregion IDisposable implementation
    }
}