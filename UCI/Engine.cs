using Azusayumi.Evaluation;
using Azusayumi.GameLogic;
using Azusayumi.Search;

namespace Azusayumi.UCI
{
    internal static class Engine
    {
        private static bool     IsPonderMode;
        private static Task?    SearchTask;
        private static Board    Board    = Board.Initial;
        private static Searcher Searcher = new(sizeMB: 32);
        
        public static void PrintUciInfo()
        {
            Console.WriteLine("id name Azusayumi 1.0");
            Console.WriteLine("id author Asato Kamamoto");
            Console.WriteLine("option name Hash type spin default 32 min 1 max 1024");
            Console.WriteLine("option name Clear Hash type button");
            Console.WriteLine("option name Ponder type check default false");
        }

        public static void ChangeHashSize(int sizeMB)
        {
            Searcher = new Searcher(sizeMB);
        }

        public static void ClearHash()
        {
            Searcher.Clear();
        }

        public static void SetPonderMode(bool isPonderMode)
        {
            IsPonderMode = isPonderMode;
        }

        public static void SetPosition(string fen, List<string> moves)
        {
            Board = new Board(fen);

            for (int i = 0; i < moves.Count; i++)
            {
                bool isLegal = false;

                MoveList legalMoves = new();
                MoveGenerator.GenerateAllMoves(legalMoves, Board);
                for (int j = 0; j < legalMoves.Count; j++)
                {
                    if (moves[i] == legalMoves[j].ToString())
                    {
                        isLegal = true;
                        Board.MakeMove(legalMoves[j]);
                        break;
                    }
                }

                if (!isLegal)
                {
                    Console.WriteLine($"Invalid move sequence. Move '{moves[i]}' is illegal. Ignoring all following moves.");
                    break;
                }
            }
        }

        public static async Task Go(int depth, int moveTime, int nodes, int wtime, int btime, int winc, int binc, bool isPonder)
        {
            if (SearchTask != null) await SearchTask;

            SearchTask = Task.Run(() =>
            {
                int time = Board.Side == Color.White ? wtime : btime;
                int inc  = Board.Side == Color.White ? winc  : binc;

                isPonder &= IsPonderMode;

                try
                {
                    Searcher.Search(Board, time, inc, moveTime, depth, nodes, isPonder);
                }
                catch (Exception ex)
                {
                    Board = Board.Initial;

                    Console.WriteLine($"An error occured during the search: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("bestmove 0000");
                }
            });
        }

        public static async Task Stop()
        {
            Searcher.Stop();
            
            if (SearchTask != null) await SearchTask;
            SearchTask = null;
        }

        public static void PonderHit()
        {
            if (IsPonderMode) Searcher.PonderHit();
        }

        public static async Task Clear()
        {
            await Stop();

            Board = Board.Initial;
            Searcher.Clear();
        }

        public static void PrintPosition()
        {
            Board.Print();
        }

        public static void PrintEvaluation()
        {
            Evaluator.Print(Board);
        }

        public static void PrintTTEntry()
        {
            Searcher.PrintTTEntry(Board);
        }
    }
}
