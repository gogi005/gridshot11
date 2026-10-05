namespace AimTrainer.Utils
{
    /// <summary>
    /// Plain C# class (no Node) — tracks all round statistics.
    /// Owned by GameManager; the mode calls RegisterHit / RegisterMiss.
    /// </summary>
    public sealed class ScoreTracker
    {
        public int Score { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }

        public void RegisterHit(int pointsPerHit)
        {
            Hits++;
            Streak++;
            if (Streak > BestStreak)
                BestStreak = Streak;
            Score += pointsPerHit;
        }

        public void RegisterMiss()
        {
            Misses++;
            Streak = 0;
        }

        /// <summary>0..100. Returns 0 when no shots fired yet.</summary>
        public float CalculateAccuracy()
        {
            int total = Hits + Misses;
            return total == 0 ? 0f : (Hits * 100f) / total;
        }

        public float CalculateHitsPerMinute(float durationSeconds)
        {
            if (durationSeconds <= 0f)
                return 0f;
            return Hits * 60f / durationSeconds;
        }

        public void Reset()
        {
            Score = 0;
            Hits = 0;
            Misses = 0;
            Streak = 0;
            BestStreak = 0;
        }
    }
}
