using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetXml;

namespace SapLichThiITCAlgoNew
{
    public class SimulatedAnnealingXml
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

        public void RunSimulatedAnnealing(ExamTimetablingData data, Lake lake, double startingTemperature, double terminateTemperature, double coolingCoef, double volatility, int markovChainLength = 10)
        {
            EvaluatorXml evaluator = new EvaluatorXml(data);

            EvaluationResult result = evaluator.Evaluate();
            var currPen = result.TotalPenalty;

            int numStep = (int)Math.Ceiling(Math.Log(terminateTemperature / startingTemperature, coolingCoef));
            Console.WriteLine($"Number of step: {numStep}");
            for (int i = 0; i < numStep; i++)
            {

                double currentTemp = startingTemperature * Math.Pow(coolingCoef, i);
                for (int j = 0; j < markovChainLength; j++)
                {

                    ForwardMove(lake, P_stepCount);

                    EvaluationResult afterResult = evaluator.Evaluate();
                    var afterPen = afterResult.TotalPenalty;

                    if (afterPen < currPen )
                    {
                        result = afterResult;
                        currPen = afterPen;
                        continue;
                    }
                    if (afterPen >= currPen)
                    {
                        var propability = ProbilityFunction(currPen, afterPen, currentTemp, volatility);
                        var acceptSolution = _random.NextDouble() <= propability;
                        if (!acceptSolution)
                        {
                            RollbackMove(lake, P_stepCount);
                            continue;
                        }
                        result = afterResult;
                        currPen = afterPen;
                        continue;
                    }

                }
                Console.WriteLine($"Is Valid: {result.HardConstraintViolations.Count}");
                Console.WriteLine($"Penalty: {currPen}");
            }
        }

        public void RunSimulatedAnnealingShift(ExamTimetablingData data, Lake lake, double startingTemperature, double terminateTemperature, double coolingCoef, double volatility, int markovChainLength = 10)
        {
            EvaluatorXml evaluator = new EvaluatorXml(data);

            var result = evaluator.Evaluate();
            var currPen = result.TotalPenalty;

            int numStep = (int)Math.Ceiling(Math.Log(terminateTemperature / startingTemperature, coolingCoef));
            Console.WriteLine($"Number of step: {numStep}");
            for (int i = 0; i < numStep; i++)
            {

                double currentTemp = startingTemperature * Math.Pow(coolingCoef, i);
                for (int j = 0; j < markovChainLength; j++)
                {
                     ForwardMoveShift(lake);

                    var afterResult = evaluator.Evaluate();
                    var afterPen = afterResult.TotalPenalty;

                    if (currPen > afterPen)
                    {
                        result = afterResult;
                        currPen = afterPen;
                        continue;
                    }
                    if (currPen <= afterPen)
                    {
                        var propability = ProbilityFunction(currPen, afterPen, currentTemp, volatility);
                        var acceptSolution = _random.NextDouble() <= propability;
                        if (!acceptSolution)
                        {
                            RollBackMoveShift(lake);
                            continue;
                        }
                        result = afterResult;
                        currPen = afterPen;
                        continue;
                    }

                }
                Console.WriteLine($"Is Valid: {result.HardConstraintViolations.Count}");
                Console.WriteLine($"Penalty: {result.TotalPenalty}");
            }
        }

