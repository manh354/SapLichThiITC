using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiITCAlgo
{
    public class SimulatedAnnealing
    {
        record AnnealingAction
        {
            public Pond FromPond { get; set; }
            public Puddle FromPuddle { get; set; }
            public Pond ToPond { get; set; }
            public Puddle ToPuddle { get; set; }
            public Exam Exam { get; set; }
        }

        record AnnealingActionShift
        {
            public Pond FromPond { get; set; }
            public Pond ToPond { get; set; }
        }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_linkages { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_requires { get; set; }
        public required Dictionary<Exam, HashSet<(Exam exam1, Exam exam2)>> I_exam_after { get; set; }
        public required HashSet<Exam> I_exam_exclusive { get; set; }

        private int P_stepCount = 1;
        public required Lake I_lake { get; set; }
        private Random _random = new Random();

        private List<AnnealingAction> Memory { get; set; } = new();
        private List<AnnealingActionShift> MemoryShift { get; set; } = new();

        public void RunSimulatedAnnealing(TimetablingData data, Lake lake, double startingTemperature, double terminateTemperature, double coolingCoef, double volatility, int markovChainLength = 10)
        {
            Evaluator evaluator = new Evaluator(data).Evaluate(Solution.FromLake(lake));
            Evaluator2 evaluator2 = new Evaluator2(lake, data.Exams, data.Periods, data.Rooms, data.InstitutionalWeightings, data.PeriodHardConstraints, data.RoomHardConstraints);

            int dtfPoint = evaluator.DistanceToFeasibility;
            int spPoint = evaluator.SoftPenalty;

            int numStep = (int)Math.Ceiling(Math.Log(terminateTemperature / startingTemperature, coolingCoef));
            Console.WriteLine($"Number of step: {numStep}");
            for (int i = 0; i < numStep; i++)
            {

                double currentTemp = startingTemperature * Math.Pow(coolingCoef, i);
                for (int j = 0; j < markovChainLength; j++)
                {

                    ForwardMove(lake, P_stepCount);

                    evaluator = evaluator.Evaluate(Solution.FromLake(lake));
                    int adtfPoint = evaluator.DistanceToFeasibility;
                    int aspPoint = evaluator.SoftPenalty;

                    if (adtfPoint > dtfPoint)
                    {
                        RollbackMove(P_stepCount);
                    }
                    else
                    {
                        if (adtfPoint < dtfPoint)
                        {
                            dtfPoint = adtfPoint;
                            spPoint = aspPoint;
                            continue;
                        }
                        if (aspPoint > spPoint)
                        {
                            RollbackMove(P_stepCount);
                            continue;
                        }
                        if (aspPoint <= spPoint)
                        {
                            var propability = ProbilityFunction(spPoint, aspPoint, currentTemp, volatility);
                            var acceptSolution = _random.NextDouble() <= propability;
                            if (!acceptSolution)
                            {
                                RollbackMove(P_stepCount);
                                continue;
                            }
                            dtfPoint = adtfPoint;
                            spPoint = aspPoint;
                            continue;
                        }
                    }
                }
                Console.WriteLine($"Distance to feasibility: {dtfPoint}");
                Console.WriteLine($"Soft Penalty: {spPoint}");
            }
        }

        public void RunSimulatedAnnealingShift(TimetablingData data, Lake lake, double startingTemperature, double terminateTemperature, double coolingCoef, double volatility, int markovChainLength = 10)
        {
            Evaluator evaluator = new Evaluator(data).Evaluate(Solution.FromLake(lake));
            Evaluator2 evaluator2 = new Evaluator2(lake, data.Exams, data.Periods, data.Rooms, data.InstitutionalWeightings, data.PeriodHardConstraints, data.RoomHardConstraints);

            int dtfPoint = evaluator.DistanceToFeasibility;
            int spPoint = evaluator.SoftPenalty;

            int numStep = (int)Math.Ceiling(Math.Log(terminateTemperature / startingTemperature, coolingCoef));
            Console.WriteLine($"Number of step: {numStep}");
            for (int i = 0; i < numStep; i++)
            {

                double currentTemp = startingTemperature * Math.Pow(coolingCoef, i);
                for (int j = 0; j < markovChainLength; j++)
                {

                    ForwardMoveShift(lake);

                    evaluator = evaluator.Evaluate(Solution.FromLake(lake));
                    int adtfPoint = evaluator.DistanceToFeasibility;
                    int aspPoint = evaluator.SoftPenalty;

                    if (adtfPoint > dtfPoint)
                    {
                        RollBackMoveShift();
                    }
                    else
                    {
                        if (adtfPoint < dtfPoint)
                        {
                            dtfPoint = adtfPoint;
                            spPoint = aspPoint;
                            continue;
                        }
                        if (aspPoint > spPoint)
                        {
                            RollBackMoveShift();
                            continue;
                        }
                        if (aspPoint <= spPoint)
                        {
                            var propability = ProbilityFunction(spPoint, aspPoint, currentTemp, volatility);
                            var acceptSolution = _random.NextDouble() <= propability;
                            if (!acceptSolution)
                            {
                                RollBackMoveShift();
                                continue;
                            }
                            dtfPoint = adtfPoint;
                            spPoint = aspPoint;
                            continue;
                        }
                    }
                }
                Console.WriteLine($"Distance to feasibility: {dtfPoint}");
                Console.WriteLine($"Soft Penalty: {spPoint}");
            }
        }

        private void ForwardMove(Lake lake)
        {
            var randFromPond = lake.Ponds[_random.Next(lake.Ponds.Count)];
            var randFromPuddle = randFromPond.Puddles[_random.Next(randFromPond.Puddles.Count)];
            if (randFromPuddle.Exams.Count == 0)
            {
                return;
            }
            var randExam = randFromPuddle.Exams[_random.Next(randFromPuddle.Exams.Count)];

            randFromPond.RemoveExam(randFromPuddle, randExam);

            lake.GetSuitablePondsForExam(randExam, out var chosenPond, out var suitablePonds, (_) => { return true; }, null);
            var randToPond = suitablePonds[_random.Next(suitablePonds.Count)];
            var randToPuddle = randToPond.Puddles[_random.Next(randToPond.Puddles.Count)];

            randToPond.AddExam(randToPuddle, randExam);

            Memory.Add(new AnnealingAction { Exam = randExam, FromPond = randFromPond, FromPuddle = randFromPuddle, ToPond = randToPond, ToPuddle = randToPuddle });
        }

        private void ForwardMove(Lake lake, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {

                var randFromPond = lake.Ponds[_random.Next(lake.Ponds.Count)];
                var randFromPuddle = randFromPond.Puddles[_random.Next(randFromPond.Puddles.Count)];
                if (randFromPuddle.Exams.Count == 0)
                {
                    i--;
                    continue;
                }
                var randExam = randFromPuddle.Exams[_random.Next(randFromPuddle.Exams.Count)];

                randFromPond.RemoveExam(randFromPuddle, randExam);

                lake.GetSuitablePondsForExam(randExam, out var chosenPond, out var suitablePonds, (_) => { return true; }, null);
                var randToPond = suitablePonds[_random.Next(suitablePonds.Count)];
                var randToPuddle = randToPond.Puddles[_random.Next(randToPond.Puddles.Count)];

                randToPond.AddExam(randToPuddle, randExam);

                Memory.Add(new AnnealingAction { Exam = randExam, FromPond = randFromPond, FromPuddle = randFromPuddle, ToPond = randToPond, ToPuddle = randToPuddle });
            }
        }

        private void ForwardMoveShift(Lake lake)
        {
            var randFromPond = lake.Ponds[_random.Next(lake.Ponds.Count)];
            var randToPond = lake.Ponds[_random.Next(lake.Ponds.Count)];

            (randFromPond.Period, randToPond.Period) = (randToPond.Period, randFromPond.Period);
            MemoryShift.Add(new() { FromPond = randFromPond, ToPond = randToPond });
        }

        private void RollbackMove(AnnealingAction action)
        {
            var fromPond = action.FromPond;
            var toPond = action.ToPond;
            var fromPuddle = action.FromPuddle;
            var toPuddle = action.ToPuddle;
            var exam = action.Exam;

            toPond.RemoveExam(toPuddle, exam);
            fromPond.AddExam(fromPuddle, exam);

        }

        private void RollbackMove(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                RollbackMove(Memory[Memory.Count - 1]);
                Memory.RemoveAt(Memory.Count - 1);
            }
        }

        private void RollBackMoveShift()
        {
            var action = MemoryShift[MemoryShift.Count - 1];
            var fromPond = action.FromPond;
            var toPond = action.ToPond;
            (fromPond.Period, toPond.Period) = (toPond.Period, fromPond.Period);
            MemoryShift.RemoveAt(MemoryShift.Count - 1);
        }
        private double ProbilityFunction(int oldPoint, int newPoint, double temperature, double volatility)
        {
            return Math.Exp((oldPoint - newPoint) * volatility / temperature);
        }
        public Comparer<Puddle> CreatePuddleComparer(bool largestPuddleFirst)
        {
            if (largestPuddleFirst)
                return Comparer<Puddle>.Create((firstPuddle, secondPuddle) =>
                {
                    if (firstPuddle.Room.Penalty == secondPuddle.Room.Penalty)
                        return firstPuddle.GetRemainingCapacity().CompareTo(secondPuddle.GetRemainingCapacity());
                    // A negative sign to represent we should choose the pond
                    // with smaller penalty first.
                    return -firstPuddle.Room.Penalty.CompareTo(secondPuddle.Room.Penalty);
                });
            else
                return Comparer<Puddle>.Create((firstPuddle, secondPuddle) =>
                {
                    if (firstPuddle.Room.Penalty == secondPuddle.Room.Penalty)
                        return secondPuddle.GetRemainingCapacity().CompareTo(firstPuddle.GetRemainingCapacity());
                    // A negative sign to represent we should choose the pond
                    // with smaller penalty first.
                    return -firstPuddle.Room.Penalty.CompareTo(firstPuddle.Room.Penalty);
                });
        }
    }
}
