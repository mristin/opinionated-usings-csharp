using CSharpSyntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree;
using System.Linq; // can't alias
using NUnit.Framework;
using NUnit.Framework.Internal; // can't alias

namespace OpinionatedUsings.Tests
{
    public class InspectTests
    {
        [Test]
        public void Test_empty_program_causes_no_errors()
        {
            const string programText = "";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void Test_non_code_program_causes_no_errors()
        {
            const string programText = "This is no C# code.";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void Test_fail_unknown_marker()
        {
            const string programText = "using File = System.IO.File;  // unknown";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(0, records.First().Line);
            Assert.AreEqual(0, records.First().Column);
            Assert.AreEqual(1, records.First().Errors.Count);

            Assert.AreEqual(
                "Unrecognized marking comment for the using " +
                "directive \"using File = System.IO.File;\": \"  // unknown\"",
                records.First().Errors.First());
        }

        [Test]
        public void Test_fail_non_aliased_using_without_marker()
        {
            const string programText = "using System.IO;";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(0, records.First().Line);
            Assert.AreEqual(0, records.First().Column);
            Assert.AreEqual(1, records.First().Errors.Count);

            Assert.AreEqual(
                "Expected a non-aliased using directive \"using System.IO;\" " +
                "to be explicitly marked with `// can't alias` comment, " +
                "but found no marking.",
                records.First().Errors.First());
        }

        [Test]
        public void Test_fail_renamed_using_without_marker()
        {
            const string programText = "using SysFile = System.IO.File;";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(0, records.First().Line);
            Assert.AreEqual(0, records.First().Column);
            Assert.AreEqual(1, records.First().Errors.Count);

            Assert.AreEqual(
                "Expected an aliased using directive " +
                "\"using SysFile = System.IO.File;\" with the alias different " +
                "from the name to have the marking comment `// renamed`, " +
                "but found no marking comment.",
                records.First().Errors.First());
        }

        [Test]
        public void Test_fail_non_renamed_aliased_using_with_renamed_marker()
        {
            const string programText = "using File = System.IO.File;  // renamed";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(0, records.First().Line);
            Assert.AreEqual(0, records.First().Column);
            Assert.AreEqual(1, records.First().Errors.Count);

            Assert.AreEqual(
                "Expected an aliased using " +
                "directive \"using File = System.IO.File;\" to have " +
                "no marking comments since there was no renaming involved, " +
                "but found the marking comment: \"  // renamed\"",
                records.First().Errors.First());
        }

        [Test]
        public void Test_fail_renamed_alias_not_marked()
        {
            const string programText = "using SysFile = System.IO.File;";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(0, records.First().Line);
            Assert.AreEqual(0, records.First().Column);
            Assert.AreEqual(1, records.First().Errors.Count);

            Assert.AreEqual(
                "Expected an aliased using " +
                "directive \"using SysFile = System.IO.File;\" with " +
                "the alias different from the name to have " +
                "the marking comment `// renamed`, but found no marking comment.",
                records.First().Errors.First());
        }

        [Test]
        public void Test_fail_non_aliased_before_aliased()
        {
            const string programText = "using System.Linq;  // can't alias\n" +
                                       "using File = System.IO.File;";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(1, records.First().Line);
            Assert.AreEqual(0, records.First().Column);
            Assert.AreEqual(1, records.First().Errors.Count);

            Assert.AreEqual(
                "Expected aliased using " +
                "directive \"using File = System.IO.File;\" before the non-aliased " +
                "using directive \"using System.Linq;\" at line 1.",
                records.First().Errors.First());
        }

        [Test]
        public void Test_fail_aliases_not_sorted()
        {
            const string programText = "using Path = System.IO.Path;\n" +
                                       "using File = System.IO.File;";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(1, records.First().Line);
            Assert.AreEqual(0, records.First().Column);
            Assert.AreEqual(1, records.First().Errors.Count);

            Assert.AreEqual(
                "Expected the alias \"File\" from the using " +
                "directive \"using File = System.IO.File;\" before the previous " +
                "alias \"Path\" from the using " +
                "directive \"using Path = System.IO.Path;\" " +
                "at line 1 (by alphabetical order).",
                records.First().Errors.First());
        }

        [Test]
        public void Test_pass_common_case()
        {
            const string programText =
                "using File = System.IO.File;\n" +
                "using Path = System.IO.Path;\n" +
                "using SystemUri = System.Uri;  // renamed\n" +
                "\n" +
                "using System.Linq;   // can't alias\n" +
                "using System.Collections.Generic;  // can't alias\n";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void Test_pass_trailing_comments_on_separate_lines_are_ok()
        {
            const string programText = "using File = System.IO.File;\n" +
                                       "// unknown";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void Test_pass_non_aliased_usings_not_sorted()
        {
            const string programText =
                "using System.Linq;   // can't alias\n" +
                "using System.Collections.Generic;  // can't alias\n";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void Test_aliased_are_sorted_by_aliases()
        {
            const string programText = "using Directory = System.IO.Directory;\n" +
                                       "using Environment = System.Environment;";
            var tree = CSharpSyntaxTree.ParseText(programText);

            var records = Inspection.Inspect(tree).ToList();
            Assert.AreEqual(0, records.Count);
        }
    }
}