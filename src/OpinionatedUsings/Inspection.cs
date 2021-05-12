using ArgumentException = System.ArgumentException;
using CompilationUnitSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax;
using InvalidOperationException = System.InvalidOperationException;
using QualifiedNameSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.QualifiedNameSyntax;
using SimpleNameSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.SimpleNameSyntax;
using StringComparison = System.StringComparison;
using SyntaxTree = Microsoft.CodeAnalysis.SyntaxTree;
using SyntaxTrivia = Microsoft.CodeAnalysis.SyntaxTrivia;
using SyntaxTriviaList = Microsoft.CodeAnalysis.SyntaxTriviaList;
using UsingDirectiveSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax;

using System.Collections.Generic;  // can't alias
using System.Linq;  // can't alias


namespace OpinionatedUsings
{
    public static class Inspection
    {
        public class Record
        {
            public int Line { get; } // indexed at 0
            public int Column { get; } // indexed at 0
            public List<string> Errors { get; }

            public Record(int line, int column, List<string> errors)
            {
                #region Preconditions

                if (line < 0)
                {
                    throw new ArgumentException($"Negative line: {line}");
                }

                if (column < 0)
                {
                    throw new ArgumentException($"Negative column: {column}");
                }

                #endregion

                Line = line;
                Column = column;
                Errors = errors;
            }
        }

        enum MarkingKind
        {
            Unknown = 0,
            CantAlias = 1,
            Renamed = 2
        }

        /**
         * <summary>
         * Parsed marking after the using directive such as <c>// can't alias</c>.
         * </summary>
         */
        private class Marking
        {
            public readonly MarkingKind Kind;
            public readonly SyntaxTriviaList Trivia;

            public Marking(MarkingKind kind, SyntaxTriviaList trivia)
            {
                Kind = kind;
                Trivia = trivia;
            }
        }

        private static MarkingKind? ParseMarkingFromTrailingTrivia(
            List<SyntaxTrivia> sameLineTrivia)
        {
            foreach (var node in sameLineTrivia)
            {
                var text = node.ToString();
                if (text.Trim() == string.Empty)
                {
                    continue;
                }

                if (!text.StartsWith("//"))
                {
                    return MarkingKind.Unknown;
                }

                if (text.Length == 2)
                {
                    return MarkingKind.Unknown;
                }

                var content = text.Substring(2).Trim();

                switch (content)
                {
                    case "can't alias":
                        return MarkingKind.CantAlias;
                    case "renamed":
                        return MarkingKind.Renamed;
                    default:
                        return MarkingKind.Unknown;
                }
            }

            return null;
        }

        /**
         * <summary>Keep track of errors in a file.</summary>
         */
        class ErrorRegistry
        {
            private readonly SortedDictionary<(int, int), List<string>> _content =
                new SortedDictionary<(int, int), List<string>>();

            public void Add(int line, int column, string message)
            {
                var lineColumn = (line, column);
                _content.TryGetValue(lineColumn, out var messages);
                if (messages is null)
                {
                    _content.Add(lineColumn, new List<string> { message });
                }
                else
                {
                    messages.Add(message);
                }
            }

            public IEnumerable<Record> Records() =>
                _content.Select(
                    item => new Record(
                        item.Key.Item1, item.Key.Item2, item.Value));
        }

        /**
         * <summary>
         * Escape the special characters so that code can be represented in the errors
         * with double quotes.
         * </summary>
         */
        private static string Quote(string text)
            => "\"" + text
                .Replace("\\", "\\\\")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\"", "\\\"")
                .Replace("\0", "\\0")
                .Replace("\a", "\\a")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\v", "\\v") + "\"";

        /**
         * <summary>Represent a using directive with an optional marking.</summary>
         */
        private class MarkedUsing
        {
            public readonly UsingDirectiveSyntax Node;
            public readonly Marking? MaybeMarking;

            public MarkedUsing(UsingDirectiveSyntax node, Marking? maybeMarking)
            {
                Node = node;
                MaybeMarking = maybeMarking;
            }
        }

        /**
         * <summary>
         * Locally inspect the marking of the node.
         * </summary>
         * <returns>error message, if any</returns>
         */
        private static string? CheckMarking(
            UsingDirectiveSyntax node, Marking? marking)
        {
            // ReSharper disable once MergeIntoPattern
            if (marking != null && marking.Kind is MarkingKind.Unknown)
            {
                return $"Unrecognized marking comment for the " +
                       $"using directive {Quote(node.ToString().TrimEnd())}: " +
                       $"{Quote(marking.Trivia.ToString().TrimEnd())}";
            }

            if (node.Alias is null)
            {
                if (marking is null)
                {
                    return
                        $"Expected a non-aliased " +
                        $"using directive {Quote(node.ToString().TrimEnd())} " +
                        $"to be explicitly marked with `// can't alias` comment, " +
                        $"but found no marking.";
                }

                if (marking.Kind != MarkingKind.CantAlias)
                {
                    return
                        $"Expected a non-aliased " +
                        $"using directive " +
                        $"{Quote(node.ToString().TrimEnd())} to be " +
                        $"explicitly marked with `// can't alias` comment, " +
                        $"but found the marking: " +
                        $"{Quote(marking.Trivia.ToString().TrimEnd())}";
                }
            }
            else
            {
                string name = node.Name switch
                {
                    QualifiedNameSyntax qns => qns.Right.Identifier.ToString(),
                    SimpleNameSyntax sns => sns.ToString(),
                    _ => throw new InvalidOperationException(
                        $"Unexpected name node of a using directive " +
                        $"of type {node.Name.GetType()} " +
                        $"reading {Quote(node.Name.ToString().TrimEnd())}")
                };

                if (node.Alias.Name.ToString() == name)
                {
                    if (marking != null)
                    {
                        return
                            $"Expected an aliased using " +
                            $"directive {Quote(node.ToString().TrimEnd())} " +
                            $"to have no marking comments " +
                            $"since there was no renaming involved, " +
                            $"but found the marking comment: " +
                            $"{Quote(marking.Trivia.ToString().TrimEnd())}";
                    }
                }
                else
                {
                    // This alias renames.

                    if (marking is null)
                    {
                        return
                            $"Expected an aliased using " +
                            $"directive {Quote(node.ToString().TrimEnd())} " +
                            $"with the alias different from the name " +
                            $"to have the marking comment `// renamed`, " +
                            $"but found no marking comment.";
                    }

                    if (marking.Kind != MarkingKind.Renamed)
                    {
                        return
                            $"Expected an aliased using " +
                            $"directive {Quote(node.ToString().TrimEnd())} " +
                            $"with the alias different from the name " +
                            $"to have the marking comment `// renamed`, " +
                            $"but found the marking comment: " +
                            $"{Quote(marking.Trivia.ToString().TrimEnd())}.";
                    }

                    // No error detected so far.
                }
            }

            return null;
        }

