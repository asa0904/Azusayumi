using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;

namespace Azusayumi.Cli
{
    internal readonly struct TraceContext : IEvaluationContext
    {
        private readonly (int Mid, int End)[] _whiteScores;
        private readonly (int Mid, int End)[] _blackScores;

        public TraceContext()
        {
            _whiteScores = new (int, int)[(int)Term.Length];
            _blackScores = new (int, int)[(int)Term.Length];
        }

        public void Add<TColor>(Term term, int index = 0, int count = 1) where TColor : struct, IColor
        {
            (int Mid, int End)[] scores = TColor.IsWhite ? _whiteScores : _blackScores;

            (int mid, int end) = Weights.GetValue(term, index);
            scores[(int)term].Mid += count * mid;
            scores[(int)term].End += count * end;
        }

        internal void Print()
        {
            static string ToString((int Mid, int End) score)
            {
                return $"{score.Mid / 100.0,5:0.00}  {score.End / 100.0,5:0.00}";
            }

            Console.WriteLine("+-------------------+--------------+--------------+--------------+");
            Console.WriteLine("|      Feature      |     White    |     Black    |     Total    |");
            Console.WriteLine("|                   |   MG    EG   |   MG    EG   |   MG    EG   |");
            Console.WriteLine("+-------------------+--------------+--------------+--------------+");

            (int Mid, int End) total = (0, 0);
            for (Term term = 0; term < Term.Length; term++)
            {
                (int Mid, int End) white = _whiteScores[(int)term];
                (int Mid, int End) black = _blackScores[(int)term];
                (int Mid, int End) diff  = (white.Mid - black.Mid, white.End - black.End);
                Console.WriteLine($"| {term,17} | {ToString(white)} | {ToString(black)} | {ToString(diff)} |");

                total.Mid += diff.Mid;
                total.End += diff.End;
            }

            Console.WriteLine( "+-------------------+--------------+--------------+--------------+");
            Console.WriteLine($"|      Total        |  ----  ----  |  ----  ----  | {ToString(total)} |");
            Console.WriteLine( "+-------------------+--------------+--------------+--------------+\n");
        }

        internal void Clear()
        {
            for (int i = 0; i < _whiteScores.Length; i++)
            {
                _whiteScores[i] = (0, 0);
            }
            for (int i = 0; i < _blackScores.Length; i++)
            {
                _blackScores[i] = (0, 0);
            }
        }
    }
}
