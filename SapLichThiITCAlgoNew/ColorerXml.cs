using static SapLichThiITCCore.DatasetXml;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace SapLichThiITCAlgoNew
{

    public class ExamColoring
    {
        private Dictionary<Exam, HashSet<Exam>> adjacencyList;
        private Dictionary<Exam, int> colorAssignment;
        private Dictionary<Exam, HashSet<int>> adjacentColors;
        private Dictionary<Exam, int> degree;
        private SortedSet<Exam> priorityQueue;

        public ExamColoring(Dictionary<Exam, HashSet<Exam>> adjacencyList)
        {
            this.adjacencyList = adjacencyList;
        }

        public void ColorExams()
        {
            // Reset color assignments
            colorAssignment = new Dictionary<Exam, int>();

            // If there are no exams, return empty color assignment
            if (adjacencyList.Count == 0)
            {
                return;
            }

            // Track saturation degree (number of different colors in adjacent vertices)
            Dictionary<Exam, HashSet<int>> adjacentColors = new Dictionary<Exam, HashSet<int>>();
            foreach (var exam in adjacencyList.Keys)
            {
                adjacentColors[exam] = new HashSet<int>();
            }

            // Track uncolored exams
            HashSet<Exam> uncoloredExams = new HashSet<Exam>(adjacencyList.Keys);

            while (uncoloredExams.Count > 0)
            {
                // Select the vertex with the highest saturation degree
                // In case of a tie, select the one with highest degree (most conflicts)
                Exam examToColor = null;
                int maxSaturation = -1;
                int maxDegree = -1;

                foreach (var exam in uncoloredExams)
                {
                    int saturation = adjacentColors[exam].Count;
                    int degree = adjacencyList[exam].Count;

                    if (saturation > maxSaturation ||
                        (saturation == maxSaturation && degree > maxDegree))
                    {
                        maxSaturation = saturation;
                        maxDegree = degree;
                        examToColor = exam;
                    }
                }

                // Find the smallest available color for the selected exam
                HashSet<int> usedColors = new HashSet<int>();
                foreach (var adjacentExam in adjacencyList[examToColor])
                {
                    if (colorAssignment.TryGetValue(adjacentExam, out int color))
                    {
                        usedColors.Add(color);
                    }
                }

                // Assign the smallest available color
                int smallestAvailableColor = 0;
                while (usedColors.Contains(smallestAvailableColor))
                {
                    smallestAvailableColor++;
                }

                colorAssignment[examToColor] = smallestAvailableColor;
                uncoloredExams.Remove(examToColor);

                // Update adjacent colors for neighbors
                foreach (var adjacentExam in adjacencyList[examToColor])
                {
                    if (uncoloredExams.Contains(adjacentExam))
                    {
                        adjacentColors[adjacentExam].Add(smallestAvailableColor);
                    }
                }
            }

        }

       
        public Dictionary<Exam, int> GetColors()
        {
            return colorAssignment;
        }
    }



    public class ColorerXml
    {
        public required List<Exam> I_exams { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_linkages { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> I_exam_requires { get; set; }
        public required Dictionary<Exam, HashSet<(Exam exam1, Exam exam2)>> I_exam_after { get; set; }
        public required HashSet<Exam> I_exam_exclusive { get; set; }
        private Dictionary<Exam, HashSet<Student>> P_exams_students { get; set; }
        public Dictionary<int, HashSet<Exam>> O_color_exams { get; set; }

        public ColorerXml Initialize()
        {
            O_color_exams = new();
            P_exams_students = I_exams.ToDictionary(x => x, x => x.Students.ToHashSet());
            return this;
        }
        private Dictionary<Exam, int> ColorExams()
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

                        if (degree > maxDegree || degree == maxDegree && selected == null)
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

        public ColorerXml Run()
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
                    .ThenByDescending(x => x.Students.Count)
                    .ToList();
                HashSet<Exam> thisColoredExams = new();
                var bestExam = sortedExams[0];
                thisColoredExams.Add(bestExam);
                notColoredExams.Remove(bestExam);

                var locallySortedExams = notColoredExams
                    .Where(x => !I_exam_linkages[bestExam].Contains(x))
                    .OrderByDescending(I_exam_requires.ContainsKey)
                    .ThenByDescending(x => I_exam_linkages[x].Count)
                    .ThenByDescending(x => x.Students.Count)
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

            // 
            var newColorer = new ExamColoring(I_exam_linkages);
            newColorer.ColorExams();
            var newColor = newColorer.GetColors();
            int maxColorCount2 = newColor.Select(c =>c.Value).Distinct().Count();
            /*var resultRaw = ColorExams();
            O_color_exams = resultRaw.Select(x => x.Key).GroupBy(x => resultRaw[x]).ToDictionary(x => x.Key, x => x.ToHashSet());*/
            // Return
            return this;
        }
    }
}
