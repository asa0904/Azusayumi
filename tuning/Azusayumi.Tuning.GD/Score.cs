namespace Azusayumi.Tuning.GD
{
    internal record struct Score(double Mid, double End)
    {
        internal static readonly Score Zero = new(0.0, 0.0);

        public static implicit operator Score((double Mid, double End) score)
        {
            return new(score.Mid, score.End);
        }

        public static Score operator +(Score left, Score right)
        {
            return new Score(left.Mid + right.Mid, left.End + right.End);
        }

        public static Score operator -(Score left, Score right)
        {
            return new Score(left.Mid - right.Mid, left.End - right.End);
        }

        public static Score operator *(int scaler, Score score)
        {
            return new Score(scaler * score.Mid, scaler * score.End);
        }

        public static Score operator *(double scaler, Score score)
        {
            return new Score(scaler * score.Mid, scaler * score.End);
        }

        public static Score operator *(Score score, double scaler)
        {
            return scaler * score;
        }

        public static Score operator *(Score left, Score right)
        {
            return new Score(left.Mid * right.Mid, left.End * right.End);
        }

        public static Score operator /(Score score, double divisor)
        {
            return new Score(score.Mid / divisor, score.End / divisor);
        }
    }
}
