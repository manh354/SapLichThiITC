using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiITCAlgo
{

    public class FirstSolutionGenerator
    {

        enum RoomHardConstraint
        {
            ROOM_EXCLUSIVE
        }

        enum PeriodHardConstraint
        {
            EXAM_COINCIDENCE, AFTER, EXCLUSION,
        }

        public required TimetablingData I_data { get; set; }
        public required Dictionary<int, HashSet<Exam>> I_color_exams { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_linkages { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_requires { get; set; }
        public required Dictionary<Exam, HashSet<(Exam before, Exam after)>> I_exam_after { get; set; }
        public required HashSet<Exam> I_exam_exclusive { get; set; }
        public required Lake I_lake { get; set; }
        private HashSet<Exam> P_scheduledExams { get; set; } = new();
        private const int MAX_ITER = 50;

        private HashSet<Exam> P_hardConstraintExams { get; set; } = new();
        public FirstSolutionGenerator Initialize()
        {
            return this;
        }

        public FirstSolutionGenerator Run()
        {
            FitRoomsHardConstraintExams();

            var sortedColors = I_color_exams
                .OrderByDescending(item => item.Value.Max(exam => I_exam_linkages[exam].Count))
                .ThenByDescending(item => item.Value.Max(exam => exam.StudentIds.Count))
                .ThenByDescending(item => item.Value.Sum(exam => exam.StudentIds.Count))
                .ToList();

            var roomHardConstraints = I_data.RoomHardConstraints;
            var periodHardContraints = I_data.PeriodHardConstraints;


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
                        .ThenByDescending(x => x.StudentIds.Count).ToList();

                    // HashSet<ExamClass> allAddedExamClasses = new();

                    var totalStudentOfThisColor = exams.Sum(x => x.StudentIds.Count);
                    var smallestClassCount = exams.Min(x => x.StudentIds.Count);
                    var maxExamTime = exams.Max(x => x.Duration);
                    var pondFound = I_lake.GetSuitablePondsForExams(
                        sortedExams,
                        out var bestPond,
                        out var suitablePonds,
                        DefaultPondCondition,
                        CreatePondComparer(false)
                        );
                    if (!pondFound)
                    {
                        Console.WriteLine($"Không tồn tại Pond hợp lý cho màu {color}");
                        Console.WriteLine($"Chương trình sẽ thử phân rã màu để xếp theo từng môn");
                        FitRoomsByExam(
                            sortedExams,
                            out var residueExams,
                            DefaultPondCondition,
                            CreatePondComparer(false),
                            DefaultPuddleCondition,
                            CreatePuddleComparer(false)
                            );
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"Pond tốt nhất tìm được cho màu {color}: Ngày {bestPond.Period.Date}, thời gian {bestPond.Period.StartTime}, độ dài {bestPond.Period.DurationMinutes}");
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
                            if (exam.Id == 195)
                                Console.WriteLine("");
                            var puddleFound = I_lake.GetSuitablePuddleForExam(
                                bestPond,
                                exam,
                                out var bestPuddle,
                                out var bestContainer,
                                DefaultPuddleCondition.PutInParent(x => bestPond.Period.DurationMinutes >= exam.Duration),
                                CreatePuddleComparer(false)
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
                                Console.WriteLine($"Chuyển sang Pond tốt thứ {pondIndex + 1}: Ngày {bestPond.Period.Date}, thời gian {bestPond.Period.StartTime}, độ dài {bestPond.Period.DurationMinutes}");
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
            // If there is any residue color/ courses , break it up and fit by individual course.
            if (sortedColors.Count > 0)
            {
                List<KeyValuePair<int, HashSet<Exam>>> remainingColors = new();
                foreach (var (color, examClasses) in sortedColors)
                {
                    Console.WriteLine($"Chương trình sẽ phân rã màu để xếp theo lớp.");
                    var maxExamTime = examClasses.Max(x => x.Duration);
                    FitRoomsByExam(examClasses.OrderByDescending(x => x.StudentIds.Count).ToList(), out var residueExams,
                        DefaultPondCondition,
                        CreatePondComparer(false),
                        DefaultPuddleCondition,
                        CreatePuddleComparer(false)
                        );
                    if (residueExams.Count > 0)
                        remainingColors.Add(new(color, residueExams));
                }
                sortedColors = remainingColors;
            }

            if (sortedColors.Count > 0)
            {
                List<KeyValuePair<int, HashSet<Exam>>> remainingColors = new();
                foreach (var (color, examClasses) in sortedColors)
                {
                    FitRoomsByExamAfter(
                        examClasses.OrderByDescending(x => x.StudentIds.Count).ToList(),
                        out var residueExams,
                        DefaultPondCondition,
                        CreatePondComparer(false),
                        DefaultPuddleCondition,
                        CreatePuddleComparer(false)
                        );
                    if (residueExams.Count > 0)
                    {
                        remainingColors.Add(new(color, residueExams));
                    }
                }
                sortedColors = remainingColors;
            }
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
                        pondCondition.PutInParent(x => x.Period.DurationMinutes >= exam.Duration),
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

                    var clonePond = bestPond.DeepClone();
                    var examClassExist = clonePond.ClearAndReturn();
                    examClassExist.Add(exam);

                    var currentResidueExams = new HashSet<Exam>();

                    FitRoomsByExamByPond(clonePond, examClassExist, out currentResidueExams, puddleCondition, puddleComparer);

                    if (currentResidueExams.Count > 0 && pondIndex < suitablePonds.Count - 1)
                    {
                        pondIndex += 1;
                        bestPond = suitablePonds[pondIndex];
                        continue;
                    }
                    if (currentResidueExams.Count > 0 && pondIndex == suitablePonds.Count - 1)
                    {
                        Console.WriteLine($"Không thể xếp mở rộng cho lớp {exam}.");
                        residueExams.Add(exam);
                        break;
                    }

                    Console.WriteLine($"Pond tốt nhất tìm được cho lớp {exam}: Ngày {bestPond.Period.Date}, thời gian {bestPond.Period.StartTime}, độ dài {bestPond.Period.DurationMinutes}");
                    bestPond.CopyFrom(clonePond);
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
                        pondCondition.PutInParent(x => x.Period.DurationMinutes >= exam.Duration),
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
                            puddleCondition.PutInParent(x => bestPond.Period.DurationMinutes >= exam.Duration),
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
                        Console.WriteLine($"Pond tốt nhất tìm được cho lớp {exam}: Ngày {bestPond.Period.Date}, thời gian {bestPond.Period.StartTime}, độ dài {bestPond.Period.DurationMinutes}");
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
            exams = exams.OrderByDescending(x => x.StudentIds.Count).ToList();
            foreach (var exam in exams)
            {
                var puddleFound = I_lake.GetSuitablePuddleForExam(
                    specificPond, exam,
                    out var bestPuddle,
                    out var suitablePuddles,
                    puddleCondition,
                    puddleComparer);
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
            exams = residueExams.ToList();
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
                out var residueExams,
                DefaultPondCondition,
                CreatePondComparer(false)
                    .PutInParent(Comparer<Pond>.Create((x, y) => y.Period.StartTime.CompareTo(x.Period.StartTime)))
                    .PutInParent(Comparer<Pond>.Create((x, y) => y.Period.Date.CompareTo(x.Period.Date))),
                DefaultPuddleCondition,
                CreatePuddleComparer(false)
                );



            foreach (var (exam, requiredExams) in I_exam_requires)
            {

                var requiredExamsNotCollide = requiredExams.Where(x => !P_scheduledExams.Contains(x)).ToList();
                FitRoomsByExam(
                    requiredExamsNotCollide,
                    out var residueRequireExams,
                    DefaultPondCondition.PutInParent(x => x.Period.DurationMinutes >= exam.Duration),
                    CreatePondComparer(false).PutInParent(Comparer<Pond>.Create((x, y) =>
                    {
                        if (x.Exams.Any(requiredExams.Contains)) return 1;
                        if (y.Exams.Any(requiredExams.Contains)) return -1;
                        return 0;
                    }
                        )),
                    DefaultPuddleCondition,
                    CreatePuddleComparer(false)

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

        public Comparer<Pond> CreatePondComparer(bool largestPondFirst)
        {
            if (largestPondFirst)
                return Comparer<Pond>.Create((firstPond, secondPond) =>
                {
                    if (firstPond.Period.Penalty == secondPond.Period.Penalty)
                        return firstPond.GetRemainingCapacity().CompareTo(secondPond.GetRemainingCapacity());
                    // A negative sign to represent we should choose the pond
                    // with smaller penalty first.
                    return -firstPond.Period.Penalty.CompareTo(secondPond.Period.Penalty);
                });
            else
                return Comparer<Pond>.Create((firstPond, secondPond) =>
                {
                    if (firstPond.Period.Penalty == secondPond.Period.Penalty)
                        return secondPond.GetRemainingCapacity().CompareTo(firstPond.GetRemainingCapacity());
                    // A negative sign to represent we should choose the pond
                    // with smaller penalty first.
                    return -firstPond.Period.Penalty.CompareTo(secondPond.Period.Penalty);
                });
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

        public Func<Puddle, bool> DefaultPuddleCondition =>
            (puddle) =>
            {
                return !puddle.Exams.Any(I_exam_exclusive.Contains);
            };


        public Func<Pond, bool> DefaultPondCondition => (pond) => true;
        public Func<Pond, bool> CreatePondCondition(Exam exam)
        {
            return (pond) =>
            {
                return pond.Period.DurationMinutes >= exam.Duration;
            };
        }
    }
}
