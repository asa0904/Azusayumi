using Azusayumi.Core.GameLogic;
using Azusayumi.Core.Search;
using System.Text;
using System.Text.RegularExpressions;

namespace Azusayumi.DatasetBuilder
{
    internal partial class DatasetBuilder(bool append, int maxScore)
    {
        private readonly bool          _append        = append;
        private readonly int           _maxScore      = maxScore;
        private readonly Board         _board         = new();
        private readonly SearchManager _searchManager = new();

        internal void Build(string source, string output)
        {
            int count = 0;
            int games = 0;

            using StreamWriter writer = new(output, _append);

            foreach ((string pgn, string result) in EnumerateGames(source))
            {
                count += Build(writer, pgn, result);
                games++;

                if ((games & 0b111111) == 0)
                {
                    Console.Write($"\rBuilding dataset... Games: {games:N0}, Samples: {count:N0}");
                }
            }

            Console.WriteLine($"\rBuilding complete. (Games: {games:N0}, Samples: {count:N0})");
            Console.WriteLine($"Saved to {output}");
        }

        private int Build(StreamWriter writer, string pgn, string result)
        {
            float floatResult = result switch
            {
                "1-0"     => 1.0F,
                "0-1"     => 0.0F,
                "1/2-1/2" => 0.5F,
                _ => throw new ArgumentException($"Unknown PGN result: {result}")
            };

            _board.Set("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

            int count = 0;
            MatchCollection matches = MoveCommentRegex().Matches(pgn);
            for (int i = 0; i < matches.Count; i++)
            {
                string notation = matches[i].Groups["move"].Value.Trim();
                string comment  = matches[i].Groups["comment"].Value.Trim();

                if (_board.TryParse(notation, out Move move))
                {
                    _board.MakeMove(move);
                }
                else { return count; }

                if (BookOrMateRegex().IsMatch(comment)) { continue; }

                int score = Core.Evaluation.Evaluator.Evaluate(_board);
                if (score != _searchManager.GetQuiescentScore(_board)) { continue; }

                if (Math.Abs(score) > _maxScore) { continue; }

                writer.WriteLine($"{_board} ; {floatResult}");
                count++;
            }

            return count;
        }

        private static IEnumerable<(string Pgn, string Result)> EnumerateGames(string source)
        {
            using StreamReader reader = new(source);

            StringBuilder moves = new();

            bool inGame = false;
            string result = "*";

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("[Event "))
                {
                    if (inGame && moves.Length > 0)
                    {
                        yield return (moves.ToString(), result);

                        moves.Clear();
                        result = "*";
                    }

                    inGame = true;
                    continue;
                }

                if (!inGame) { continue; }

                if (line.StartsWith("[Result "))
                {
                    result = ExtractHeaderValue(line);
                    continue;
                }

                if (line.StartsWith('[')) { continue; }

                if (string.IsNullOrWhiteSpace(line)) { continue; }

                moves.Append(line);
                moves.Append(' ');
            }

            if (inGame && moves.Length > 0)
            {
                yield return (moves.ToString(), result);
            }
        }

        private static string ExtractHeaderValue(string line)
        {
            int firstQuote = line.IndexOf('"');
            int lastQuote  = line.LastIndexOf('"');

            if (firstQuote < 0 || lastQuote <= firstQuote) { return "*"; }

            return line[(firstQuote + 1)..lastQuote];
        }

        [GeneratedRegex(@"(?<move>[^\s{}]+)\s*\{(?<comment>[^}]*)\}")]
        private static partial Regex MoveCommentRegex();

        [GeneratedRegex(@"(book|(\+|\-)M\d*)", RegexOptions.IgnoreCase, "ja-JP")]
        private static partial Regex BookOrMateRegex();
    }
}
