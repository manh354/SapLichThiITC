using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiITCAlgo
{
    public class Colorer
    {
        public required List<Exam> I_exams { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_linkages { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_requires { get; set; }
        public required Dictionary<Exam, HashSet<(Exam exam1, Exam exam2)>> I_exam_after { get; set; }
        public required HashSet<Exam> I_exam_exclusive { get; set; }
        private Dictionary<Exam, HashSet<int>> P_exams_studentIds { get; set; }
        public Dictionary<int, HashSet<Exam>> O_color_exams { get; set; }

        public Colorer Initialize()
        {
            O_color_exams = new();
            P_exams_studentIds = I_exams.ToDictionary(x => x, x => x.StudentIds.ToHashSet());
            return this;
        }
        public Dictionary<Exam, int> ColorExams()
        {
            var colors = new Dictionary<Exam, int>();
            int currentColor = 0;

            // Collect all unique exams from both keys and adjacency lists
            var allExams = new HashSet<Exam>();
            foreach (var exam in I_exam_linkages.Keys)
            {
                allExams.Add(exam);
                foreach (var adj in I_exam_linkages[exam])
                {
                    allExams.Add(adj);
                }
            }

            var uncolored = new HashSet<Exam>(allExams);

            while (uncolored.Count > 0)
            {
                currentColor++;
                var currentColorGroup = new HashSet<Exam>();
                var candidates = new HashSet<Exam>(uncolored);

                while (candidates.Count > 0)
                {
                    // Select the exam with the maximum degree in the current candidates
                    Exam selected = null;
                    int maxDegree = -1;

                    foreach (var exam in candidates)
                    {
                        int degree = 0;
                        if (I_exam_linkages.TryGetValue(exam, out var adjacents))
                        {
                            degree = adjacents.Count(adj => candidates.Contains(adj));
                        }
                        // If exam is not a key in the dictionary, its adjacency list is empty

                        if (degree > maxDegree || (degree == maxDegree && selected == null))
                        {
                            maxDegree = degree;
                            selected = exam;
                        }
                    }

                    if (selected == null)
                        break;

                    currentColorGroup.Add(selected);
                    candidates.Remove(selected);

                    // Remove all adjacent exams of the selected one from candidates
                    if (I_exam_linkages.TryGetValue(selected, out var selectedAdjacents))
                    {
                        foreach (var adj in selectedAdjacents)
                        {
                            candidates.Remove(adj);
                        }
                    }
                }

                // Assign the current color to all in the current group
                foreach (var exam in currentColorGroup)
                {
                    colors[exam] = currentColor;
                    uncolored.Remove(exam);
                }
            }

            return colors;
        }

        public Colorer Run()
        {
            // Prepare
            int colorIndex = 0;
            var notColoredExams = I_exams.ToHashSet();


            // Process
            while (notColoredExams.Count > 0)
            {
                var sortedExams = notColoredExams
                    .OrderByDescending(I_exam_requires.ContainsKey)
                    .ThenByDescending(x => I_exam_linkages[x].Count)
                    .ThenByDescending(x => x.StudentIds.Count)
                    .ToList();
                HashSet<Exam> thisColoredExams = new();
                var bestExam = sortedExams[0];
                thisColoredExams.Add(bestExam);
                notColoredExams.Remove(bestExam);

                var locallySortedExams = notColoredExams
                    .Where(x => !I_exam_linkages[bestExam].Contains(x))
                    .OrderByDescending(I_exam_requires.ContainsKey)
                    .ThenByDescending(x => I_exam_linkages[x].Count)
                    .ThenByDescending(x => x.StudentIds.Count)
                    .ToList();

                foreach (var sortedExam in locallySortedExams)
                {
                    if (thisColoredExams.All(x => !I_exam_linkages[x].Contains(sortedExam) || !I_exam_linkages[sortedExam].Contains(x)))
                    {
                        thisColoredExams.Add(sortedExam);
                        notColoredExams.Remove(sortedExam);
                    }
                }

                O_color_exams.Add(colorIndex, thisColoredExams);
                colorIndex++;
            }
            /*var resultRaw = ColorExams();
            O_color_exams = resultRaw.Select(x => x.Key).GroupBy(x => resultRaw[x]).ToDictionary(x => x.Key, x => x.ToHashSet());*/
            // Return
            return this;
        }
    }
}
