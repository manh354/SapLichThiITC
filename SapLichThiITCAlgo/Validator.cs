using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiITCAlgo
{
    public class Validator
    {
        public required TimetablingData I_data { get; set; }
        public required Lake I_lake { get; set; }
        public int P_missingCount { get; set; }
        public Dictionary<Exam, int> P_duplicateCount { get; set; }
        public Validator Initialize()
        {
            return this;
        }
        public Validator Run()
        {
            ValidateMissingClass();
            ValidateDuplicateClass();
            return this;
        }

        public void ValidateMissingClass()
        {
            HashSet<Exam> exams = I_data.Exams.ToHashSet();
            foreach (var pond in I_lake.Ponds)
            {
                foreach (var puddle in pond.Puddles)
                {
                    foreach (var exam in puddle.Exams)
                    {
                        exams.Remove(exam);
                    }
                }
            }
            Console.WriteLine("===================================");
            Console.WriteLine($"Missing exam: {exams.Count}");
        }

        public void ValidateDuplicateClass()
        {
            Dictionary<Exam, int> exams = new();
            foreach (var pond in I_lake.Ponds)
            {
                foreach (var puddle in pond.Puddles)
                {
                    foreach (var exam in puddle.Exams)
                    {
                        exams.TryAdd(exam, 0);
                        exams[exam]++;
                    }
                }
            }
            var count = exams.Where(x => x.Value > 1).Count();
            Console.WriteLine("===================================");
            Console.WriteLine($"Duplicate exam: {count}");
        }
    }
}
