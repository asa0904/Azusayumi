using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    internal class Board
    {
        private          int         _ply;
        private          int         _sideToMove;
        private          int         _whiteKingIndex;
        private          int         _blackKingIndex;
        private          ulong       _occupancy;
        private          ulong       _whitePieces;
        private          ulong       _blackPieces;
        private readonly ulong[]     _whiteBitboards = new ulong[PieceType.Length];
        private readonly ulong[]     _blackBitboards = new ulong[PieceType.Length];
        private readonly int[]       _pieceTypes     = new int[Square.Length];
        private readonly GameState[] _gameStates;

        private struct GameState
        {
            internal ulong Key;
            internal byte  CastlingRights;
            internal byte  EnPassantIndex;
            internal byte  HalfmoveClock;
            internal byte  CapturedPiece;
        }

        internal Board(int historyCapacity = 1024)
        {
            _gameStates = new GameState[historyCapacity];
            Set("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        }

        internal Board(ReadOnlySpan<char> fen, int historyCapacity = 1024)
        {
            _gameStates = new GameState[historyCapacity];
            Set(fen);
        }

        internal bool IsWhiteToMove
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sideToMove == Color.White;
        }

        internal ulong Occupancy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _occupancy;
        }

        internal ulong Key
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _gameStates[_ply].Key;
        }

        internal int CastlingRights
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _gameStates[_ply].CastlingRights;
        }

        internal int EnPassantIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _gameStates[_ply].EnPassantIndex;
        }

        internal void Set(ReadOnlySpan<char> fen)
        {
            int spaceIndex = fen.IndexOf(' ');
            ReadOnlySpan<char> section = fen[..spaceIndex];
            fen = fen[(spaceIndex + 1)..];

            _occupancy   = 0UL;
            _whitePieces = 0UL;
            _blackPieces = 0UL;
            for (int i = 0; i < _whiteBitboards.Length; i++) { _whiteBitboards[i] = 0UL; }
            for (int i = 0; i < _blackBitboards.Length; i++) { _blackBitboards[i] = 0UL; }
            for (int i = 0; i < _pieceTypes.Length; i++) { _pieceTypes[i] = PieceType.None; }

            ulong key = 0UL;

            int squareIndex = Square.A8;
            foreach (char symbol in section)
            {
                if (symbol is >= '0' and <= '9')
                {
                    squareIndex += symbol - '0';
                }
                else if (symbol == '/')
                {
                    squareIndex -= 16;
                }
                else
                {
                    int color = symbol is >= 'A' and <= 'Z' ? Color.White : Color.Black;
                    int pieceType = symbol switch
                    {
                        'P' or 'p' => PieceType.Pawn,
                        'N' or 'n' => PieceType.Knight,
                        'B' or 'b' => PieceType.Bishop,
                        'R' or 'r' => PieceType.Rook,
                        'Q' or 'q' => PieceType.Queen,
                        'K' or 'k' => PieceType.King,
                        _ => PieceType.None
                    };

                    ulong square = 1UL << squareIndex;
                    _occupancy |= square;
                    if (color == Color.White)
                    {
                        _whitePieces |= square;
                        _whiteBitboards[pieceType] |= square;

                        key ^= Zobrist.GetPositionKey<White>(pieceType, squareIndex);
                    }
                    else
                    {
                        _blackPieces |= square;
                        _blackBitboards[pieceType] |= square;

                        key ^= Zobrist.GetPositionKey<Black>(pieceType, squareIndex);
                    }
                    
                    _pieceTypes[squareIndex] = pieceType;

                    squareIndex++;
                }
            }

            spaceIndex = fen.IndexOf(' ');
            section = fen[..spaceIndex];
            fen = fen[(spaceIndex + 1)..];
            _sideToMove = section.SequenceEqual("w") ? Color.White : Color.Black;
            if (_sideToMove == Color.Black) { key ^= Zobrist.GetTurnKey(); }

            spaceIndex = fen.IndexOf(' ');
            section = fen[..spaceIndex];
            fen = fen[(spaceIndex + 1)..];
            int castlingRights = 0;
            foreach (char right in section)
            {
                switch (right)
                {
                    case 'K': castlingRights |= Castling.WhiteO_O;   break;
                    case 'Q': castlingRights |= Castling.WhiteO_O_O; break;
                    case 'k': castlingRights |= Castling.BlackO_O;   break;
                    case 'q': castlingRights |= Castling.BlackO_O_O; break;
                    default: break;
                }
            }
            key ^= Zobrist.GetCastlingRightKey(castlingRights);

            spaceIndex = fen.IndexOf(' ');
            section = fen[..spaceIndex];
            fen = fen[(spaceIndex + 1)..];
            int enPassantIndex = Square.None;
            if (!section.SequenceEqual("-"))
            {
                int file = section[0] - 'a';
                int rank = section[1] - '1';
                enPassantIndex = Square.GetIndex(rank, file);
                key ^= Zobrist.GetEnPassantKey(enPassantIndex);
            }

            spaceIndex = fen.IndexOf(' ');
            section = fen[..spaceIndex];
            fen = fen[(spaceIndex + 1)..];
            int halfmoveClock = int.Parse(section);

            section = fen;
            int moveCount = int.Parse(section);
            _ply = 2 * (moveCount - 1);
            if (_sideToMove == Color.Black) { _ply++; }

            _gameStates[_ply] = new GameState()
            {
                Key            = key,
                CastlingRights = (byte)castlingRights,
                EnPassantIndex = (byte)enPassantIndex,
                HalfmoveClock  = (byte)halfmoveClock,
                CapturedPiece  = PieceType.None,
            };

            _whiteKingIndex = Bitboard.GetLsb(GetFriends<White>(PieceType.King));
            _blackKingIndex = Bitboard.GetLsb(GetFriends<Black>(PieceType.King));
        }

        public override string ToString()
        {
            System.Text.StringBuilder fen = new();

            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    int squareIndex = Square.GetIndex(rank, file);

                    int emptyCount;
                    for (emptyCount = 0; file < 8 && GetPieceType(squareIndex++) == PieceType.None; file++)
                    {
                        emptyCount++;
                    }
                    if (emptyCount > 0) { fen.Append(emptyCount); }

                    if (file < 8)
                    {
                        char symbol = "PNBRQK"[GetPieceType(--squareIndex)];
                        if (((1UL << squareIndex) & GetFriends<Black>()) != 0)
                        {
                            symbol = (char)(symbol + ('a' - 'A'));
                        }
                        fen.Append(symbol);
                    }
                }

                if (rank > 0) { fen.Append('/'); }
            }
            fen.Append(' ');

            fen.Append(_sideToMove == Color.White ? 'w' : 'b');
            fen.Append(' ');

            GameState state = _gameStates[_ply];

            if ((state.CastlingRights & Castling.WhiteO_O)   != 0) { fen.Append('K'); }
            if ((state.CastlingRights & Castling.WhiteO_O_O) != 0) { fen.Append('Q'); }
            if ((state.CastlingRights & Castling.BlackO_O)   != 0) { fen.Append('k'); }
            if ((state.CastlingRights & Castling.BlackO_O_O) != 0) { fen.Append('q'); }
            if (state.CastlingRights == 0) { fen.Append('-'); }
            fen.Append(' ');

            fen.Append(state.EnPassantIndex == Square.None ? "-" : Square.ToCoordinate(state.EnPassantIndex));
            fen.Append(' ');

            fen.Append(state.HalfmoveClock);
            fen.Append(' ');

            fen.Append(((_ply - _sideToMove) / 2) + 1);

            return fen.ToString();
        }

        internal void Print()
        {
            Console.WriteLine();

            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    int  squareIndex = Square.GetIndex(rank, file);
                    int  pieceType   = GetPieceType(squareIndex);
                    char simble      = "PNBRQK."[pieceType];

                    if (file == 0) { Console.Write($"{rank + 1} | "); }

                    ulong square = 1UL << squareIndex;
                    Console.ForegroundColor = (square & GetFriends<White>()) != 0 ? ConsoleColor.White
                                            : (square & GetFriends<Black>()) != 0 ? ConsoleColor.DarkCyan
                                                                                  : ConsoleColor.DarkGray;
                    Console.Write($"{simble} ");
                    Console.ResetColor();

                    if (file == 7) { Console.WriteLine(); }
                }
            }

            Console.WriteLine("   ----------------\n    a b c d e f g h");
            Console.WriteLine($"FEN: {this}\n");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong GetFriends<TColor>() where TColor : struct, IColor
        {
            return TColor.IsWhite ? _whitePieces : _blackPieces;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong GetFriends<TColor>(int pieceType) where TColor : struct, IColor
        {
            return TColor.IsWhite ? _whiteBitboards[pieceType] : _blackBitboards[pieceType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong GetEnemies<TColor>() where TColor : struct, IColor
        {
            return TColor.IsWhite ? _blackPieces : _whitePieces;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong GetEnemies<TColor>(int pieceType) where TColor : struct, IColor
        {
            return TColor.IsWhite ? _blackBitboards[pieceType] : _whiteBitboards[pieceType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetPieceType(int squareIndex)
        {
            return _pieceTypes[squareIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetKingIndex<TColor>() where TColor : struct, IColor
        {
            return TColor.IsWhite ? _whiteKingIndex : _blackKingIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsInCheck<TColor>() where TColor : struct, IColor
        {
            int   kingIndex = GetKingIndex<TColor>();
            ulong occupancy = _occupancy;
            return (Attacks.GetBishopAttacks(kingIndex, occupancy) & (GetEnemies<TColor>(PieceType.Queen) | GetEnemies<TColor>(PieceType.Bishop))) != 0
                || (Attacks.GetRookAttacks(kingIndex, occupancy)   & (GetEnemies<TColor>(PieceType.Queen) | GetEnemies<TColor>(PieceType.Rook)))   != 0
                || (Attacks.GetKnightAttacks(kingIndex)            & GetEnemies<TColor>(PieceType.Knight)) != 0
                || (Attacks.GetPawnAttacks<TColor>(kingIndex)      & GetEnemies<TColor>(PieceType.Pawn))   != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong CalculateAttackedBB<TColor>(ulong occupancy) where TColor : struct, IColor
        {
            ulong attackedBB = Attacks.GetKingAttacks(TColor.IsWhite ? GetKingIndex<Black>() : GetKingIndex<White>());

            ulong pieces = GetEnemies<TColor>(PieceType.Queen) | GetEnemies<TColor>(PieceType.Bishop);
            while (pieces != 0) { attackedBB |= Attacks.GetBishopAttacks(Bitboard.PopLsb(ref pieces), occupancy); }
            
            pieces = GetEnemies<TColor>(PieceType.Queen) | GetEnemies<TColor>(PieceType.Rook);
            while (pieces != 0) { attackedBB |= Attacks.GetRookAttacks(Bitboard.PopLsb(ref pieces), occupancy); }

            pieces = GetEnemies<TColor>(PieceType.Knight);
            while (pieces != 0) { attackedBB |= Attacks.GetKnightAttacks(Bitboard.PopLsb(ref pieces)); }

            pieces = GetEnemies<TColor>(PieceType.Pawn);
            attackedBB |= TColor.IsWhite ? (Black.GetPawnRightAttacks(pieces) | Black.GetPawnLeftAttacks(pieces))
                                         : (White.GetPawnRightAttacks(pieces) | White.GetPawnLeftAttacks(pieces));
            return attackedBB;
        }
    }
}
