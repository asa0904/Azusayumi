using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;
using System.Text;

namespace Azusayumi.Tuning.GD
{
    internal class Weights
    {
        private readonly Score[][] _weights;

        internal Weights()
        {
            _weights = new Score[(int)Term.Length][];
            for (Term term = 0; term < Term.Length; term++)
            {
                _weights[(int)term] = term == Term.Material
                                    ? [(100, 100), (325, 325), (350, 350), (500, 500), (900, 900)]
                                    : new Score[Core.Evaluation.Weights.GetLength(term)];
            }
        }

        internal Score this[Term term, int index]
        {
            get => _weights[(int)term][index];
            set => _weights[(int)term][index] = value;
        }

        internal int GetLength(Term term)
        {
            return _weights[(int)term].Length;
        }

        internal void Save(string path)
        {
            static string Round(Score score)
            {
                int mid = (int)Math.Round(score.Mid, MidpointRounding.AwayFromZero);
                int end = (int)Math.Round(score.End, MidpointRounding.AwayFromZero);
                return $"({mid,4}, {end,4})";
            }

            StringBuilder weights = new("# Azusayumi Weights\n");
            foreach (Term term in Enum.GetValues<Term>())
            {
                if (term == Term.Length) { continue; }

                string label = term.ToString();
                Span<Score> scores = _weights[(int)term];

                weights.Append($"\n{label}:");

                switch (scores.Length)
                {
                    case 1:
                        weights.AppendLine($" {Round(scores[0])}");
                        break;

                    case 64:
                        weights.AppendLine();
                        for (int rank = 0; rank < 8; rank++)
                        {
                            weights.Append("            ");
                            for (int file = 0; file < 8; file++)
                            {
                                Score score = scores[Square.GetIndex(rank, file)];
                                weights.Append($"{Round(score)}, ");
                            }
                            weights.AppendLine();
                        }
                        break;

                    default:
                        for (int i = 0; i < scores.Length; i++)
                        {
                            if (i % 9 == 0) { weights.AppendLine(); }
                            weights.Append($"{Round(scores[i])}, ");
                        }
                        weights.AppendLine();
                        break;
                }
            }

            File.WriteAllText(path, weights.ToString());
        }
    }
}
