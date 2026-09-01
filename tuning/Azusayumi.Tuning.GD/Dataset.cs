namespace Azusayumi.Tuning.GD
{
    internal record struct DatasetEntry(string Fen, float Result);

    internal class Dataset
    {
        private readonly DatasetEntry[] _dataset;

        internal Dataset(string filePath)
        {
            if (!File.Exists(filePath)) { throw new FileNotFoundException("Dataset file was not found.", filePath); }

            IEnumerable<string> dataset = File.ReadLines(filePath);
            _dataset = new DatasetEntry[dataset.Count()];

            int count = 0;
            foreach (ReadOnlySpan<char> line in dataset)
            {
                int separateIndex = line.IndexOf(";");

                _dataset[count].Fen    = line[..separateIndex].Trim().ToString();
                _dataset[count].Result = float.Parse(line[(separateIndex + 1)..].Trim());
                count++;

                if ((count & 0b111111) == 0) { Console.Write($"\rLoading dataset... {100 * count / _dataset.Length}%"); }
            }

            Console.WriteLine($"\rLoading complete. (Samples: {_dataset.Length})");
        }

        internal ReadOnlySpan<DatasetEntry> TrainData => _dataset.AsSpan();
    }
}