        private void ForwardMove(Lake lake)
        {
            var randFromPond = lake.Ponds[_random.Next(lake.Ponds.Count)];
            var randFromPuddle = randFromPond.Puddles[_random.Next(randFromPond.Puddles.Count)];
            if (randFromPuddle.Exam == null)
            {
                return;
            }
            var randExam = randFromPuddle.Exam;

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
                if (randFromPuddle.Exam == null)
                {
                    i--;
                    continue;
                }
                var randExam = randFromPuddle.Exam;


                lake.GetSuitablePondsForExam(randExam, out var chosenPond, out var suitablePonds, GetDefaultPondCondition(randExam), GetDefaultPondComparer(randExam));
                if(suitablePonds.Count == 0)
                {
                    continue;
                }
                var randToPond = suitablePonds[_random.Next(suitablePonds.Count)];
                var suitablePuddles = randToPond.Puddles.Where(randExam.GetPuddleSizeCondition()).ToList();
                if(suitablePuddles.Count == 0)
                {
                    continue;
                }
                var randToPuddle = suitablePuddles[_random.Next(suitablePuddles.Count)];
                

                randFromPond.RemoveExam(randFromPuddle, randExam);
                randToPond.AddExam(randToPuddle, randExam);


                Memory.Add(new AnnealingAction { Exam = randExam, FromPond = randFromPond, FromPuddle = randFromPuddle, ToPond = randToPond, ToPuddle = randToPuddle });
            }
        }

        private void ForwardMoveShift(Lake lake)
        {
            var randFromPond = lake.Ponds[_random.Next(lake.Ponds.Count)];
            var randToPond = lake.Ponds[_random.Next(lake.Ponds.Count)];
            if (randFromPond == randToPond)
                return;
            foreach (var (puddle1, puddle2) in randFromPond.Puddles.Zip(randToPond.Puddles))
            {
                var exam1 = puddle1.Exam; 
                var exam2 = puddle2.Exam;
                randFromPond.RemoveExam(puddle1, exam1);
                randFromPond.AddExam(puddle1, exam2);
                randToPond.RemoveExam(puddle2 , exam2);
                randToPond.AddExam(puddle2, exam1);
            }

            lake.UpdateAssignmentOnly();

            MemoryShift.Add(new() { FromPond = randFromPond, ToPond = randToPond });
        }


        private void RollbackMove(Lake lake, AnnealingAction action)
        {
            var fromPond = action.FromPond;
            var toPond = action.ToPond;
            var fromPuddle = action.FromPuddle;
            var toPuddle = action.ToPuddle;
            var exam = action.Exam;


            toPond.RemoveExam(toPuddle, exam);
            fromPond.AddExam(fromPuddle, exam);

            lake.UpdateAssignmentOnly();
        }

        private void RollbackMove(Lake lake, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                RollbackMove(lake, Memory[Memory.Count - 1]);
                Memory.RemoveAt(Memory.Count - 1);
            }
        }

        private void RollBackMoveShift(Lake lake)
        {
            var action = MemoryShift[MemoryShift.Count - 1];
            var fromPond = action.FromPond;
            var toPond = action.ToPond;

            foreach (var (puddle1, puddle2) in fromPond.Puddles.Zip(toPond.Puddles))
            {
                var exam1 = puddle1.Exam;
                var exam2 = puddle2.Exam;
                fromPond.RemoveExam(puddle1, exam1);
                fromPond.AddExam(puddle1, exam2);
                toPond.RemoveExam(puddle2, exam2);
                toPond.AddExam(puddle2, exam1);
            }

            lake.UpdateAssignmentOnly();
            MemoryShift.RemoveAt(MemoryShift.Count - 1);
        }
        private double ProbilityFunction(int oldPoint, int newPoint, double temperature, double volatility)
        {
            return Math.Exp((oldPoint - newPoint) * volatility / temperature);
        }


        public Func<Puddle, bool> GetDefaultPuddleCondition(Exam exam)
        {
            return exam
                .GetPuddleSizeCondition()
                .WrapOverChild(exam.GetPuddleRoomAvailableCondition())
                .WrapOverChild(exam.GetPuddleHardConstraintCondition())
                ;
        }

        public Comparer<Puddle> GetDefaultPuddleComparer(Exam exam)
        {
            return exam
                .GetPuddlePenaltyComparer()
                .WrapOverChild(exam.GetPuddleSoftContraintComparer(ConstraintType.SameRoom))
                .WrapOverChild(exam.GetPuddleSoftContraintComparer(ConstraintType.DifferentRoom))
                .WrapOverChild(exam.GetPuddleSizeComparer())
                ;
        }

        public Func<Pond, bool> GetDefaultPondCondition(Exam exam)
        {
            return exam
                .GetPondDurationCondition()
                .WrapOverChild(exam.GetPondAvailableCondition())
                .WrapOverChild(exam.GetPondLinkageCondition(I_exam_linkages))
                .WrapOverChild(exam.GetPondInstructorAvailabilityCondition())
                .WrapOverChild(exam.GetPondStudentAvailabilityCondition())
                .WrapOverChild(exam.GetPondHardConstraintCondition())
                ;
        }

        public Comparer<Pond> GetDefaultPondComparer(Exam exam)
        {
            return exam
                .GetPondSoftConstraintComparer(ConstraintType.DifferentPeriod)
                .WrapOverChild(exam.GetPondSoftConstraintComparer(ConstraintType.SamePeriod))
                .WrapOverChild(exam.GetPondDurationComparer(longerContainerFirst: false))
                .WrapOverChild(exam.GetPondCapacityComparer(largerContainerFirst: false))
                ;
        }
    }
}
