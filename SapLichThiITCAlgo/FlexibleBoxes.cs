using SapLichThiITCCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiITCAlgo
{
    public class Puddle
    {
        public required Room Room { get; set; }
        public required List<Exam> Exams { get; set; }
        private int _totalStudents;

        public Puddle() { _totalStudents = 0; }
        // Method to calculate the remaining capacity
        public int GetRemainingCapacity() => Room.Capacity - _totalStudents;

        // Method to get the total capacity
        public int GetCapacity() => Room.Capacity;

        // Method to get the used capacity
        public int GetUsedCapacity() => _totalStudents;

        // Add an exam and update the capacity
        public void AddExam(Exam exam)
        {
            Exams.Add(exam);
            // Update the total number of students
            _totalStudents += exam.StudentIds.Count;
        }

        // Remove an exam and update the capacity
        public void RemoveExam(Exam exam)
        {
            if (Exams.Remove(exam))
            {
                // Update the total number of students
                _totalStudents -= exam.StudentIds.Count;
            }
        }

        public void Clear()
        {
            Exams.Clear();
        }
    }
    public class Pond
    {
        public required Period Period { get; set; }
        public required List<Puddle> Puddles { get; set; }
        public required HashSet<Exam> Exams { get; set; }
        private int _totalStudents;
        public Pond()
        {
            _totalStudents = 0;
        }

        // Method to get the total used capacity of all puddles
        public int GetUsedCapacity() => _totalStudents;

        // Method to get the remaining capacity of all puddles
        public int GetRemainingCapacity() => Puddles.Sum(p => p.GetRemainingCapacity());

        // Method to get the total capacity of all puddles
        public int GetCapacity() => Puddles.Sum(p => p.GetCapacity());

        public void AddExam(Puddle puddle, Exam exam)
        {
            if (!Exams.Contains(exam))
            {
                puddle.AddExam(exam);
                Exams.Add(exam);
                _totalStudents += exam.StudentIds.Count;
            }
            else
            {
                throw new InvalidOperationException("Add exam false");
            }
        }

        public void RemoveExam(Puddle puddle, Exam exam)
        {
            if (Exams.Contains(exam))
            {
                puddle.RemoveExam(exam);
                Exams.Remove(exam);
                _totalStudents -= exam.StudentIds.Count;
            }
        }

        public Pond DeepClone()
        {
            var clone = new Pond
            {
                Period = this.Period,
                Puddles = this.Puddles.Select(p => new Puddle
                {
                    Room = p.Room,
                    Exams = p.Exams.ToList()
                }).ToList(),
                Exams = new HashSet<Exam>(this.Exams),
                _totalStudents = this._totalStudents
            };
            return clone;
        }

        public void CopyFrom(Pond other)
        {
            Period = other.Period;
            Puddles = other.Puddles.Select(p => new Puddle
            {
                Room = p.Room,
                Exams = p.Exams.ToList()
            }).ToList();
            Exams = new HashSet<Exam>(other.Exams);
            _totalStudents = other._totalStudents;
        }


        public bool GetSuitablePuddleForExam(Exam exam, out Puddle? chosenPuddle, out List<Puddle> suitablePuddles, Func<Puddle, bool>? condition, Comparer<Puddle>? comparer)
        {
            IEnumerable<Puddle> tempSuitablePuddles = Puddles;
            if (condition != null)
                tempSuitablePuddles = Puddles.Where(condition);
            suitablePuddles = tempSuitablePuddles.Where(x => x.GetRemainingCapacity() >= exam.StudentIds.Count).ToList();
            if (suitablePuddles.Count > 0)
            {
                if (comparer != null)
                {
                    // Sort by Descending order
                    suitablePuddles.SortDescending(comparer);
                }
                //-------------------------
                chosenPuddle = suitablePuddles[0];
                return true;
            }
            chosenPuddle = null;
            return false;
        }


        public void Clear()
        {
            foreach (var puddle in Puddles)
            {
                puddle.Clear();
            }
        }
    }
    public class Lake
    {
        public required List<Pond> Ponds { get; set; }
        public required Dictionary<Exam, HashSet<Exam>> Linkages { get; set; }
        public bool GetSuitablePondsForExam(Exam checkExam, out Pond? chosenPond, out List<Pond> suitablePonds, Func<Pond, bool>? condition, Comparer<Pond>? comparer)
        {
            IEnumerable<Pond> tempSuitablePonds = Ponds;
            if (condition != null)
                tempSuitablePonds = Ponds.Where(condition);
            suitablePonds = tempSuitablePonds.Where(pond =>
            {
                if (Linkages.TryGetValue(checkExam, out var linkage))
                {
                    foreach (var exam in pond.Exams)
                    {
                        if (linkage.Contains(exam))
                            return false;
                    }
                }
                return true;
            }).ToList();
            if (suitablePonds.Count > 0)
            {
                // Sort by Descending order
                if (comparer != null)
                    suitablePonds.SortDescending(comparer);
                //-------------------------
                chosenPond = suitablePonds[0];
                return true;
            }
            chosenPond = null;
            return false;
        }

        public bool GetSuitablePondsForExams(List<Exam> exams, out Pond? chosenPond, out List<Pond> suitablePonds, Func<Pond, bool>? condition, Comparer<Pond>? comparer)
        {
            IEnumerable<Pond> tempSuitablePonds = Ponds;
            if (condition != null)
                tempSuitablePonds = Ponds.Where(condition);
            suitablePonds = tempSuitablePonds.Where(pond =>
            {
                foreach (var checkExam in exams)
                {
                    if (Linkages.TryGetValue(checkExam, out var linkage))
                    {
                        foreach (var exam in pond.Exams)
                        {
                            if (linkage.Contains(exam))
                                return false;
                        }
                    }
                }
                return true;
            }).ToList();
            if (suitablePonds.Count > 0)
            {
                // Sort by Descending order
                if (comparer != null)
                {
                    suitablePonds.SortDescending(comparer);
                }
                //-------------------------
                chosenPond = suitablePonds[0];
                return true;
            }
            chosenPond = null;
            return false;
        }

        public bool GetSuitablePuddleForExam(Pond pond, Exam exam, out Puddle? chosenPuddle, out List<Puddle> suitablePuddles, Func<Puddle, bool>? condition, Comparer<Puddle>? comparer)
        {
            return pond.GetSuitablePuddleForExam(exam, out chosenPuddle, out suitablePuddles, condition, comparer);
        }

        public void AddElementToPuddle(Pond pond, Puddle puddle, Exam exam)
        {
            pond.AddExam(puddle, exam);
        }

        public void Clear()
        {
            foreach (var pond in Ponds)
            {
                pond.Clear();
            }
        }
    }
}
