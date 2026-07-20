using Azusayumi.Core.Evaluation;

namespace Azusayumi.Core.Tests.Evaluation
{
    public class ScoreTests
    {
        [Fact]
        public void ImplicitTupleConversion_ProducesExpectedComponents()
        {
            Score score = (-1, -2);

            Assert.Equal(-1, score.Mid);
            Assert.Equal(-2, score.End);
        }

        [Fact]
        public void Addition_WorksComponentwise()
        {
            Score score1 = (1, 2);
            Score score2 = (3, 4);

            Score sum = score1 + score2;

            Assert.Equal(4, sum.Mid);
            Assert.Equal(6, sum.End);
        }

        [Fact]
        public void Subtraction_WorksComponentwise()
        {
            Score score1 = (1, 2);
            Score score2 = (3, 4);

            Score diff = score1 - score2;

            Assert.Equal(-2, diff.Mid);
            Assert.Equal(-2, diff.End);
        }

        [Fact]
        public void ScalarMultiplication_ScalesBothComponents()
        {
            Score score = (-1, -2);

            Score scaled = 3 * score;

            Assert.Equal(-3, scaled.Mid);
            Assert.Equal(-6, scaled.End);
        }
    }
}
