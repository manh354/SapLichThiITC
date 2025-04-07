using static SapLichThiITCCore.DatasetXml;

public class EvaluationResult
{
    public bool IsValid { get; set; } = true;
    public int TotalPenalty { get; set; }
    public List<string> HardConstraintViolations { get; } = new List<string>();
    public List<string> SoftConstraintViolations { get; } = new List<string>();
}

public class EvaluatorXml
{
    private readonly ExamTimetablingData _data;
    private readonly Dictionary<Exam, (Period Period, List<Room> Rooms)> _assignments = new();
    private readonly Dictionary<Exam, int> _examStudentCounts = new Dictionary<Exam, int>();
    private readonly Dictionary<Exam, List<Instructor>> _examInstructors = new Dictionary<Exam, List<Instructor>>();

    public EvaluatorXml(ExamTimetablingData data)
    {
        _data = data;
        PrecomputeData();
    }

    private void PrecomputeData()
    {
        // Calculate student counts per exam
        foreach (var student in _data.Students)
        {
            foreach (var examId in student.Exams)
            {
                _examStudentCounts[examId] = _examStudentCounts.GetValueOrDefault(examId, 0) + 1;
            }
        }

        // Map instructors to exams
        foreach (var instructor in _data.Instructors)
        {
            foreach (var exam in instructor.Exams)
            {
                if (!_examInstructors.ContainsKey(exam))
                    _examInstructors[exam] = new List<Instructor>();
                _examInstructors[exam].Add(instructor);
            }
        }

        foreach (var exam in _data.Exams)
        {
            _assignments.Add(exam, (exam.Assignment.Period, exam.Assignment.Rooms));
        }
    }

    public EvaluationResult Evaluate()
    {
        var result = new EvaluationResult();

        CheckHardConstraints(result);

        CalculateSoftPenalties(result);



        return result;
    }

    private void CheckHardConstraints(EvaluationResult result)
    {
        // Check all exams are assigned
        foreach (var exam in _data.Exams)
        {
            if (!_assignments.ContainsKey(exam))
            {
                result.HardConstraintViolations.Add($"Course {exam.Id} has no assignment");
                result.IsValid = false;
            }
        }

        var roomPeriodUsage = new Dictionary<(Room, Period), int>();

        foreach (var exam in _data.Exams)
        {
            if (!_assignments.TryGetValue(exam, out var assignment)) continue;

            // Check valid period
            if (!exam.AvailablePeriods.Any(p => p.Period == assignment.Period))
            {
                result.HardConstraintViolations.Add($"Course {exam.Id} assigned to invalid period {assignment.Period}");
                result.IsValid = false;
            }

            // Check room availability and capacity
            if (exam.MaxRooms == 0 && assignment.Rooms.Count > 0)
            {
                result.HardConstraintViolations.Add($"Course {exam.Id} should not have room assignments");
                result.IsValid = false;
            }

            int totalCapacity = 0;
            foreach (var room in assignment.Rooms)
            {
                if (room == null)
                {
                    result.HardConstraintViolations.Add($"Course {exam.Id} assigned to invalid room {room}");
                    result.IsValid = false;
                    continue;
                }

                // Check room availability for period
                var roomPeriod = room.PeriodPreferences.FirstOrDefault(p => p.Period == assignment.Period);
                if (roomPeriod?.Available == false)
                {
                    result.HardConstraintViolations.Add($"Room {room} unavailable for exam {exam.Id} in period {assignment.Period}");
                    result.IsValid = false;
                }

                // Track room usage
                var key = (room, assignment.Period);
                roomPeriodUsage[key] = roomPeriodUsage.GetValueOrDefault(key, 0) + 1;

                totalCapacity += exam.AltSeating ? room.AltSize : room.Size;
            }

            // Check capacity
            if (_examStudentCounts.TryGetValue(exam, out var students) && totalCapacity < students)
            {
                result.HardConstraintViolations.Add($"Insufficient capacity for exam {exam.Id} ({students} students, {totalCapacity} seats)");
                result.IsValid = false;
            }

            // Check max rooms
            if (assignment.Rooms.Count > exam.MaxRooms)
            {
                result.HardConstraintViolations.Add($"Course {exam.Id} exceeds max rooms ({exam.MaxRooms})");
                result.IsValid = false;
            }
        }

        // Check room period conflicts
        foreach (var kvp in roomPeriodUsage.Where(x => x.Value > 1))
        {
            result.HardConstraintViolations.Add($"Room {kvp.Key.Item1} in period {kvp.Key.Item2} has {kvp.Value} exams");
            result.IsValid = false;
        }

        // Check student conflicts
        foreach (var student in _data.Students)
        {
            var periods = student.Exams
                .Where(_assignments.ContainsKey)
                .Select(eid => _assignments[eid].Period)
                .ToList();

            if (periods.Distinct().Count() != periods.Count)
            {
                result.HardConstraintViolations.Add($"Student {student.Id} has exam conflicts");
                result.IsValid = false;
            }
        }

        // Check constraints
        foreach (var constraint in _data.Constraints.Where(c => c.IsHard))
        {
            var exams = constraint.Exams.Where(_assignments.ContainsKey).ToList();
            if (exams.Count < 2) continue;

            switch (constraint.Type)
            {
                case ConstraintType.DifferentPeriod:
                    if (exams.Select(eid => _assignments[eid].Period).Distinct().Count() != exams.Count)
                    {
                        result.HardConstraintViolations.Add($"Constraint {constraint.Id} (DifferentPeriod) violated");
                        result.IsValid = false;
                    }
                    break;

                case ConstraintType.SamePeriod:
                    var firstPeriod = _assignments[exams[0]].Period;
                    if (exams.Any(eid => _assignments[eid].Period != firstPeriod))
                    {
                        result.HardConstraintViolations.Add($"Constraint {constraint.Id} (SamePeriod) violated");
                        result.IsValid = false;
                    }
                    break;
            }
        }
    }

    private void CalculateSoftPenalties(EvaluationResult result)
    {
        // Period penalties
        foreach (var (exam, (period, rooms)) in _assignments)
        {
            result.TotalPenalty += period?.Penalty??0;
            result.TotalPenalty += exam.AvailablePeriods.FirstOrDefault(p => p.Period == period)?.Penalty ?? 0;
        }

        // Room penalties
        foreach (var (exam, (period, rooms)) in _assignments)
        {
            foreach (var room in rooms)
            {
                var roomPeriod = room.PeriodPreferences.FirstOrDefault(p => p.Period == period);
                result.TotalPenalty += roomPeriod?.Penalty ?? 0;

                var examRoom = exam.AvailableRooms.FirstOrDefault(r => r.Room == room);
                result.TotalPenalty += examRoom?.Penalty?? 0 ;
            }
        }

        // Soft constraints
        foreach (var constraint in _data.Constraints.Where(c => !c.IsHard))
        {
            var exams = constraint.Exams.Where(_assignments.ContainsKey).ToList();
            if (exams.Count < 2) continue;

            bool violated = false;
            switch (constraint.Type)
            {
                case ConstraintType.DifferentPeriod:
                    violated = exams.Select(eid => _assignments[eid].Period).Distinct().Count() != exams.Count;
                    break;

                case ConstraintType.SamePeriod:
                    var firstPeriod = _assignments[exams[0]].Period;
                    violated = exams.Any(eid => _assignments[eid].Period != firstPeriod);
                    break;
            }

            if (violated) result.TotalPenalty += constraint.Weight;
        }
    }
}