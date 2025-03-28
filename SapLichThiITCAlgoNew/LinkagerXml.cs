using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetXml;

namespace SapLichThiITCAlgoNew
{
    public class LinkagerXml
    {
        public required List<Exam> I_exams { get; set; }
        public required ExamTimetablingData I_data { get; set; }
        public Dictionary<Exam, HashSet<Exam>> O_exam_linkages { get; set; }
        public Dictionary<Exam, HashSet<Exam>> O_exam_requires { get; set; }
        public Dictionary<Exam, HashSet<(Exam before, Exam after)>> O_exam_after { get; set; }
        public HashSet<Exam> O_exam_exclusive { get; set; }

        private Dictionary<Exam, HashSet<Student>> P_exam_studentIds { get; set; }

        public LinkagerXml Initialize()
        {
            O_exam_linkages = new();
            O_exam_requires = new();
            O_exam_exclusive = new();
            O_exam_after = new();
            P_exam_studentIds = I_exams.ToDictionary(x => x, x => x.Students.ToHashSet());
            return this;
        }
        public LinkagerXml Run()
        {
            for (int i = 0; i < I_exams.Count; i++)
            {
                var firstExam = I_exams[i];
                for (int j = i + 1; j < I_exams.Count; j++)
                {
                    var secondExam = I_exams[j];
                    if (!P_exam_studentIds.TryGetValue(firstExam, out var firstStudentIds) || !P_exam_studentIds.TryGetValue(secondExam, out var secondStudentIds))
                    {
                        continue;
                    }
                    if (firstStudentIds.Any(secondStudentIds.Contains) || secondStudentIds.Any(firstStudentIds.Contains))
                    {
                        O_exam_linkages.TryAdd(firstExam, new());
                        O_exam_linkages.TryAdd(secondExam, new());
                        O_exam_linkages[firstExam].Add(secondExam);
                        O_exam_linkages[secondExam].Add(firstExam);
                    }
                }
            }
            for (int i = 0; i < I_exams.Count; i++)
            {
                var exam = I_exams[i];
                O_exam_linkages.TryAdd(exam, new());
            }
            foreach (var periodHardConstraint in I_data.Constraints.Where(dc => dc.IsHard))
            {
                if (periodHardConstraint.Type == ConstraintType.DifferentPeriod)
                {
                    for (int i = 0; i < periodHardConstraint.Exams.Count; i++)
                    {
                        var exam1 = periodHardConstraint.Exams[i];
                        for (int j = i; j < periodHardConstraint.Exams.Count; j++)
                        {
                            var exam2 = periodHardConstraint.Exams[j];
                            O_exam_linkages[exam1].Add(exam2);
                            O_exam_linkages[exam2].Add(exam1);
                            // add to linkages
                            O_exam_linkages[exam1].Add(exam2);
                            O_exam_linkages[exam2].Add(exam1);

                        }
                    }
                }
                if (periodHardConstraint.Type == ConstraintType.SamePeriod)
                {
                    for (int i = 0; i < periodHardConstraint.Exams.Count; i++)
                    {
                        var exam1 = periodHardConstraint.Exams[i];
                        for (int j = i; j < periodHardConstraint.Exams.Count; j++)
                        {
                            var exam2 = periodHardConstraint.Exams[j];
                            O_exam_requires.TryAdd(exam1, new());
                            O_exam_requires[exam1].Add(exam2);
                            O_exam_requires.TryAdd(exam2, new());
                            O_exam_requires[exam2].Add(exam1);

                            O_exam_linkages[exam1].UnionWith(O_exam_linkages[exam2]);
                            O_exam_linkages[exam2].UnionWith(O_exam_linkages[exam1]);

                        }
                    }
                }
                if (periodHardConstraint.Type == ConstraintType.Precedence)
                {
                    for (int i = 0; i< periodHardConstraint.Exams.Count - 1;i++)
                    {
                        Exam exam1 = periodHardConstraint.Exams[i];
                        Exam exam2 = periodHardConstraint.Exams[i + 1];
                        O_exam_after.TryAdd(exam1, new());
                        O_exam_after[exam1].Add((exam2, exam1));
                        O_exam_after.TryAdd(exam2, new());
                        O_exam_after[exam2].Add((exam2, exam1));
                        // add to linkages
                        O_exam_linkages[exam1].Add(exam2);
                        O_exam_linkages[exam2].Add(exam1);
                    }
                }
            }

            return this;
        }
    }
}
