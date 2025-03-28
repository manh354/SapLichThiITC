using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetXml;

namespace SapLichThiITCAlgoNew
{
    public class ValidatorXml
    {
        public required ExamTimetablingData I_data { get; set; }
        public required Lake I_lake { get; set; }
        public int P_missingCount { get; set; }
        public Dictionary<Exam, int> P_duplicateCount { get; set; }
        public ValidatorXml Initialize()
        {
            return this;
        }
        public ValidatorXml Run()
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
                    exams.Remove(puddle.Exam);
                }
            }
            var orphanExams = exams.Where(e => e.Assignment == null || e.Assignment.Period == null).ToList();

            Console.WriteLine("===================================");
            Console.WriteLine($"Missing exam: {exams.Count} orphan: {orphanExams.Count}");
        }

        public void ValidateDuplicateClass()
        {
            Dictionary<Exam, int> exams = new();
            foreach (var pond in I_lake.Ponds)
            {
                foreach (var puddle in pond.Puddles)
                {
                    if (puddle.Exam != null)
                    {
                        exams.TryAdd(puddle.Exam, 0);
                        exams[puddle.Exam]++;

                    }

                }
            }
            var count = exams.Where(x => x.Value > 1).Count();
            Console.WriteLine("===================================");
            Console.WriteLine($"Duplicate exam: {count}");
        }
    }
}
