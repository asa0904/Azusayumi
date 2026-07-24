using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;
using System.Text;

namespace Azusayumi.Core.Tests.Evaluation
{
    public class EvaluatorTests
    {
        public static TheoryData<string> GetSymmetryTestData()
        {
            return new()
            {
                { "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1" },
                { "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1" },
            };
        }

        [Theory]
        [MemberData(nameof(GetSymmetryTestData))]
        public void Evaluate_MirroredPosition_ReturnsSameEvaluation(string fen)
        {
            Board board = new(historyCapacity: 2);
            string fenOriginal = fen;
            string fenMirrored = GetMirroredFEN(fenOriginal);

            board.Set(fenOriginal);
            int evalOriginal = Evaluator.Evaluate(board);

            board.Set(fenMirrored);
            int evalMirrored = Evaluator.Evaluate(board);

            Assert.True(evalOriginal == -evalMirrored,
                $"Failed symmetry test.\nOriginal: {evalOriginal} (FEN: {fenOriginal})\nMirrored: {evalMirrored} (FEN: {fenMirrored})"
            );
        }

        private static string GetMirroredFEN(string fen)
        {
            static char ToggleCase(char c)
            {
                return c switch
                {
                    >= 'A' and <= 'Z' => (char)('a' + (c - 'A')),
                    >= 'a' and <= 'z' => (char)('A' + (c - 'a')),
                    _ => c,
                };
            }

            StringBuilder mirroredFen = new();

            string[] parts = fen.Split(' ');
            string piecePlacement  = parts[0];
            string sideToMove      = parts[1];
            string castlingRights  = parts[2];
            string enPassantSquare = parts[3];

            string[] rows = piecePlacement.Split('/');
            Array.Reverse(rows);
            piecePlacement = string.Join('/', rows);
            foreach (char c in piecePlacement)
            {
                mirroredFen.Append(ToggleCase(c));
            }

            mirroredFen.Append(sideToMove == "w" ? " b " : " w ");

            foreach (char c in castlingRights)
            {
                mirroredFen.Append(ToggleCase(c));
            }
            mirroredFen.Append(' ');

            if (enPassantSquare == "-")
            {
                mirroredFen.Append('-');
            }
            else
            {
                int file = enPassantSquare[0] - 'a';
                int rank = enPassantSquare[1] - '1';
                int enPassantIndex = Square.GetIndex(rank, file);
                mirroredFen.Append(Square.ToCoordinate(enPassantIndex ^ 56));
            }

            mirroredFen.Append(" 0 1");

            return mirroredFen.ToString();
        }
    }
}
