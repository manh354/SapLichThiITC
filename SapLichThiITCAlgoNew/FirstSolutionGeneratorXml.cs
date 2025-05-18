using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetXml;

namespace SapLichThiITCAlgoNew
{
    public class FirstSolutionGeneratorXml
    {
        public required ExamTimetablingData I_data { get; set; }
        public required Dictionary<int, HashSet<Exam>> I_color_exams { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_linkages { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_requires { get; set; }
        public required Dictionary<Exam, HashSet<(Exam before, Exam after)>> I_exam_after { get; set; }
        public required HashSet<Exam> I_exam_exclusive { get; set; }
        public required Lake I_lake { get; set; }
        private HashSet<Exam> P_scheduledExams { get; set; } = new();
        private const int MAX_ITER = 10;

        private HashSet<Exam> P_hardConstraintExams { get; set; } = new();
        public FirstSolutionGeneratorXml Initialize()
        {
            return this;
        }

        public FirstSolutionGeneratorXml Run()
        {
            FitRoomsHardConstraintExams();

            var sortedColors = I_color_exams
                .OrderByDescending(item => item.Value.Max(exam => I_exam_linkages[exam].Count))
                .ThenByDescending(item => item.Value.Max(exam => exam.Students.Count))
                .ThenByDescending(item => item.Value.Sum(exam => exam.Students.Count))
                .ToList();


            foreach (var (color, classes) in sortedColors)
            {
                classes.ExceptWith(P_scheduledExams);
            }

            int iter = 0;

            while (++iter < MAX_ITER)
            {
                Console.WriteLine($"Xếp các lớp tại iteration {iter}");
                List<KeyValuePair<int, HashSet<Exam>>> residueForEachColor = new();
                foreach (var (color, exams) in sortedColors)
                {

                    if (exams.Count == 0)
                        continue;
                    var sortedExams = exams
                        .OrderByDescending(x => I_exam_linkages[x].Count)
                        .ThenByDescending(x => x.Students.Count).ToList();

                    // HashSet<ExamClass> allAddedExamClasses = new();

                    var totalStudentOfThisColor = exams.Sum(x => x.Students.Count);
                    var smallestClassCount = exams.Min(x => x.Students.Count);
                    var maxExamTime = exams.Max(x => x.Length);
                    var pondCondititonForExams = (Pond pond) => { return !sortedExams.Any(e => pond.Exams.Any(pe => I_exam_linkages[e].Contains(pe))); };
                    var pondComparerForExams = Comparer<Pond>.Create((Pond firstPond, Pond secondPond) => { return secondPond.GetRemainingCapacity().CompareTo(firstPond.GetRemainingCapacity()); });
                    var pondFound = I_lake.GetSuitablePondsForExams(
                        sortedExams,
                        out var bestPond,
                        out var suitablePonds,
                        pondCondititonForExams,
                        pondComparerForExams
                        );
                    if (!pondFound)
                    {
                        Console.WriteLine($"Không tồn tại Pond hợp lý cho màu {color}");
                        Console.WriteLine($"Chương trình sẽ thử phân rã màu để xếp theo từng môn");
                        FitRoomsByExam(
                            sortedExams,
                            out var residueExams
                            /*DefaultPondCondition,
                            CreatePondComparer(false),
                            DefaultPuddleCondition,
                            CreatePuddleComparer(false)*/
                            );
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"Pond tốt nhất tìm được cho màu {color}: Ngày {bestPond.Period.Time}, thời gian {bestPond.Period.Time}, độ dài {bestPond.Period.Length}");
                        // Logger.LogMessage("____________________________________________________");
                    }
                    var atLeastOneClassAdded = false;
                    var pondIndex = 0;
                    var examsOfOneShift = sortedExams.ToHashSet();
                    HashSet<Exam> residueClasseOfOneShift = new();
                    while (!atLeastOneClassAdded)
                    {
                        residueClasseOfOneShift = new();
                        foreach (var exam in sortedExams)
                        {
                            var puddleFound = I_lake.GetSuitablePuddleForExam(
                                bestPond,
                                exam,
                                out var bestPuddle,
                                out var bestContainer,
                                GetDefaultPuddleCondition(exam, bestPond),
                                GetDefaultPuddleComparer(exam)
                                );
                            if (!puddleFound)
                            {
                                Console.WriteLine($"Không tìm thấy Puddle cho lớp {exam}, chuyển sang kíp thi tiếp theo.");
                                residueClasseOfOneShift.Add(exam);
                                continue;
                            }
                            I_lake.AddElementToPuddle(bestPond, bestPuddle!, exam);
                            atLeastOneClassAdded = true;
                            Console.WriteLine($"Tìm thấy Puddle cho lớp {exam} ");
                            if (!P_scheduledExams.Add(exam))
                            {
                                throw new Exception("Scheduled");
                            }

                        }
                        if (!atLeastOneClassAdded)
                        {
                            if (++pondIndex < suitablePonds.Count)
                            {
                                bestPond = suitablePonds[pondIndex];
                                Console.WriteLine($"Chuyển sang Pond tốt thứ {pondIndex + 1}: Ngày {bestPond.Period.Length}, thời gian {bestPond.Period.Time}, độ dài {bestPond.Period.Length}");
                                examsOfOneShift = residueClasseOfOneShift;
                                residueClasseOfOneShift = new();
                                continue;
                            }
                            break;
                        }
                    }
                    var allRemainingExam = residueClasseOfOneShift;
                    if (residueClasseOfOneShift.Count > 0)
                        residueForEachColor.Add(new KeyValuePair<int, HashSet<Exam>>(color, allRemainingExam));
                }
                if (residueForEachColor.Count > 0)
                    sortedColors = residueForEachColor;
                else
                {
                    sortedColors = residueForEachColor;
                    break;
                }
            }

            EvaluatorXml evaluatorXml = new EvaluatorXml(I_data);
            var result = evaluatorXml.Evaluate();

            // If there is any residue color/ courses , break it up and fit by individual course.
            if (sortedColors.Count > 0)
            {
                sortedColors = sortedColors
                .OrderByDescending(item => item.Value.Max(exam => I_exam_linkages[exam].Count))
                .ThenByDescending(item => item.Value.Max(exam => exam.Students.Count))
                .ThenByDescending(item => item.Value.Sum(exam => exam.Students.Count))
                .ToList();

                List<KeyValuePair<int, HashSet<Exam>>> remainingColors = new();
                foreach (var (color, examClasses) in sortedColors)
                {
                    Console.WriteLine($"Chương trình sẽ phân rã màu {color} để xếp theo lớp.");
                    var maxExamTime = examClasses.Max(x => x.Length);
                    FitRoomsByExam(examClasses.OrderByDescending(x => x.Students.Count).ToList(), out var residueExams
                        /*DefaultPondCondition,
                        CreatePondComparer(false),
                        DefaultPuddleCondition,
                        CreatePuddleComparer(false)*/
                        );
                    if (residueExams.Count > 0)
                        remainingColors.Add(new(color, residueExams));
                }
                sortedColors = remainingColors;
            }

            result = evaluatorXml.Evaluate();

            if (sortedColors.Count > 0)
            {
                sortedColors = sortedColors
                .OrderByDescending(item => item.Value.Max(exam => I_exam_linkages[exam].Count))
                .ThenByDescending(item => item.Value.Max(exam => exam.Students.Count))
                .ThenByDescending(item => item.Value.Sum(exam => exam.Students.Count))
                .ToList();

                List<KeyValuePair<int, HashSet<Exam>>> remainingColors = new();

                foreach (var (color, examClasses) in sortedColors)
                {
                    FitRoomsByExamAfter(
                        examClasses.OrderByDescending(x => x.Students.Count).ToList(),
                        out var residueExams
                        /*DefaultPondCondition,
                        CreatePondComparer(false),
                        DefaultPuddleCondition,
                        CreatePuddleComparer(false)*/
                        );
                    if (residueExams.Count > 0)
                    {
                        remainingColors.Add(new(color, residueExams));
                    }

                    EvaluatorXml evaluatorXml2 = new EvaluatorXml(I_data);
                    var result2 = evaluatorXml2.Evaluate();
                }
                sortedColors = remainingColors;
            }

            result = evaluatorXml.Evaluate();

            if (sortedColors.Count > 0)
            {
                sortedColors = sortedColors
                .OrderByDescending(item => item.Value.Max(exam => I_exam_linkages[exam].Count))
                .ThenByDescending(item => item.Value.Max(exam => exam.Students.Count))
                .ThenByDescending(item => item.Value.Sum(exam => exam.Students.Count))
                .ToList();

                List<KeyValuePair<int, HashSet<Exam>>> remainingColors = new();
                foreach (var (color, examClasses) in sortedColors)
                {
                    FitRoomByExamThirdPhase(
                        examClasses.OrderByDescending(x => x.Students.Count).ToList(),
                        out var residueExams
                        /*DefaultPondCondition,
                        CreatePondComparer(false),
                        DefaultPuddleCondition,
                        CreatePuddleComparer(false)*/
                        );
                    if (residueExams.Count > 0)
                    {
                        remainingColors.Add(new(color, residueExams));
                    }
                }
            }

            this.I_lake.UpdateAssignmentOnly();
            result = evaluatorXml.Evaluate();
            return this;
        }

        private void FitRoomsByExamAfter(
            List<Exam> exams,
            out HashSet<Exam> residueExams,
            Func<Pond, bool>? pondCondition = null,
            Comparer<Pond>? pondComparer = null,
            Func<Puddle, bool>? puddleCondition = null,
            Comparer<Puddle>? puddleComparer = null
            )
        {
            residueExams = new();
            foreach (var exam in exams)
            {
                var pondFound = I_lake.GetSuitablePondsForExam(
                       exam,
                       out var bestPond,
                       out var suitablePonds,
                       GetDefaultPondCondition(exam),
                       GetDefaultPondComparer(exam)
                       );
                if (!pondFound)
                {
                    Console.WriteLine($"Không xếp mở rộng được lớp thi {exam}, Lí do: Không có Pond Hợp lệ");
                    residueExams.Add(exam);
                    continue;
                }

                var pondIndex = 0;
                while (pondIndex < suitablePonds.Count)
                {

                    var clonePond = bestPond.DeepClone();
                    var examClassExist = clonePond.ClearAndReturn();
                    examClassExist.Add(exam);

                    var currentResidueExams = new HashSet<Exam>();

                    FitRoomsByExamByPond(
                        clonePond,
                        examClassExist,
                        out currentResidueExams,
                        GetDefaultPuddleCondition(exam),
                        GetDefaultPuddleComparer(exam));

                    if (currentResidueExams.Count > 0 && pondIndex < suitablePonds.Count - 1)
                    {
                        bestPond.UpdateAssignmentOnlyExam();
                        pondIndex += 1;
                        bestPond = suitablePonds[pondIndex];
                        continue;
                    }
                    if (currentResidueExams.Count > 0 && pondIndex == suitablePonds.Count - 1)
                    {
                        Console.WriteLine($"Không thể xếp mở rộng cho lớp {exam}, Lí do: Xếp thử thất bại.");
                        residueExams.Add(exam);
                        bestPond.UpdateAssignmentOnlyExam();
                        break;
                    }

                    Console.WriteLine($"Xếp mở rộng thành công cho lớp {exam}: Ngày {bestPond.Period.Time}, thời gian {bestPond.Period.Time}, độ dài {bestPond.Period.Length}");
                    bestPond.Clear();
                    bestPond.CopyAndUpdateExamAssignmentFrom(clonePond);
                    var count2 = bestPond.Exams.Where(e => e.Assignment.Period != null).Count();
                    if (P_scheduledExams.Add(exam))
                    {
                    }
                    break;
                
                }
            }
        }


        private void FitRoomByExamThirdPhase(
            List<Exam> exams,
            out HashSet<Exam> residueExams,
            Func<Pond, bool>? pondCondition = null,
            Comparer<Pond>? pondComparer = null,
            Func<Puddle, bool>? puddleCondition = null,
            Comparer<Puddle>? puddleComparer = null
            )
        {
            residueExams = new();
            foreach (var exam in exams)
            {

                var conflictExam = I_lake.Linkages[exam];

                var pondFound = I_lake.GetSuitablePondsForExam(
                       exam,
                       out var bestPond,
                       out var suitablePonds,
                       GetDefaultPondConditionWithoutPondLinkage(exam),
                       GetDefaultPondComparerWithLinkages(exam, I_exam_linkages)
                       );

                if (!pondFound)
                {
                    Console.WriteLine($"Không xếp mở rộng được lớp thi {exam}, Lí do: Không có Pond Hợp lệ");
                    residueExams.Add(exam);
                    continue;
                }

                var pondIndex = 0;
                while (pondIndex < suitablePonds.Count)
                {

                    var clonePond = bestPond.DeepClone();
                    var examClassExist = clonePond.ClearAndReturn();
                    examClassExist.Add(exam);

                    var currentResidueExams = new HashSet<Exam>();

                    FitRoomsByExamByPond(
                        clonePond,
                        examClassExist,
                        out currentResidueExams,
                        GetDefaultPuddleCondition(exam),
                        GetDefaultPuddleComparer(exam));

                    if (currentResidueExams.Count > 0 && pondIndex < suitablePonds.Count - 1)
                    {
                        bestPond.UpdateAssignmentOnlyExam();
                        pondIndex += 1;
                        bestPond = suitablePonds[pondIndex];

                        continue;
                    }
                    if (currentResidueExams.Count > 0 && pondIndex == suitablePonds.Count - 1)
                    {
                        Console.WriteLine($"Không thể xếp mở rộng cho lớp {exam}, Lí do: Xếp thử thất bại.");
                        exam.Assignment.Clear();
                        residueExams.Add(exam);
                        break;
                    }

                    Console.WriteLine($"Xếp mở rộng thành công cho lớp {exam}: Ngày {bestPond.Period.Time}, thời gian {bestPond.Period.Time}, độ dài {bestPond.Period.Length}");
                    bestPond.Clear();
                    bestPond.CopyAndUpdateExamAssignmentFrom(clonePond);
                    if (P_scheduledExams.Add(exam))
                    {
                    }
                    break;
                }
            }
        }

        private void FitRoomByExamFourthPhase(
            List<Exam> exams,
            out HashSet<Exam> residueExams,
            Func<Pond, bool>? pondCondition = null,
            Comparer<Pond>? pondComparer = null,
            Func<Puddle, bool>? puddleCondition = null,
            Comparer<Puddle>? puddleComparer = null
            )
        {
            residueExams = new();
            foreach (var exam in exams)
            {

                var conflictExam = I_lake.Linkages[exam];
                var orderedPonds = I_lake.Ponds.Select(p => (
                    pond: p,
                    exams: p.Exams.Where(conflictExam.Contains).ToList()
                )).OrderBy(pe => pe.exams.Count).ToList();

                var pondFound = I_lake.GetSuitablePondsForExam(
                       exam,
                       out var bestPond,
                       out var suitablePonds,
                       GetDefaultPondConditionWithoutPondLinkage(exam),
                       GetDefaultPondComparerWithLinkages(exam, I_exam_linkages)
                       );
                if (!pondFound)
                {
                    Console.WriteLine($"Không xếp mở rộng được lớp thi {exam}, Lí do: Không có Pond Hợp lệ");
                    residueExams.Add(exam);
                    continue;
                }

                var pondIndex = 0;
                while (pondIndex < suitablePonds.Count)
                {

                    var clonePond = bestPond.DeepClone();
                    var examClassExist = clonePond.ClearAndReturn();
                    examClassExist.Add(exam);

                    var currentResidueExams = new HashSet<Exam>();

                    FitRoomsByExamByPond(
                        clonePond,
                        examClassExist,
                        out currentResidueExams,
                        GetDefaultPuddleCondition(exam),
                        GetDefaultPuddleComparer(exam));

                    if (currentResidueExams.Count > 0 && pondIndex < suitablePonds.Count - 1)
                    {
                        pondIndex += 1;
                        bestPond = suitablePonds[pondIndex];

                        continue;
                    }
                    if (currentResidueExams.Count > 0 && pondIndex == suitablePonds.Count - 1)
                    {
                        Console.WriteLine($"Không thể xếp mở rộng cho lớp {exam}, Lí do: Xếp thử thất bại.");
                        residueExams.Add(exam);
                        break;
                    }

                    Console.WriteLine($"Xếp mở rộng thành công cho lớp {exam}: Ngày {bestPond.Period.Time}, thời gian {bestPond.Period.Time}, độ dài {bestPond.Period.Length}");
                    bestPond.Clear();
                    bestPond.CopyAndUpdateExamAssignmentFrom(clonePond);
                    if (P_scheduledExams.Add(exam))
                    {
                    }
                    break;
                }
            }
        }

        private void FitRoomsByExam(
            List<Exam> exams,
            out HashSet<Exam> residueExams,
            Func<Pond, bool>? pondCondition = null,
            Comparer<Pond>? pondComparer = null,
            Func<Puddle, bool>? puddleCondition = null,
            Comparer<Puddle>? puddleComparer = null
            )
        {
            int iter = 0;
            residueExams = new HashSet<Exam>();
            while (++iter < MAX_ITER)
            {
                residueExams = new HashSet<Exam>();
                foreach (var exam in exams)
                {
                    var pondFound = I_lake.GetSuitablePondsForExam(
                        exam,
                        out var bestPond,
                        out var suitablePonds,
                        pondCondition,
                        pondComparer
                        );
                    if (!pondFound)
                    {
                        Console.WriteLine($"Không xếp được lớp thi {exam}");
                        residueExams.Add(exam);
                        continue;
                    }

                    var pondIndex = 0;
                    while (pondIndex < suitablePonds.Count)
                    {
                        var puddleFound = I_lake.GetSuitablePuddleForExam(
                            bestPond, exam,
                            out var bestPuddle,
                            out var suitablePuddles,
                            puddleCondition,
                            puddleComparer);
                        if (!puddleFound && pondIndex < suitablePonds.Count - 1)
                        {
                            pondIndex += 1;
                            bestPond = suitablePonds[pondIndex];
                            continue;
                        }
                        if (!puddleFound && pondIndex == suitablePonds.Count - 1)
                        {
                            Console.WriteLine($"Không tìm thấy Puddle cho lớp {exam}.");
                            residueExams.Add(exam);
                            break;
                        }

                        I_lake.AddElementToPuddle(bestPond, bestPuddle, exam);
                        Console.WriteLine($"Pond tốt nhất tìm được cho lớp {exam}: Ngày {bestPond.Period.Time}, thời gian {bestPond.Period.Length}, độ dài {bestPond.Period.Length}");
                        if (P_scheduledExams.Add(exam))
                        {

                        }
                        break;
                    }
                }
                exams = residueExams.ToList();
            }
        }

        private void FitRoomsByExam(
            List<Exam> exams,
            out HashSet<Exam> residueExams
            )
        {
            int iter = 0;
            residueExams = new HashSet<Exam>();
            while (++iter < MAX_ITER)
            {
                residueExams = new HashSet<Exam>();
                foreach (var exam in exams)
                {
                    var pondFound = I_lake.GetSuitablePondsForExam(
                        exam,
                        out var bestPond,
                        out var suitablePonds,
                        GetDefaultPondCondition(exam),
                        GetDefaultPondComparer(exam)
                        );
                    if (!pondFound)
                    {
                        Console.WriteLine($"Không xếp được lớp thi {exam}");
                        residueExams.Add(exam);
                        continue;
                    }

                    var pondIndex = 0;
                    while (pondIndex < suitablePonds.Count)
                    {
                        var puddleFound = I_lake.GetSuitablePuddleForExam(
                            bestPond, exam,
                            out var bestPuddle,
                            out var suitablePuddles,
                            GetDefaultPuddleCondition(exam),
                            GetDefaultPuddleComparer(exam)
                            );
                        if (!puddleFound && pondIndex < suitablePonds.Count - 1)
                        {
                            pondIndex += 1;
                            bestPond = suitablePonds[pondIndex];
                            continue;
                        }
                        if (!puddleFound && pondIndex == suitablePonds.Count - 1)
                        {
                            Console.WriteLine($"Không tìm thấy Puddle cho lớp {exam}.");
                            residueExams.Add(exam);
                            break;
                        }

                        I_lake.AddElementToPuddle(bestPond, bestPuddle, exam);
                        Console.WriteLine($"Pond tốt nhất tìm được cho lớp {exam}: Ngày {bestPond.Period.Time}, thời gian {bestPond.Period.Length}, độ dài {bestPond.Period.Length}");
                        if (P_scheduledExams.Add(exam))
                        {

                        }
                        break;
                    }
                }
                exams = residueExams.ToList();
            }
        }

        private void FitRoomsByExamByPond(
            Pond specificPond,
            List<Exam> exams,
            out HashSet<Exam> residueExams,
            Func<Puddle, bool>? puddleCondition = null,
            Comparer<Puddle>? puddleComparer = null
            )
        {
            residueExams = new HashSet<Exam>();
            exams = exams.OrderByDescending(x => x.Students.Count).ToList();
            foreach (var exam in exams)
            {
                var puddleFound = I_lake.GetSuitablePuddleForExam(
                    specificPond, exam,
                    out var bestPuddle,
                    out var suitablePuddles,
                    GetDefaultPuddleCondition(exam),
                    GetDefaultPuddleComparer(exam));

                if (!puddleFound)
                {
                    Console.WriteLine($"Pond_specific: Không tìm thấy Puddle cho lớp {exam}.");
                    residueExams.Add(exam);
                    break;
                }

                I_lake.AddElementToPuddle(specificPond, bestPuddle, exam);
                if (P_scheduledExams.Add(exam))
                {
                }
            }
        }

        private void RemoveAssignmentOfExams(List<Exam> exams)
        {
            foreach (var exam in exams)
            {
                exam.Assignment = new();
            }
        }

        private void FitRoomsHardConstraintExams()
        {
            /*HashSet<Exam> scheduledExams = new();
            foreach (var (color, examClasses) in I_color_exams)
            {
                if (examClassRequire.Count != 0)
                {
                    FitRoomsByExam(examClassRequire, out var residueRequire,
                    (Pond pond) =>
                    {
                        return pond.Exams.Count == 0 || pond.Exams.;
                    },
                    CreatePondComparer(true),
                    (Puddle puddle)
                    );
                }
                if (examClassAfter.Count != 0)
                {
                    FitRoomsByExam(examClassAfter, )
                }

            }*/

            Console.WriteLine(I_exam_after.Count);

            List<Exam> exams = DetermineExamSequence(I_exam_after);
            Console.WriteLine(exams.Count);
            FitRoomsByExam(
                exams,
                out var residueExams
                );



            foreach (var (exam, requiredExams) in I_exam_requires)
            {

                var requiredExamsNotCollide = requiredExams.Where(x => !P_scheduledExams.Contains(x)).ToList();
                FitRoomsByExam(
                    requiredExamsNotCollide,
                    out var residueRequireExams,
                    GetDefaultPondCondition(exam),
                    GetDefaultPondComparer(exam),
                    GetDefaultPuddleCondition(exam),
                    GetDefaultPuddleComparer(exam)
                );
            }
        }

        public List<Exam> DetermineExamSequence(Dictionary<Exam, HashSet<(Exam before, Exam after)>> exam_orderPair)
        {
            // Collect all unique edges and all exams involved
            var edges = new HashSet<(Exam before, Exam after)>();
            var allExams = new HashSet<Exam>(exam_orderPair?.Keys ?? Enumerable.Empty<Exam>());

            foreach (var pairList in exam_orderPair?.Values ?? Enumerable.Empty<HashSet<(Exam before, Exam after)>>())
            {
                foreach (var pair in pairList)
                {
                    edges.Add(pair);
                    allExams.Add(pair.before);
                    allExams.Add(pair.after);
                }
            }

            // If there are no exams, return an empty list
            if (allExams.Count == 0)
            {
                return new List<Exam>();
            }

            // Build adjacency list and in-degree dictionary
            var adjacency = new Dictionary<Exam, List<Exam>>();
            var inDegree = new Dictionary<Exam, int>();

            foreach (var exam in allExams)
            {
                adjacency[exam] = new List<Exam>();
                inDegree[exam] = 0;
            }

            foreach (var (before, after) in edges)
            {
                adjacency[before].Add(after);
                inDegree[after]++;
            }

            // Kahn's algorithm for topological sort
            var queue = new Queue<Exam>();
            foreach (var exam in inDegree.Keys.Where(e => inDegree[e] == 0))
            {
                queue.Enqueue(exam);
            }

            var result = new List<Exam>();
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                foreach (var neighbor in adjacency[current])
                {
                    if (--inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // Check for cycles
            if (result.Count != allExams.Count)
            {
                throw new InvalidOperationException("A cycle exists in the exam dependencies, making a valid schedule impossible.");
            }

            return result;
        }
        public static List<Exam> GetExamOrder(List<(Exam Before, Exam After)> dependencies)
        {
            var graph = new Dictionary<Exam, List<Exam>>();
            var inDegree = new Dictionary<Exam, int>();

            // Build graph and in-degree map
            foreach (var (before, after) in dependencies)
            {
                if (!graph.ContainsKey(before)) graph[before] = new List<Exam>();
                if (!graph.ContainsKey(after)) graph[after] = new List<Exam>();

                graph[before].Add(after);

                if (!inDegree.ContainsKey(before)) inDegree[before] = 0;
                if (!inDegree.ContainsKey(after)) inDegree[after] = 0;

                inDegree[after]++;
            }

            // Find nodes with zero in-degree
            var queue = new Queue<Exam>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var sortedOrder = new List<Exam>();

            while (queue.Count > 0)
            {
                var exam = queue.Dequeue();
                sortedOrder.Add(exam);

                foreach (var next in graph[exam])
                {
                    inDegree[next]--;
                    if (inDegree[next] == 0)
                        queue.Enqueue(next);
                }
            }

            return sortedOrder.Count == inDegree.Count ? sortedOrder : new List<Exam>(); // Return empty if cycle exists
        }

        public bool GetSuitablePondsForExam(Lake lake, Exam exam, out Pond? chosenPond, out List<Pond> suitablePonds, Func<Pond, bool> condition, Comparer<Pond> comparer)
        {
            return lake.GetSuitablePondsForExam(exam, out chosenPond, out suitablePonds, condition, comparer);
        }
        public bool GetSuitablePondsForExams(Lake lake, List<Exam> exams, out Pond? chosenPond, out List<Pond> suitablePonds, Func<Pond, bool> condition, Comparer<Pond> comparer)
        {
            return lake.GetSuitablePondsForExams(exams, out chosenPond, out suitablePonds, condition, comparer);
        }

        public Func<Puddle, bool> GetDefaultPuddleConditionForExams(List<Exam> exams)
        {
            throw new Exception();
            //return exams.Select(e=> e.GetPondDurationComparer())
        }


        public Func<Puddle, bool> GetDefaultPuddleCondition(Exam exam)
        {
            return exam
                .GetPuddleSizeCondition()
                .WrapOverChild(exam.GetPuddleRoomAvailableCondition())
                .WrapOverChild(exam.GetPuddleHardConstraintCondition())
                ;
        }

        public Func<Puddle, bool> GetDefaultPuddleCondition(Exam exam, Pond pond)
        {
            var condition = exam.GetPondAvailableCondition().And(exam.GetPondDurationCondition())(pond);
            return p =>
            {
                if (!condition) return false;
                else return exam
                .GetPuddleSizeCondition()
                .And(condition)
                .WrapOverChild(exam.GetPuddleRoomAvailableCondition())
                .WrapOverChild(exam.GetPuddleHardConstraintCondition())
                (p);
            };
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

        public Func<Pond, bool> GetDefaultPondConditionWithoutPondLinkage(Exam exam)
        {
            return exam
                .GetPondDurationCondition()
                .WrapOverChild(exam.GetPondAvailableCondition())
                .WrapOverChild(exam.GetPondInstructorAvailabilityCondition())
                .WrapOverChild(exam.GetPondStudentAvailabilityCondition())
                .WrapOverChild(exam.GetPondHardConstraintCondition());

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

        public Comparer<Pond> GetDefaultPondComparerWithLinkages(Exam exam, Dictionary<Exam, HashSet<Exam>> linkages)
        {
            return exam
                .GetPondLinkagesComparer(linkages)
                .WrapOverChild(exam.GetPondSoftConstraintComparer(ConstraintType.DifferentPeriod))
                .WrapOverChild(exam.GetPondSoftConstraintComparer(ConstraintType.SamePeriod))
                .WrapOverChild(exam.GetPondDurationComparer(longerContainerFirst: false))
                .WrapOverChild(exam.GetPondCapacityComparer(largerContainerFirst: false))
                ;
        }
    }
}

