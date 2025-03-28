using SapLichThiITCAlgo;
using SapLichThiITCCore;

public class Evaluator2
{
    private readonly Lake _timetable;
    private readonly List<DatasetExam.Exam> _allExams;
    private readonly List<DatasetExam.Period> _periods;
    private readonly List<DatasetExam.Room> _rooms;
    private readonly List<DatasetExam.InstitutionalWeighting> _weights;
    private readonly List<DatasetExam.PeriodHardConstraint> _periodHardConstraints;
    private readonly List<DatasetExam.RoomHardConstraint> _roomHardConstraints;

    // Precomputed data
    private Dictionary<int, DatasetExam.Exam> _examMap;
    private Dictionary<int, DatasetExam.Period> _periodMap;
    private Dictionary<int, DatasetExam.Room> _roomMap;
    private Dictionary<int, List<int>> _studentExams;
    private Dictionary<int, (DatasetExam.Period Period, DatasetExam.Room Room)> _examAssignments;

    public Evaluator2(
        Lake timetable,
        List<DatasetExam.Exam> allExams,
        List<DatasetExam.Period> periods,
        List<DatasetExam.Room> rooms,
        List<DatasetExam.InstitutionalWeighting> weights,
        List<DatasetExam.PeriodHardConstraint> periodHardConstraints,
        List<DatasetExam.RoomHardConstraint> roomHardConstraints)
    {
        _timetable = timetable;
        _allExams = allExams;
        _periods = periods;
        _rooms = rooms;
        _weights = weights;
        _periodHardConstraints = periodHardConstraints;
        _roomHardConstraints = roomHardConstraints;

        InitializeMappings();
        PrecomputeExamAssignments();
    }

    private void InitializeMappings()
    {
        _examMap = _allExams.ToDictionary(e => e.Id);
        _periodMap = _periods.ToDictionary(p => p.Id);
        _roomMap = _rooms.ToDictionary(r => r.Id);

        // Map students to their exams
        _studentExams = new Dictionary<int, List<int>>();
        foreach (var exam in _allExams)
        {
            foreach (var studentId in exam.StudentIds)
            {
                if (!_studentExams.ContainsKey(studentId))
                    _studentExams[studentId] = new List<int>();
                _studentExams[studentId].Add(exam.Id);
            }
        }
    }

    private void PrecomputeExamAssignments()
    {
        _examAssignments = new Dictionary<int, (DatasetExam.Period Period, DatasetExam.Room Room)>();
        foreach (var pond in _timetable.Ponds)
        {
            foreach (var puddle in pond.Puddles)
            {
                foreach (var exam in puddle.Exams)
                {
                    _examAssignments[exam.Id] = (pond.Period, puddle.Room);
                }
            }
        }
    }

