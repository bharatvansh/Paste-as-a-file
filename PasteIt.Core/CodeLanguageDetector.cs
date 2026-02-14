using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PasteIt.Core
{
    public sealed class CodeLanguageDetector
    {
        private readonly IReadOnlyList<LanguageRule> _rules = new[]
        {
            new LanguageRule("Python", ".py", 6, new[]
            {
                new PatternScore(@"\bdef\s+\w+\s*\(", 4),
                new PatternScore(@"\bimport\s+\w+", 3),
                new PatternScore(@"\belif\b", 3),
                new PatternScore(@"\bprint\s*\(", 2),
                new PatternScore(@":\s*(#.*)?$", 1, RegexOptions.Multiline)
            }),
            new LanguageRule("JavaScript", ".js", 6, new[]
            {
                new PatternScore(@"\bfunction\s+\w+\s*\(", 4),
                new PatternScore(@"\b(const|let)\s+\w+", 3),
                new PatternScore(@"=>", 2),
                new PatternScore(@"\bconsole\.log\s*\(", 2),
                new PatternScore(@"\bexport\s+(default|const|function|class)\b", 2)
            }),
            new LanguageRule("TypeScript", ".ts", 6, new[]
            {
                new PatternScore(@"\binterface\s+\w+", 4),
                new PatternScore(@"\btype\s+\w+\s*=", 4),
                new PatternScore(@"\b(enum|implements)\s+\w+", 3),
                new PatternScore(@":[ \t]*(string|number|boolean|any|unknown|never|void|Record<|Array<)", 3),
                new PatternScore(@"\bimport\s+.+\s+from\s+['""]", 2),
                new PatternScore(@"\bexport\s+(type|interface|class|const|function)\b", 2)
            }),
            new LanguageRule("C#", ".cs", 6, new[]
            {
                new PatternScore(@"\busing\s+System\b", 4),
                new PatternScore(@"\bnamespace\s+\w+", 3),
                new PatternScore(@"\bpublic\s+class\s+\w+", 4),
                new PatternScore(@"\bConsole\.Write(Line)?\s*\(", 2)
            }),
            new LanguageRule("Java", ".java", 6, new[]
            {
                new PatternScore(@"\bpublic\s+class\s+\w+", 3),
                new PatternScore(@"\bpublic\s+static\s+void\s+main\s*\(", 5),
                new PatternScore(@"\bSystem\.out\.println\s*\(", 4),
                new PatternScore(@"\bimport\s+java\.\w+", 3),
                new PatternScore(@"\bpackage\s+[a-zA-Z_][\w\.]*\s*;", 3)
            }),
            new LanguageRule("C++", ".cpp", 6, new[]
            {
                new PatternScore(@"#include\s*<[\w\.]+>", 4),
                new PatternScore(@"\bint\s+main\s*\(", 4),
                new PatternScore(@"\bstd::", 2),
                new PatternScore(@"\bcout\s*<<", 2)
            }),
            new LanguageRule("C", ".c", 6, new[]
            {
                new PatternScore(@"#include\s*<stdio\.h>", 5),
                new PatternScore(@"\bint\s+main\s*\(", 4),
                new PatternScore(@"\bprintf\s*\(", 3),
                new PatternScore(@"\bscanf\s*\(", 2)
            }),
            new LanguageRule("HTML", ".html", 6, new[]
            {
                new PatternScore(@"<html[\s>]", 4),
                new PatternScore(@"<body[\s>]", 3),
                new PatternScore(@"<div[\s>]", 2),
                new PatternScore(@"<!DOCTYPE\s+html>", 4)
            }),
            new LanguageRule("CSS", ".css", 7, new[]
            {
                new PatternScore(@"\.[A-Za-z_][\w\-]*\s*\{", 3),
                new PatternScore(@"#[A-Za-z_][\w\-]*\s*\{", 2),
                new PatternScore(@"\b(color|margin|padding|display|font-family|background(-color)?|justify-content|align-items|grid-template-columns)\s*:", 3),
                new PatternScore(@"@media\s*\(", 4),
                new PatternScore(@":root\s*\{", 4),
                new PatternScore(@"--[\w\-]+\s*:", 4)
            }),
            new LanguageRule("XML", ".xml", 6, new[]
            {
                new PatternScore(@"<\?xml\s+version\s*=", 5),
                new PatternScore(@"</[A-Za-z_][\w\.\-:]*>", 3),
                new PatternScore(@"<[A-Za-z_][\w\.\-:]*[^>]*>", 2),
                new PatternScore(@"\sxmlns(:\w+)?=""[^""]+""", 3)
            }),
            new LanguageRule("SQL", ".sql", 6, new[]
            {
                new PatternScore(@"\bSELECT\b", 3),
                new PatternScore(@"\bINSERT\s+INTO\b", 4),
                new PatternScore(@"\bCREATE\s+TABLE\b", 4),
                new PatternScore(@"\bWHERE\b", 2),
                new PatternScore(@";\s*$", 1, RegexOptions.Multiline)
            }),
            new LanguageRule("Go", ".go", 6, new[]
            {
                new PatternScore(@"\bpackage\s+main\b", 4),
                new PatternScore(@"\bfunc\s+\w+\s*\(", 4),
                new PatternScore(@"\bfmt\.\w+\s*\(", 2),
                new PatternScore(@"\bimport\s+\(", 2)
            }),
            new LanguageRule("Rust", ".rs", 6, new[]
            {
                new PatternScore(@"\bfn\s+\w+\s*\(", 4),
                new PatternScore(@"\blet\s+mut\b", 3),
                new PatternScore(@"\bimpl\s+\w+", 3),
                new PatternScore(@"::", 1)
            }),
            new LanguageRule("Kotlin", ".kt", 6, new[]
            {
                new PatternScore(@"\bfun\s+\w+\s*\(", 4),
                new PatternScore(@"\b(val|var)\s+\w+\s*:\s*\w+", 4),
                new PatternScore(@"\bdata\s+class\s+\w+", 3),
                new PatternScore(@"\boverride\s+fun\b", 3),
                new PatternScore(@"\bprintln\s*\(", 2)
            }),
            new LanguageRule("Swift", ".swift", 6, new[]
            {
                new PatternScore(@"\bimport\s+Foundation\b", 4),
                new PatternScore(@"\bfunc\s+\w+\s*\(", 4),
                new PatternScore(@"\b(let|var)\s+\w+\s*:\s*\w+", 3),
                new PatternScore(@"\bguard\s+let\b", 3),
                new PatternScore(@"\bprint\s*\(", 2)
            }),
            new LanguageRule("PHP", ".php", 6, new[]
            {
                new PatternScore(@"<\?php", 5),
                new PatternScore(@"\$\w+\s*=", 3),
                new PatternScore(@"\becho\b", 2),
                new PatternScore(@"->\w+", 2),
                new PatternScore(@"\bnamespace\s+[\w\\]+", 2)
            }),
            new LanguageRule("Ruby", ".rb", 7, new[]
            {
                new PatternScore(@"\bdef\s+\w+", 3),
                new PatternScore(@"\bend\b", 3),
                new PatternScore(@"\bputs\s+['""]", 3),
                new PatternScore(@":[\w\-]+\s*=>", 2),
                new PatternScore(@"@\w+", 2)
            }),
            new LanguageRule("Shell", ".sh", 6, new[]
            {
                new PatternScore(@"^#!\s*/bin/(ba)?sh", 5, RegexOptions.Multiline),
                new PatternScore(@"\becho\s+['""]?.+", 2),
                new PatternScore(@"\$\([^)]+\)", 2),
                new PatternScore(@"\bif\s+\[.+\]\s*;\s*then", 3),
                new PatternScore(@"\bfi\b", 2),
                new PatternScore(@"\bexport\s+\w+=", 2)
            }),
            new LanguageRule("PowerShell", ".ps1", 6, new[]
            {
                new PatternScore(@"\b(Get|Set|New|Remove|Write)-[A-Za-z]+\b", 4),
                new PatternScore(@"\$\w+\s*=", 2),
                new PatternScore(@"\bparam\s*\(", 3),
                new PatternScore(@"\|\s*Where-Object\b", 2),
                new PatternScore(@"\|\s*ForEach-Object\b", 2)
            })
        };

        public CodeDetectionResult Detect(string? rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return CodeDetectionResult.NoMatch();
            }

            var text = rawText.Trim();

            if (LooksLikeJson(text))
            {
                return new CodeDetectionResult(
                    isCode: true,
                    language: "JSON",
                    extension: ".json",
                    score: 10,
                    threshold: 6);
            }

            if (LooksLikePlainSentence(text))
            {
                return CodeDetectionResult.NoMatch();
            }

            var scores = _rules
                .Select(rule => new Candidate(rule, Score(rule, text)))
                .OrderByDescending(candidate => candidate.Score)
                .ToList();

            if (scores.Count == 0)
            {
                return CodeDetectionResult.NoMatch();
            }

            var best = scores[0];
            var secondScore = scores.Count > 1 ? scores[1].Score : 0;

            if (best.Score < best.Rule.Threshold || best.Score - secondScore < 2)
            {
                return CodeDetectionResult.NoMatch();
            }

            return new CodeDetectionResult(
                isCode: true,
                language: best.Rule.Language,
                extension: best.Rule.Extension,
                score: best.Score,
                threshold: best.Rule.Threshold);
        }

        private static int Score(LanguageRule rule, string text)
        {
            var score = 0;
            foreach (var pattern in rule.Patterns)
            {
                var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | pattern.Options;
                if (Regex.IsMatch(text, pattern.Pattern, options))
                {
                    score += pattern.Weight;
                }
            }

            if (text.Contains("{") && text.Contains("}") && text.Contains(";"))
            {
                score += 1;
            }

            if (text.Contains("\n"))
            {
                score += 1;
            }

            return score;
        }

        private static bool LooksLikeJson(string text)
        {
            if ((text.StartsWith("{") && text.EndsWith("}")) ||
                (text.StartsWith("[") && text.EndsWith("]")))
            {
                var hasQuotes = text.Contains("\"");
                var hasColon = text.Contains(":");
                return hasQuotes && hasColon;
            }

            return false;
        }

        private static bool LooksLikePlainSentence(string text)
        {
            if (text.Length < 24)
            {
                return false;
            }

            var hasLineBreak = text.Contains("\n");
            var containsCommonPunctuation = text.Contains(".") || text.Contains("!") || text.Contains("?");
            var containsCodeTokens =
                text.Contains("{") ||
                text.Contains("}") ||
                text.Contains(";") ||
                text.Contains("=>") ||
                text.Contains("::") ||
                text.Contains("#include") ||
                text.Contains("using ") ||
                text.Contains("def ") ||
                text.Contains("<?php") ||
                text.Contains("#!/bin/bash") ||
                text.Contains("Get-") ||
                text.Contains("param(");

            return !hasLineBreak && containsCommonPunctuation && !containsCodeTokens;
        }

        private sealed class Candidate
        {
            public Candidate(LanguageRule rule, int score)
            {
                Rule = rule;
                Score = score;
            }

            public LanguageRule Rule { get; }

            public int Score { get; }
        }

        private sealed class LanguageRule
        {
            public LanguageRule(
                string language,
                string extension,
                int threshold,
                IReadOnlyList<PatternScore> patterns)
            {
                Language = language;
                Extension = extension;
                Threshold = threshold;
                Patterns = patterns;
            }

            public string Language { get; }

            public string Extension { get; }

            public int Threshold { get; }

            public IReadOnlyList<PatternScore> Patterns { get; }
        }

        private sealed class PatternScore
        {
            public PatternScore(string pattern, int weight)
                : this(pattern, weight, RegexOptions.None)
            {
            }

            public PatternScore(string pattern, int weight, RegexOptions options)
            {
                Pattern = pattern;
                Weight = weight;
                Options = options;
            }

            public string Pattern { get; }

            public int Weight { get; }

            public RegexOptions Options { get; }
        }
    }
}
