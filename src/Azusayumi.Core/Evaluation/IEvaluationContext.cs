namespace Azusayumi.Core.Evaluation
{
    public interface IEvaluationContext
    {
        void Add<TColor>(Term term, int index = 0, int count = 1) where TColor : struct, GameLogic.IColor;
    }
}
