using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Tests.GameLogic
{
    public class MakeUnmakeMoveTests
    {
        public static TheoryData<string, string, string> GetIncrementalUpdateTestData()
        {
            return new()
            {
                { "White quiet move",     "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",            "d2d4"  },
                { "Black quiet move",     "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq - 0 1",            "d7d5"  },
                { "White capture move",   "rnbqk2r/ppp2ppp/4pn2/3p4/2PP4/2b1PN2/PP3PPP/R1BQKB1R w KQkq - 0 1",   "b2c3"  },
                { "Black capture move",   "r1bqkb1r/pp3ppp/2B1pn2/2pp4/3P4/4PN2/PPP2PPP/RNBQK2R b KQkq - 0 1",   "b7c6"  },
                { "White castling move",  "r1bqkb1r/1ppp1ppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 0 1", "e1h1"  },
                { "Black castling move",  "rnbqk2r/pppp1ppp/5n2/b3p3/4P3/P1N2N2/1PPP1PPP/R1BQKB1R b KQkq - 0 1", "e8h8"  },
                { "White enPassant move", "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1",                                   "e5d6"  },
                { "Black enPassant move", "4k3/8/8/8/3Pp3/8/8/4K3 b - d3 0 1",                                   "e4d3"  },
                { "White promotion move", "4k3/2P5/8/8/8/8/8/4K3 w - - 0 1",                                     "c7c8q" },
                { "Black promotion move", "4k3/8/8/8/8/8/2p5/4K3 b - - 0 1",                                     "c2c1q" },
            };
        }

        [Theory]
        [MemberData(nameof(GetIncrementalUpdateTestData))]
        public void MakeMove_SingleMove_MatchesRecalculatedKey(string scenario, string fen, string stringMove)
        {
            Board board = new(fen, historyCapacity: 3);
            Move  move  = board.ToMove(stringMove);

            board.MakeMove(move);

            ulong expected = CalculateKey(board);
            ulong actual   = board.Key;

            Assert.True(expected == actual, $"Failed test: {scenario}");
        }

        [Theory]
        [MemberData(nameof(GetIncrementalUpdateTestData))]
        public void MakeMove_SingleMove_MatchesRecalculatedGamePhase(string scenario, string fen, string stringMove)
        {
            Board board = new(fen, historyCapacity: 3);
            Move  move  = board.ToMove(stringMove);

            board.MakeMove(move);

            int expectedWhitePhase = board.GetPhase<White>();
            int actualWhitePhase   = CalculatePhase(isWhite: true, board);
            
            Assert.True(expectedWhitePhase == actualWhitePhase, $"Failed test: {scenario}");

            int expectedBlackPhase = board.GetPhase<Black>();
            int actualBlackPhase   = CalculatePhase(isWhite: false, board);
            
            Assert.True(expectedBlackPhase == actualBlackPhase, $"Failed test: {scenario}");
        }

        [Theory]
        [MemberData(nameof(GetIncrementalUpdateTestData))]
        public void MakeMove_SingleMove_MatchesRecalculatedPstScore(string scenario, string fen, string stringMove)
        {
            Board board = new(fen, historyCapacity: 3);
            Move  move  = board.ToMove(stringMove);

            board.MakeMove(move);

            Score expectedScore = CalculatePstScore(board);
            Score actualScore   = board.Score;

            Assert.True(expectedScore.Mid == actualScore.Mid
                     && expectedScore.End == actualScore.End, $"Failed test: {scenario}");
        }

        [Theory]
        [MemberData(nameof(GetIncrementalUpdateTestData))]
        public void UnmakeMove_SingleMove_RestoresOriginalBoard(string scenario, string fen, string stringMove)
        {
            Board board = new(fen, historyCapacity: 3);
            Move  move  = board.ToMove(stringMove);
            
            board.MakeMove(move);
            board.UnmakeMove(move);

            string expected = fen;
            string actual   = board.ToString();

            Assert.True(expected == actual, $"Failed test: {scenario}");
        }

        private static ulong CalculateKey(Board board)
        {
            ulong key = 0UL;

            if (!board.IsWhiteToMove) { key ^= Zobrist.GetTurnKey(); }

            ulong pieces = board.GetFriends<White>();
            while (pieces != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref pieces);
                int pieceType   = board.GetPieceType(squareIndex);
                key ^= Zobrist.GetPositionKey<White>(pieceType, squareIndex);
            }
            pieces = board.GetFriends<Black>();
            while (pieces != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref pieces);
                int pieceType   = board.GetPieceType(squareIndex);
                key ^= Zobrist.GetPositionKey<Black>(pieceType, squareIndex);
            }

            key ^= Zobrist.GetCastlingRightKey(board.CastlingRights);

            int enPassantIndex = board.EnPassantIndex;
            if (enPassantIndex != Square.None)
            {
                key ^= Zobrist.GetEnPassantKey(enPassantIndex);
            }

            return key;
        }

        private static int CalculatePhase(bool isWhite, Board board)
        {
            int phase = 0;
            for (int pieceType = PieceType.Pawn; pieceType < PieceType.Length; pieceType++)
            {
                ulong pieces = isWhite ? board.GetFriends<White>(pieceType) : board.GetFriends<Black>(pieceType);
                phase += Bitboard.PopCount(pieces) * GamePhase.GetWeight(pieceType);
            }

            return phase;
        }

        private static Score CalculatePstScore(Board board)
        {
            Score score = Score.Zero;

            ulong pieces = board.GetFriends<White>();
            while (pieces != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref pieces);
                int pieceType   = board.GetPieceType(squareIndex);
                score += PieceSquareTables.GetScore<White>(pieceType, squareIndex);
            }

            pieces = board.GetFriends<Black>();
            while (pieces != 0)
            {
                int squareIndex = Bitboard.PopLsb(ref pieces);
                int pieceType   = board.GetPieceType(squareIndex);
                score += PieceSquareTables.GetScore<Black>(pieceType, squareIndex);
            }

            return score;
        }
    }
}
