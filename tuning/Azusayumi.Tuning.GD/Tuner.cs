namespace Azusayumi.Tuning.GD
{
    internal class Tuner(double learningRate, int epochs)
    {
        private readonly double _learningRate = learningRate;
        private readonly int    _epochs       = epochs;

        internal void Tune(Dataset dataset, Weights weights)
        {
            using ThreadLocal<Model>          models    = new(() => new Model(weights));
            using ThreadLocal<GradientBuffer> gradients = new(() => new GradientBuffer(weights));

            object         aggregateLock   = new();
            Optimizer      optimizer       = new(weights);
            GradientBuffer globalGradients = new(weights);

            for (int epoch = 0; epoch < _epochs; epoch++)
            {
                double totalLoss = 0.0;
                _ = Parallel.For(fromInclusive: 0, toExclusive: dataset.TrainData.Length,
                    localInit: () =>
                    {
                        Model          localModel     = models.Value!;
                        GradientBuffer localGradients = gradients.Value!;

                        localGradients.Clear();

                        return (Model: localModel, Gradients: localGradients, Loss: 0.0);
                    },
                    body: (i, state, local) =>
                    {
                        local.Loss += local.Model.AccumulateGradients(dataset.TrainData[i], local.Gradients);
                        return local;
                    },
                    localFinally: local =>
                    {
                        lock (aggregateLock)
                        {
                            totalLoss += local.Loss;
                            globalGradients.MergeFrom(local.Gradients);
                        }
                    }
                );

                optimizer.Update(_learningRate, dataset.TrainData.Length, weights, globalGradients);
                globalGradients.Clear();

                Console.WriteLine($"Epoch: {epoch}, Loss: {totalLoss / dataset.TrainData.Length}");
            }
        }
    }
}
