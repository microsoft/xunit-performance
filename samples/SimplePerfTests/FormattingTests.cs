// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance;
using Xunit;

namespace SimplePerfTests
{
    public class Document
    {
        private string _text;

        public Document(string text)
        {
            _text = text;
        }

        public void Format()
        {
            _text = _text.ToUpper();
        }

        public Task FormatAsync()
        {
            Format();
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return _text;
        }
    }

    public class FormattingTests
    {
        private static IEnumerable<object[]> MakeArgs(params object[] args)
        {
            return args.Select(arg => new object[] { arg });
        }

        public static IEnumerable<object[]> FormatCurlyBracesMemberData = MakeArgs(
            new Document("Hello, world!")
        );

        [Benchmark]
        [MemberData(nameof(FormatCurlyBracesMemberData))]
        public static void FormatCurlyBracesTest(Document document)
        {
            Benchmark.Iterate(document.Format);
        }

        [Benchmark]
        [MemberData(nameof(FormatCurlyBracesMemberData))]
        public static async Task FormatCurlyBracesTestAsync(Document document)
        {
            await Benchmark.IterateAsync(() => document.FormatAsync());
        }
    }
}