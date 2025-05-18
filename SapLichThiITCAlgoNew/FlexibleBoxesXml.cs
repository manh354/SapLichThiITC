
using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetXml;


namespace SapLichThiITCAlgoNew
{
    public class Puddle
    {
        public required Room Room { get; set; }
        public required int Penalty { get; set; }
        public required bool Available { get; set; }
        public required Exam? Exam { get; set; }
        private int _totalStudents;
        private bool HaveAlt { get; set; } = false;
        public Puddle()
        {
            _totalStudents = 0;
        }
        // Method to calculate the remaining capacity
        public int GetRemainingCapacity() => Room.Size ;
        public int GetRemainingCapacityAlt() => Room.AltSize ;


        // Method to get the total capacity
        public int GetCapacity() => Room.Size;

        // Method to get the used capacity
        public int GetUsedCapacity() => _totalStudents;

        // Add an exam and update the capacity
        public void AssignExam(Exam exam, Period period)
        {
            if (Exam != null)
            {
                throw new InvalidOperationException("Course must be nul before assignment.");
            }
            Exam = exam;
            Exam.Assignment.Assign(period, this.Room);
            // Update the total number of students
            _totalStudents = exam.Students.Count;
        }

        public void UpdateAssignmentOnlyExam(Period period)
        {
            Exam?.Assignment.Assign(period, this.Room);
        }

        // Remove an exam and update the capacity
        public void RemoveExam(Exam exam)
        {
            if (Exam == exam)
            {
                Exam.Assignment.Clear();
                Exam = null;
                _totalStudents = 0;
            }
            else
            {
                throw new InvalidOperationException("Trying to remove non-existence exam.");
            }
        }

        public void Clear()
        {
            Exam?.Assignment.Clear();
            Exam = null;
            _totalStudents = 0;
        }

        public Exam ClearAndReturn()
        {
            Exam exam = Exam;
            Clear();
            return exam;
        }
    }
    public class Pond
    {
        public required Period Period { get; set; }
        public required int Penalty { get; set; }
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
        public int GetRemainingCapacity() => Puddles.Where(x => x.Available).Sum(p => p.GetRemainingCapacity());

        // Method to get the total capacity of all puddles
        public int GetCapacity() => Puddles.Sum(p => p.GetCapacity());

        public void AddExam(Puddle puddle, Exam? exam)
        {
            if (exam == null)
                return;
            if (!Exams.Contains(exam))
            {
                puddle.AssignExam(exam, this.Period);
                Exams.Add(exam);
                _totalStudents += exam.Students.Count;
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
                _totalStudents -= exam.Students.Count;
            }
        }

        public Pond DeepClone()
        {
            var clone = new Pond
            {
                Period = this.Period,
                Penalty = this.Penalty,
                Puddles = this.Puddles.Select(p => new Puddle
                {
                    Room = p.Room,
                    Available = p.Available,
                    Penalty = p.Penalty,
                    Exam = p.Exam,
                }).ToList(),
                Exams = new HashSet<Exam>(this.Exams),
                _totalStudents = this._totalStudents
            };
            return clone;
        }

        public void CopyAndUpdateExamAssignmentFrom(Pond other)
        {
            Period = other.Period;
            Puddles = other.Puddles.Select(p => new Puddle
            {
                Room = p.Room,
                Available = p.Available,
                Penalty = p.Penalty,
                Exam = p.Exam,
            }).ToList();
            Exams = new HashSet<Exam>(other.Exams);
            _totalStudents = other._totalStudents;
            UpdateAssignmentOnlyExam();
        }

        public void UpdateAssignmentOnlyExam()
        {
            foreach (var puddle in Puddles)
            {
                puddle.UpdateAssignmentOnlyExam(Period);
            }
        }


        public bool GetSuitablePuddleForExam(Exam exam, out Puddle? chosenPuddle, out List<Puddle> suitablePuddles, Func<Puddle, bool>? condition, Comparer<Puddle>? comparer)
        {
            IEnumerable<Puddle> tempSuitablePuddles = Puddles;
            if (condition != null)
                tempSuitablePuddles = Puddles.Where(condition);
            suitablePuddles = tempSuitablePuddles.ToList();
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
            Exams.Clear();
            _totalStudents = 0;
        }


        public List<Exam> ClearAndReturn()
        {
            var result = new List<Exam>();
            foreach (var puddle in Puddles)
            {
                Exam exam = puddle.ClearAndReturn();
                if(exam != null)
                {
                    result.Add(exam);
                }
            }
            Exams.Clear();
            _totalStudents = 0;
            return result;
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
            suitablePonds = tempSuitablePonds.ToList();
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

        public void UpdateAssignmentOnly()
        {
            foreach (var pond in Ponds)
            {
                pond.UpdateAssignmentOnlyExam();
            }
        }

        public bool GetSuitablePondsForExams(List<Exam> exams, out Pond? chosenPond, out List<Pond> suitablePonds, Func<Pond, bool>? condition, Comparer<Pond>? comparer)
        {
            IEnumerable<Pond> tempSuitablePonds = Ponds;
            if (condition != null)
                tempSuitablePonds = Ponds.Where(condition);
            suitablePonds = tempSuitablePonds.ToList();
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
