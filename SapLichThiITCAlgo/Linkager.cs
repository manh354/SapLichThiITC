using SapLichThiITCCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiITCAlgo
{
    public class Linkager
    {
        public required List<Exam> I_exams { get; set; }
        public required TimetablingData I_data { get; set; }
        public Dictionary<Exam, HashSet<Exam>> O_exam_linkages { get; set; }
        public Dictionary<Exam, HashSet<Exam>> O_exam_requires { get; set; }
        public Dictionary<Exam, HashSet<(Exam before, Exam after)>> O_exam_after { get; set; }
        public HashSet<Exam> O_exam_exclusive { get; set; }

        private Dictionary<Exam, HashSet<int>> P_exam_studentIds { get; set; }
        
        public Linkager Initialize()
        {
            O_exam_linkages = new();
            O_exam_requires = new();
            O_exam_exclusive = new();
            O_exam_after = new();
            P_exam_studentIds = I_exams.ToDictionary(x => x, x => x.StudentIds.ToHashSet());
            return this;
        }
        public Linkager Run()
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
            foreach (var periodHardConstraint in I_data.PeriodHardConstraints)
            {
                var exam1 = I_exams[periodHardConstraint.ExamId1];
                var exam2 = I_exams[periodHardConstraint.ExamId2];
                if (periodHardConstraint.Type == "EXCLUSION")
                {
                    O_exam_linkages[exam1].Add(exam2);
                    O_exam_linkages[exam2].Add(exam1);
                    // add to linkages
                    O_exam_linkages[exam1].Add(exam2);
                    O_exam_linkages[exam2].Add(exam1);
                }
                else if(periodHardConstraint.Type== "EXAM_COINCIDENCE")
                {
                    O_exam_requires.TryAdd(exam1, new());
                    O_exam_requires[exam1].Add(exam2);
                    O_exam_requires.TryAdd(exam2 , new());
                    O_exam_requires[exam2].Add(exam1);

                    O_exam_linkages[exam1].UnionWith(O_exam_linkages[exam2]);
                    O_exam_linkages[exam2].UnionWith(O_exam_linkages[exam1]);
                }
                else if(periodHardConstraint.Type=="AFTER")
                {
                    O_exam_after.TryAdd(exam1, new());
                    O_exam_after[exam1].Add((exam2, exam1));
                    O_exam_after.TryAdd(exam2, new());
                    O_exam_after[exam2].Add((exam2, exam1));
                    // add to linkages
                    O_exam_linkages[exam1].Add(exam2);
                    O_exam_linkages[exam2].Add(exam1);
                }
            }
            return this;
        }
    }
}
