namespace Azusayumi.Core.Search
{
    internal class SearchStatistics
    {
        internal long InteriorNodes;
        internal long HorizonNodes;
        internal long QuiescentNodes;

        internal long CutNodes;
        internal long FirstCutNodes;
        internal long QCutNodes;
        internal long FirstQCutNodes;

        internal int  ResearchCount;
        internal long ResearchNodes;

        private readonly long[] _nodesByDepth = new long[SearchWorker.MaxPly];

        internal long TotalNodes => InteriorNodes + QuiescentNodes;

        internal void RecordNodes(int depth)
        {
            _nodesByDepth[depth] = InteriorNodes + HorizonNodes;
        }

        internal void Reset()
        {
            InteriorNodes  = 0L;
            HorizonNodes   = 0L;
            QuiescentNodes = 0L;

            CutNodes       = 0L;
            FirstCutNodes  = 0L;
            QCutNodes      = 0L;
            FirstQCutNodes = 0L;

            ResearchCount = 0;
            ResearchNodes = 0L;

            for (int i = 0; i < _nodesByDepth.Length; i++)
            {
                _nodesByDepth[i] = 0L;
            }
        }

        internal void Print()
        {
            long totalNodes = TotalNodes;
            Console.WriteLine($"------ Search Stats ------");
            Console.WriteLine($"Total Nodes       : {totalNodes:N0}");
            Console.WriteLine($"Interior Nodes    : {InteriorNodes :N0} ({(double)InteriorNodes  / totalNodes * 100:F1}%)");
            Console.WriteLine($"Horizon Nodes     : {HorizonNodes  :N0} ({(double)HorizonNodes   / totalNodes * 100:F1}%)");
            Console.WriteLine($"Quiescent Nodes   : {QuiescentNodes:N0} ({(double)QuiescentNodes / totalNodes * 100:F1}%)");
            Console.WriteLine($"Cut Nodes         : {CutNodes:N0}");
            Console.WriteLine($"First Cut Nodes   : {FirstCutNodes:N0} ({(double)FirstCutNodes / CutNodes * 100:F1}%)");
            Console.WriteLine($"Q-Cut Nodes       : {QCutNodes:N0}");
            Console.WriteLine($"First Q-Cut Nodes : {FirstQCutNodes:N0} ({(double)FirstQCutNodes / QCutNodes * 100:F1}%)");
            Console.WriteLine($"Re-Search Count   : {ResearchCount:N0}");
            Console.WriteLine($"Re-Search Nodes   : {ResearchNodes:N0}");
            PrintEffectiveBranchingFactor();
            Console.WriteLine($"--------------------------");
        }

        private void PrintEffectiveBranchingFactor()
        {
            Console.WriteLine("\nEffective Branching Factor");

            double total = 1.0;
            int depth = 2;
            while (_nodesByDepth[depth] > 0)
            {
                double ebf = (double)_nodesByDepth[depth] / _nodesByDepth[depth - 1];
                Console.WriteLine($"Depth {depth}/{depth - 1} : {ebf:F1}");
                total *= ebf;
                depth++;
            }
            Console.WriteLine($"Average : {Math.Pow(total, 1.0 / (depth - 2)):F1}");
        }
    }
}
