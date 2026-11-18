public sealed class BayesStrategy : ISkipStrategy
{
    private static readonly Subject[] Subjects = Enum.GetValues<Subject>(); 
    private static readonly int SubjectCount = Subjects.Length;
    private static readonly int PairsCount = (SubjectCount * (SubjectCount - 1)) / 2;
    private static readonly int HypothesisCount = 1 + SubjectCount + PairsCount;
    private const double RiskLimit = 0.002;

    public string Name => "Байесовская стартегия";
    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(day);
        ArgumentNullException.ThrowIfNull(history);

        // первоначальные вероятности гипотез
        var weightsHypothesis = new double[SubjectCount, HypothesisCount];
        for (int subject = 0; subject < SubjectCount; subject++)
        {
            weightsHypothesis[subject, 0] = 1.0 / 3;
            for (int hypothesis = 1; hypothesis <= SubjectCount; hypothesis++)
                weightsHypothesis[subject, hypothesis] = 1.0 / 3 / SubjectCount;
            for (int hypothesis = 1 + SubjectCount; hypothesis < HypothesisCount; hypothesis++)
                weightsHypothesis[subject, hypothesis] = 1.0 / 3 / PairsCount;
        }

        var yesterday = new bool[SubjectCount];
        var today = new bool[SubjectCount];
        var probs = new double[SubjectCount];

        for (int d = 0; d < day; d++)
        {
            InitProbs(yesterday, probs);
            for (int subject = 0; subject < SubjectCount; subject++)
            {
                today[subject] = history.WasAsked(d, Subjects[subject]) ?? false;
                double total = 0;
                // пересчитывание весов на основе данных всей имеющейся истории 
                for (int hypothesis = 0; hypothesis < HypothesisCount; hypothesis++)
                {
                    weightsHypothesis[subject, hypothesis] *= today[subject] ? probs[hypothesis] : 1 - probs[hypothesis];
                    total += weightsHypothesis[subject, hypothesis];
                }

                // нормировка весов
                if (total > 0)
                    for (int hypothesis = 0; hypothesis < HypothesisCount; hypothesis++) weightsHypothesis[subject, hypothesis] /= total;
            }
            (yesterday, today) = (today, yesterday);
        }

        InitProbs(yesterday, probs);
        var plan = new bool[SubjectCount];

        // принимаем решение о сегоднешнем дне
        for (int subject = 0; subject < SubjectCount; subject++)
        {
            double prob = 0;
            for (int hypothesis = 0; hypothesis < HypothesisCount; hypothesis++) prob += weightsHypothesis[subject, hypothesis] * probs[hypothesis];
            plan[subject] = prob >= RiskLimit;
        }

        return plan;
    }

    private static void InitProbs(bool[] yesterday, double[] probs)
    {
        probs[0] = 0.5;

        for (int s = 0; s < SubjectCount; s++)
            probs[1 + s] = yesterday[s] ? 1.0 : 0.0;
        
        int idx = 1 + SubjectCount;
        for (int a = 0; a < SubjectCount; a++)
            for (int b = 0; b < SubjectCount; b++)
                probs[idx++] = yesterday[a] ^ yesterday[b] ? 1.0 : 0.0;
    }
}