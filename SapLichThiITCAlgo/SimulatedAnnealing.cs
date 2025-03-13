using SapLichThiITCCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public required Dictionary<Exam, HashSet<Exam>> I_exam_linkages { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_requires { get; set; }
        public required Dictionary<Exam, HashSet<(Exam exam1, Exam exam2)>> I_exam_after { get; set; }
        public required HashSet<Exam> I_exam_exclusive { get; set; }
        public required Lake I_lake { get; set; }
        private Random _random = new Random();

        private List<AnnealingAction> Memory { get; set; } = new();

        public void RunSimulatedAnnealing(TimetablingData data, Lake lake, double startingTemperature, double terminateTemperature, double coolingCoef, double volatility, int markovChainLength = 10)
        {
            Evaluator evaluator = new Evaluator(data).Evaluate(Solution.FromLake(lake));
            int dtfPoint = evaluator.DistanceToFeasibility;
            int spPoint = evaluator.SoftPenalty;

            int numStep = (int)Math.Ceiling(Math.Log(terminateTemperature / startingTemperature, coolingCoef));
            Console.WriteLine($"Number of step: {numStep}");
            for (int i = 0; i < numStep; i++)
            {

                double currentTemp = startingTemperature * Math.Pow(coolingCoef, i);
                for (int j = 0; j < markovChainLength; j++)
                {

                    ForwardMove(lake,5);

                    evaluator = evaluator.Evaluate(Solution.FromLake(lake));
                    int adtfPoint = evaluator.DistanceToFeasibility;
                    int aspPoint = evaluator.SoftPenalty;

                    if (adtfPoint > dtfPoint)
                    {
                        RollbackMove(5);
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
                            RollbackMove(5);
                            continue;
                        }
                        if (aspPoint <= spPoint)
                        {
                            var propability = ProbilityFunction(spPoint, aspPoint, currentTemp, volatility);
                            var acceptSolution = _random.NextDouble() <= propability;
                            if (!acceptSolution)
                            {
                                RollbackMove(5);
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