    // --- Hard Constraints ---
    public bool CheckHardConstraints(out int distanceToFeasibility)
    {
        int violations = 0;

        // 1. No student in multiple exams in the same period (Conflict)
        foreach (var pond in _timetable.Ponds)
        {
            var studentsInPeriod = new HashSet<int>();
            foreach (var puddle in pond.Puddles)
            {
                foreach (var exam in puddle.Exams)
                {
                    foreach (var studentId in exam.StudentIds)
                    {
                        if (!studentsInPeriod.Add(studentId))
                            violations++; // Conflict
                    }
                }
            }
        }

        // 2. Room capacities not exceeded (RoomOccupancy)
        foreach (var pond in _timetable.Ponds)
        {
            foreach (var puddle in pond.Puddles)
            {
                int totalStudents = puddle.Exams.Sum(e => e.StudentIds.Count);
                if (totalStudents > puddle.Room.Capacity)
                    violations++;
            }
        }

        // 3. Period durations respected (PeriodUtilization)
        foreach (var pond in _timetable.Ponds)
        {
            foreach (var puddle in pond.Puddles)
            {
                foreach (var exam in puddle.Exams)
                {
                    if (exam.Duration > pond.Period.DurationMinutes)
                        violations++;
                }
            }
        }

        // 4. Period-related hard constraints (PeriodRelated)
        foreach (var constraint in _periodHardConstraints)
        {
            if (!_examAssignments.ContainsKey(constraint.ExamId1) ||
                !_examAssignments.ContainsKey(constraint.ExamId2))
                continue;

            var period1 = _examAssignments[constraint.ExamId1].Period;
            var period2 = _examAssignments[constraint.ExamId2].Period;

            switch (constraint.Type)
            {
                case "AFTER":
                    if (period1.Date > period2.Date ||
                        (period1.Date == period2.Date && period1.StartTime >= period2.StartTime))
                        violations++;
                    break;
                case "COINCIDENCE":
                    if (period1.Id != period2.Id)
                        violations++;
                    break;
                case "EXCLUSION":
                    if (period1.Id == period2.Id)
                        violations++;
                    break;
            }
        }

        // 5. Room-related hard constraints (RoomRelated)
        foreach (var constraint in _roomHardConstraints)
        {
            if (!_examAssignments.ContainsKey(constraint.ExamId))
                continue;

            var assignedRoom = _examAssignments[constraint.ExamId].Room;
            switch (constraint.Type)
            {
                case "EXCLUSIVE":
                    var examPeriod = _examAssignments[constraint.ExamId].Period;
                    foreach (var pond in _timetable.Ponds.Where(p => p.Period.Id == examPeriod.Id))
                    {
                        foreach (var puddle in pond.Puddles.Where(p => p.Room.Id == assignedRoom.Id))
                        {
                            if (puddle.Exams.Count > 1)
                                violations++;
                        }
                    }
                    break;
            }
        }

        distanceToFeasibility = violations;
        return violations == 0;
    }

    // --- Soft Constraints ---
    public double CalculateSoftPenalty()
    {
        double penalty = 0;

        // Get all institutional weights
        var twoInRowWeight = _weights.First(w => w.Type == "TWOINAROW").Weight;
        var twoInDayWeight = _weights.First(w => w.Type == "TWOINADAY").Weight;
        var periodSpreadWeight = _weights.First(w => w.Type == "PERIODSPREAD").Weight;
        var mixedDurationsWeight = _weights.First(w => w.Type == "NONMIXEDDURATIONS").Weight;
        var frontLoadWeight = _weights.First(w => w.Type == "FRONTLOAD").Weight;

        // 1. Two exams in a row
        penalty += CalculateTwoInARowPenalty() * twoInRowWeight;

        // 2. Two exams in a day (non-consecutive)
        penalty += CalculateTwoInADayPenalty() * twoInDayWeight;

        // 3. Period spread
        penalty += CalculatePeriodSpreadPenalty() * periodSpreadWeight;

        // 4. Mixed durations
        penalty += CalculateMixedDurationsPenalty() * mixedDurationsWeight;

        // 5. Front-load penalty
        penalty += CalculateFrontLoadPenalty() * frontLoadWeight;

        // 6. Room penalties
        penalty += CalculateRoomPenalties();

        // 7. Period penalties
        penalty += CalculatePeriodPenalties();

        return penalty;
    }

    private double CalculateTwoInARowPenalty()
    {
        double penalty = 0;
        foreach (var student in _studentExams)
        {
            var orderedExams = student.Value
                .Select(eId => _examAssignments[eId].Period)
                .OrderBy(p => p.Date).ThenBy(p => p.StartTime)
                .ToList();

            for (int i = 0; i < orderedExams.Count - 1; i++)
            {
                var current = orderedExams[i];
                var next = orderedExams[i + 1];
                if (current.Date == next.Date &&
                    next.StartTime == current.StartTime.Add(TimeSpan.FromMinutes(current.DurationMinutes)))
                {
                    penalty++;
                }
            }
        }
        return penalty;
    }

