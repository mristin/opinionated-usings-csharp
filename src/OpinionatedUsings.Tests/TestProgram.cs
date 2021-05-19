using Directory = System.IO.Directory;
using Environment = System.Environment;
using File = System.IO.File;
using Path = System.IO.Path;

using NUnit.Framework;  // can't alias

namespace OpinionatedUsings.Tests
{
    public class ProgramTests
    {
        [Test]
        public void Test_no_command_line_arguments_shows_an_error()
        {
            using var consoleCapture = new ConsoleCapture();

            int exitCode = Program.MainWithCode(new string[0]);

            string nl = Environment.NewLine;

            Assert.AreEqual(1, exitCode);
            Assert.AreEqual(
                $"Option '--inputs' is required.{nl}{nl}",
                consoleCapture.Error());
        }

        [Test]
        public void Test_invalid_command_line_arguments_causes_an_error()
        {
            using var consoleCapture = new ConsoleCapture();

            int exitCode = Program.MainWithCode(new[] { "--invalid-arg" });

            string nl = Environment.NewLine;

            Assert.AreEqual(1, exitCode);
            Assert.AreEqual(
                $"Option '--inputs' is required.{nl}" +
                $"Unrecognized command or argument '--invalid-arg'{nl}{nl}",
                consoleCapture.Error());
        }

        [Test]
        public void Test_non_code_input_causes_an_error()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(path, "this is not parsable C# code.");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path });

            Assert.AreEqual("", consoleCapture.Error());
            Assert.AreEqual("", consoleCapture.Output());
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void Test_valid_input_causes_no_errors()
        {
            using var tmpdir = new TemporaryDirectory();

            using var consoleCapture = new ConsoleCapture();

            string nl = Environment.NewLine;

            string path = Path.Join(tmpdir.Path, "SomeProgram.cs");
            File.WriteAllText(
                path,
                $"using File = System.IO.File;{nl}" +
                $"using Path = System.IO.Path;{nl}" +
                $"using SystemUri = System.Uri; // renamed{nl}" +
                $"{nl}" +
                $"using System.Linq;  // can't alias");

            int exitCode = Program.MainWithCode(new[] { "--inputs", path });

            Assert.AreEqual("", consoleCapture.Error());
            Assert.AreEqual("", consoleCapture.Output());
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void Test_recorded_output_on_fails()
        {
            using var consoleCapture = new ConsoleCapture();

            string failsDir = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                Path.Join(
                    "TestResources"),
                $"{nameof(OpinionatedUsings)}.{nameof(Tests)}",
                "fails");

            foreach (string caseDir in Directory.GetDirectories(failsDir))
            {
                string path = Path.Join(caseDir, "Code.cs");

                int exitCode = Program.MainWithCode(new[] { "--inputs", path });

                Assert.AreEqual(1, exitCode);

                string gotOut = consoleCapture.Output()
                    .Replace(path, "<path>")
                    .Replace("\r\n", "\n");

                string gotErr = consoleCapture.Error()
                    .Replace(path, "<path>")
                    .Replace("\r\n", "\n");

                string outputPath = Path.Join(caseDir, "ExpectedOutput.txt");
                string expectedOut = File.ReadAllText(outputPath)
                    .Replace("\r\n", "\n");

                string errPath = Path.Join(caseDir, "ExpectedError.txt");
                string expectedErr = File.ReadAllText(errPath)
                    .Replace("\r\n", "\n");

                Assert.AreEqual(expectedOut, gotOut);
                Assert.AreEqual(expectedErr, gotErr);
            }
        }
    }
}