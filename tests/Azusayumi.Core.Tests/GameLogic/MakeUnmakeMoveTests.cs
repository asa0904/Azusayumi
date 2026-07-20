using Azusayumi.Core.Evaluation;
using Azusayumi.Core.GameLogic;

namespace Azusayumi.Core.Tests.GameLogic
{
    public class MakeUnmakeMoveTests
    {
        [Theory]
        [InlineData("Quiet move",     "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",            "d2d4")]
        [InlineData("Capture move",   "rnbqk2r/ppp2ppp/4pn2/3p4/2PP4/2b1PN2/PP3PPP/R1BQKB1R w KQkq - 0 1",   "b2c3")]
        [InlineData("Castling move",  "r1bqkb1r/1ppp1ppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 0 1", "e1h1")]
        [InlineData("EnPassant move", "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1",                                   "e5d6")]
        [InlineData("Promotion move", "5b2/3KPk2/8/8/8/8/8/8 w - - 0 1",                                     "e7e8q")]
        public void MakeMove_SingleMove_MatchesRecalculatedKey(string scenario, string fen, string stringMove)
        {
            Board board = new(fen, historyCapacity: 2);
            Move  move  = board.ToMove(stringMove);

            board.MakeMove<White>(move);

            ulong expected = CalculateKey(board);
            ulong actual   = board.Key;

            Assert.True(expected == actual, $"Failed test: {scenario}");
        }

        [Theory]
        [InlineData("Quiet move",     "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",            "d2d4")]
        [InlineData("Capture move",   "rnbqk2r/ppp2ppp/4pn2/3p4/2PP4/2b1PN2/PP3PPP/R1BQKB1R w KQkq - 0 1",   "b2c3")]
        [InlineData("Castling move",  "r1bqkb1r/1ppp1ppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 0 1", "e1h1")]
        [InlineData("EnPassant move", "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1",                                   "e5d6")]
        [InlineData("Promotion move", "5b2/3KPk2/8/8/8/8/8/8 w - - 0 1",                                     "e7e8q")]
        public void MakeMove_SingleMove_MatchesRecalculatedGamePhase(string scenario, string fen, string stringMove)
        {
            Board board = new(fen, historyCapacity: 2);
            Move  move  = board.ToMove(stringMove);

            board.MakeMove<White>(move);

            int expectedWhitePhase = board.GetPhase<White>();
            int actualWhitePhase   = CalculatePhase(isWhite: true, board);
            
            Assert.True(expectedWhitePhase == actualWhitePhase, $"Failed test: {scenario}");

            int expectedBlackPhase = board.GetPhase<Black>();
            int actualBlackPhase   = CalculatePhase(isWhite: false, board);
            
            Assert.True(expectedBlackPhase == actualBlackPhase, $"Failed test: {scenario}");
        }

        [Theory]
        [InlineData("Quiet move",     "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",            "d2d4")]
        [InlineData("Capture move",   "rnbqk2r/ppp2ppp/4pn2/3p4/2PP4/2b1PN2/PP3PPP/R1BQKB1R w KQkq - 0 1",   "b2c3")]
        [InlineData("Castling move",  "r1bqkb1r/1ppp1ppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 0 1", "e1h1")]
        [InlineData("EnPassant move", "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1",                                   "e5d6")]
        [InlineData("Promotion move", "5b2/3KPk2/8/8/8/8/8/8 w - - 0 1",                                     "e7e8q")]
        public void MakeMove_SingleMove_MatchesRecalculatedPstScore(string scenario, string fen, string uciMove)
        {
            Board board = new(fen, historyCapacity: 2);
            Move  move  = board.ToMove(uciMove);

            board.MakeMove<White>(move);

            Score expectedScore = CalculatePstScore(board);
            Score actualScore   = board.Score;

            Assert.True(expectedScore.Mid == actualScore.Mid
                     && expectedScore.End == actualScore.End, $"Failed test: {scenario}");
        }

        [Theory]
        [InlineData("Quiet move",     "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",            "d2d4")]
        [InlineData("Capture move",   "rnbqk2r/ppp2ppp/4pn2/3p4/2PP4/2b1PN2/PP3PPP/R1BQKB1R w KQkq - 0 1",   "b2c3")]
        [InlineData("Castling move",  "r1bqkb1r/1ppp1ppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 0 1", "e1h1")]
        [InlineData("EnPassant move", "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1",                                   "e5d6")]
        [InlineData("Promotion move", "5b2/3KPk2/8/8/8/8/8/8 w - - 0 1",                                     "e7e8q")]
        public void UnmakeMove_SingleMove_RestoresOriginalBoard(string scenario, string fen, string stringMove)
        {
            Board board = new(fen, historyCapacity: 2);
            Move  move  = board.ToMove(stringMove);
            
            board.MakeMove<White>(move);
            board.UnmakeMove<White>(move);

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