    private double CalculateTwoInADayPenalty()
    {
        double penalty = 0;
        foreach (var student in _studentExams)
        {
            var sameDayExams = student.Value
                .Select(eId => _examAssignments[eId].Period)
                .GroupBy(p => p.Date)
                .Where(g => g.Count() >= 2);

            foreach (var dayGroup in sameDayExams)
            {
                var orderedPeriods = dayGroup.OrderBy(p => p.StartTime).ToList();
                for (int i = 0; i < orderedPeriods.Count - 1; i++)
                {
                    for (int j = i + 1; j < orderedPeriods.Count; j++)
                    {
                        if (orderedPeriods[j].StartTime > orderedPeriods[i].StartTime.Add(
                            TimeSpan.FromMinutes(orderedPeriods[i].DurationMinutes)))
                        {
                            penalty++;
                        }
                    }
                }
            }
        }
        return penalty;
    }

    private double CalculatePeriodSpreadPenalty()
    {
        double penalty = 0;
        int spread = _weights.First(w => w.Type == "PERIODSPREAD").Parameters[0];

        foreach (var student in _studentExams)
        {
            var exams = student.Value
                .Select(eId => _examAssignments[eId])
                .OrderBy(a => a.Period.Date).ThenBy(a => a.Period.StartTime)
                .ToList();

            for (int i = 0; i < exams.Count - 1; i++)
            {
                for (int j = i + 1; j < exams.Count; j++)
                {
                    if ((exams[j].Period.Date - exams[i].Period.Date).TotalDays * 1440 +
                        (exams[j].Period.StartTime - exams[i].Period.StartTime).TotalMinutes <= spread)
                    {
                        penalty++;
                    }
                }
            }
        }
        return penalty;
    }

    private double CalculateMixedDurationsPenalty()
    {
        double penalty = 0;
        foreach (var pond in _timetable.Ponds)
        {
            foreach (var puddle in pond.Puddles)
            {
                var durations = puddle.Exams.Select(e => e.Duration).Distinct().Count();
                if (durations > 1)
                    penalty += puddle.Exams.Count * (durations - 1);
            }
        }
        return penalty;
    }

    private double CalculateFrontLoadPenalty()
    {
        var frontLoadParams = _weights.First(w => w.Type == "FRONTLOAD").Parameters;
        int numLargeExams = frontLoadParams[0];
        int lastPeriodsToAvoid = frontLoadParams[1];

        // Get largest exams
        var largeExams = _allExams
            .OrderByDescending(e => e.StudentIds.Count)
            .ThenBy(e => e.Id)
            .Take(numLargeExams)
            .Select(e => e.Id)
            .ToList();

        // Get last periods to avoid
        var allPeriods = _periods.OrderBy(p => p.Date).ThenBy(p => p.StartTime).ToList();
        var forbiddenPeriods = allPeriods.TakeLast(lastPeriodsToAvoid).Select(p => p.Id).ToList();

        double penalty = 0;
        foreach (var examId in largeExams)
        {
            if (_examAssignments.TryGetValue(examId, out var assignment) &&
                forbiddenPeriods.Contains(assignment.Period.Id))
            {
                penalty++;
            }
        }
        return penalty * frontLoadParams[2]; // Multiply by penalty weight
    }

    private double CalculateRoomPenalties()
    {
        double penalty = 0;
        foreach (var pond in _timetable.Ponds)
        {
            foreach (var puddle in pond.Puddles)
            {
                penalty += puddle.Room.Penalty * puddle.Exams.Count;
            }
        }
        return penalty;
    }

    private double CalculatePeriodPenalties()
    {
        double penalty = 0;
        foreach (var pond in _timetable.Ponds)
        {
            penalty += pond.Period.Penalty * pond.Puddles.Sum(p => p.Exams.Count);
        }
        return penalty;
    }
}