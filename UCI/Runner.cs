using System.Text;

namespace Azusayumi.UCI
{
    internal static class Runner
    {
        private static bool IsRunning = true;

        public static async Task Main()
        {
            Console.InputEncoding  = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            await Engine.Clear(); // 初期化

            while (IsRunning)
            {
                string? input = Console.ReadLine();
                if (input == null) continue;

                try
                {
                    await ProcessCommand(input.Trim());
                }
                catch (Exception ex)
                {
                    await Engine.Clear();
                    Console.WriteLine($"An error occurred while processing the UCI command: {ex.Message}");
                }
            }
        }

        private static async Task ProcessCommand(string command)
        {
            string[] tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (tokens[0])
            {
                case "uci":
                    Engine.PrintUciInfo();
                    Console.WriteLine("uciok");
                    break;
                
                case "isready":
                    Console.WriteLine("readyok");
                    break;

                case "setoption":
                    SetOption(tokens);
                    break;
                
                case "ucinewgame":
                    await Engine.Clear();
                    break;
                
                case "position":
                    SetPosition(tokens);
                    break;
                
                case "go":
                    RunEngine(tokens);
                    break;

                case "stop":
                    await Engine.Stop();
                    break;

                case "ponderhit":
                    Engine.PonderHit();
                    break;

                case "quit":
                    await Engine.Stop();
                    IsRunning = false;
                    break;

                // 以下はデバッグ用の独自コマンド
                case "draw":
                    Engine.PrintPosition();
                    break;

                case "eval":
                    Engine.PrintEvaluation();
                    break;

                case "hash":
                    Engine.PrintTTEntry();
                    break;
                
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        private static void SetOption(string[] tokens)
        {
            if (tokens[1] != "name") return;

            int index = 2;
            while (index < tokens.Length && tokens[index] != "value") index++;

            string name = string.Join(' ', tokens[2..index]);
            switch (name)
            {
                case "Hash" when ++index < tokens.Length: 
                    Engine.ChangeHashSize(sizeMB: Convert.ToInt32(tokens[index]));
                    break;

                case "Clear Hash":
                    Engine.ClearHash();
                    break;

                case "Ponder" when ++index < tokens.Length:
                    Engine.SetPonderMode(Convert.ToBoolean(tokens[index]));
                    break;

                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }

        private static void SetPosition(string[] tokens)
        {
            if (tokens.Length == 1) return;

            int index = 1;

            string fen;
            switch (tokens[index++])
            {
                case "startpos":
                    fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
                    break;
                
                case "fen":
                    if (tokens.Length < index + 6) { Console.WriteLine("Invalid fen."); return; }
                    fen = string.Join(' ', tokens, index, count: 6);
                    index += 6;
                    break;
                
                default:
                    return;
            }

            List<string> moves = [];
            if (index < tokens.Length && tokens[index++] == "moves")
                while (index < tokens.Length) moves.Add(tokens[index++]);

            Engine.SetPosition(fen, moves);
        }

        private static void RunEngine(string[] tokens)
        {
            int  depth    = 63;
            int  moveTime = int.MaxValue;
            int  nodes    = int.MaxValue;
            int  wtime    = int.MaxValue;
            int  btime    = int.MaxValue;
            int  winc     = 0;
            int  binc     = 0;
            bool isPonder = false;

            for (int i = 1; i < tokens.Length; i++)
            {
                switch (tokens[i])
                {
                    case "depth" when int.TryParse(tokens[++i], out int d):
                        depth = Math.Min(d, depth); break;
                    
                    case "movetime" when int.TryParse(tokens[++i], out int mt):
                        moveTime = Math.Min(mt, moveTime); break;
                    
                    case "nodes" when int.TryParse(tokens[++i], out int n):
                        nodes = Math.Min(n, nodes); break;
                    
                    case "wtime" when int.TryParse(tokens[++i], out int wt):
                        wtime = wt; break;
                    
                    case "btime" when int.TryParse(tokens[++i], out int bt):
                        btime = bt; break;
                    
                    case "winc" when int.TryParse(tokens[++i], out int wi):
                        winc = wi; break;
                    
                    case "binc" when int.TryParse(tokens[++i], out int bi):
                        binc = bi; break;

                    case "ponder":
                        isPonder = true; break;
                    
                    case "infinite":
                    default: break;
                }
            }

            _ = Engine.Go(depth, moveTime, nodes, wtime, btime, winc, binc, isPonder);
        }
    }
}