        /**
         * <summary>
         * Check that the current using directive fits the expected order compared to
         * previous using directives.
         * </summary>
         * <returns>errors, if any</returns>
         */
        private static List<string>? CheckOrder(
            UsingDirectiveSyntax node,
            UsingDirectiveSyntax? previousAliased,
            UsingDirectiveSyntax? previousNonaliased)
        {
            #region Preconditions

            if (previousAliased is { Alias: null })
            {
                throw new ArgumentException(
                    $"Unexpected {nameof(previousAliased)}.Alias null");
            }

            if (previousNonaliased?.Alias != null)
            {
                throw new ArgumentException(
                    $"Unexpected {nameof(previousNonaliased)}.Alias not null");
            }

            #endregion

            List<string>? result = null;

            // We currently do not enforce order on purely non-aliased using directives.
            if (node.Alias is null)
            {
                return result;
            }

            if (previousNonaliased != null)
            {
                var line = previousNonaliased
                    .GetLocation()
                    .GetLineSpan()
                    .StartLinePosition.Line;

                (result ??= new List<string>()).Add(
                    $"Expected aliased using directive " +
                    $"{Quote(node.ToString().TrimEnd())} " +
                    $"before the non-aliased using directive " +
                    $"{Quote(previousNonaliased.ToString().TrimEnd())} " +
                    $"at line {line + 1}.");
            }

            if (previousAliased != null
                && string.Compare(
                    previousAliased.Name.ToString(),
                    node.Name.ToString(),
                    StringComparison.InvariantCulture) > 0)
            {
                var line = previousAliased
                    .GetLocation()
                    .GetLineSpan()
                    .StartLinePosition.Line;

                (result ??= new List<string>()).Add(
                    $"Expected aliased using directive " +
                    $"{Quote(node.ToString().TrimEnd())} " +
                    $"before the previous aliased using directive " +
                    Quote(previousAliased.ToString().TrimEnd()) +
                    $" at line {line + 1} (by alphabetical order).");
            }

            return result;
        }


        /// <summary>
        /// Inspects the syntax tree and reports the unexpected using directives.
        /// </summary>
        /// <param name="tree">Parsed syntax tree</param>
        /// <returns>List of inspected using directives</returns>
        public static List<Record> Inspect(SyntaxTree tree)
        {
            ErrorRegistry errors = new ErrorRegistry();

            var root = (CompilationUnitSyntax)tree.GetRoot();

            IEnumerable<MarkedUsing> markedUsings =
                root.DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(node =>
                    {
                        if (!node.HasTrailingTrivia)
                        {
                            return new MarkedUsing(node, null);
                        }

                        var nodeLine = node
                            .GetLocation().GetLineSpan()
                            .StartLinePosition.Line;

                        var sameLineTrivia =
                            node
                                .GetTrailingTrivia()
                                .Where(trivia =>
                                {
                                    var line = trivia.GetLocation().GetLineSpan()
                                        .StartLinePosition.Line;

                                    return line == nodeLine;
                                })
                                .ToList();

                        if (sameLineTrivia.Count == 0)
                        {
                            return new MarkedUsing(node, null);
                        }

                        var markingKind =
                            ParseMarkingFromTrailingTrivia(sameLineTrivia);

                        if (markingKind is null)
                        {
                            return new MarkedUsing(node, null);
                        }

                        return new MarkedUsing(
                            node, new Marking(
                                (MarkingKind)markingKind,
                                node.GetTrailingTrivia()));
                    });

            UsingDirectiveSyntax? previousAliased = null;
            UsingDirectiveSyntax? previousNonaliased = null;

            foreach (var markedUsing in markedUsings)
            {
                var node = markedUsing.Node;
                var marking = markedUsing.MaybeMarking;

                var position = node.GetLocation().GetLineSpan().StartLinePosition;

                string? maybeError = CheckMarking(node, marking);
                if (maybeError != null)
                {
                    errors.Add(
                        position.Line, position.Character,
                        maybeError);
                }

                var maybeErrors = CheckOrder(
                    node, previousAliased, previousNonaliased);
                if (maybeErrors != null)
                {
                    foreach (var error in maybeErrors)
                    {
                        errors.Add(
                            position.Line, position.Character, error);
                    }
                }

                // Update previous references
                if (node.Alias is null)
                {
                    previousNonaliased = node;
                }
                else
                {
                    previousAliased = node;
                }
            }

            return errors.Records().ToList();
        }
    }
}