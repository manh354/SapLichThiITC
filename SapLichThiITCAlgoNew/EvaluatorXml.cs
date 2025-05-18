using static SapLichThiITCCore.DatasetXml;

public class SoftConstraintWeights
{
    public int DirectConflict { get; set; } = 1;
    public int MoreThanTwoExamsPerDay { get; set; } = 1;
    public int BackToBackConflict { get; set; } = 1;
    public int DistanceBackToBackConflict { get; set; } = 1;
    public int PeriodPenalty { get; set; } = 1;
    public int RoomPenalty { get; set; } = 1;
    public int DistributionPenalty { get; set; } = 1;
    public int RoomSplit { get; set; } = 1;
    public int RoomSplitDistance { get; set; } = 1;
    public int RoomSize { get; set; } = 1;
    public int RotationPenalty { get; set; } = 1;
    public int LargeExamPenalty { get; set; } = 1;
    public int PerturbationPenalty { get; set; } = 1;
    public int DistanceThreshold { get; set; } = 670; // meters
    public int LargeExamThreshold { get; set; } = 600; // students
}

public class EvaluationResult
{

    // Hard constraints
    public bool IsValid { get; set; } = true;
    public int TotalPenalty
    {
        get
        {
            return DirectConflictPenalty
                + MoreThanTwoExamsPerDayPenalty
                + BackToBackConflictPenalty
                + DistanceBackToBackConflictPenalty
                + InstructorDirectConflictPenalty
                + InstructorMoreThanTwoExamsPerDayPenalty
                + InstructorBackToBackConflictPenalty
                + InstructorDistanceBackToBackConflictPenalty
                + PeriodPreferencePenalty
                + RoomPreferencePenalty
                + DistributionConstraintPenalty
                + RoomSplitCountPenalty
                + RoomSplitDistancePenalty
                + RoomSizePenalty
                + RotationPenalty
                + LargeExamPenalty
                + PerturbationPenalty;
        }
    }

    public List<string> HardConstraintViolations { get; } = new List<string>();
    public int NoAssignmentCount { get; set; } = 0;
    public int InvalidPeriodCount { get; set; } = 0;
    public int ShouldNotAssignToRoomCount { get; set; } = 0;
    public int InvalidRoomCount { get; set; } = 0;
    public int InavailableRoomPeriodCount { get; set; } = 0;
    public int InsufficientCapacityCount { get; set; } = 0;
    public int InvalidMaxRoomCount { get; set; } = 0;
    public int RoomPeriodConflictCount { get; set; } = 0;
    public int StudentConflictCount { get; set; } = 0;
    public int ConstraintCount { get; set; } = 0;

    // Student conflicts
    public int DirectConflictPenalty { get; set; }
    public int MoreThanTwoExamsPerDayPenalty { get; set; }
    public int BackToBackConflictPenalty { get; set; }
    public int DistanceBackToBackConflictPenalty { get; set; }

    // Instructor conflicts
    public int InstructorDirectConflictPenalty { get; set; }
    public int InstructorMoreThanTwoExamsPerDayPenalty { get; set; }
    public int InstructorBackToBackConflictPenalty { get; set; }
    public int InstructorDistanceBackToBackConflictPenalty { get; set; }

    // Period and room penalties
    public int PeriodPreferencePenalty { get; set; }
    public int RoomPreferencePenalty { get; set; }

    // Distribution constraints
    public int DistributionConstraintPenalty { get; set; }

    // Room-related penalties
    public int RoomSplitCountPenalty { get; set; }
    public int RoomSplitDistancePenalty { get; set; }
    public int RoomSizePenalty { get; set; }

    // Other penalties
    public int RotationPenalty { get; set; }
    public int LargeExamPenalty { get; set; }
    public int PerturbationPenalty { get; set; }


    public List<string> SoftConstraintViolations { get; } = new List<string>();
}

public class EvaluatorXml
{
    private readonly ExamTimetablingData _data;
    private readonly Dictionary<Exam, (Period Period, List<Room> Rooms)> _assignments = new();
    private readonly Dictionary<Exam, int> _examStudentCounts = new Dictionary<Exam, int>();
    private readonly Dictionary<Exam, List<Instructor>> _examInstructors = new Dictionary<Exam, List<Instructor>>();
    private readonly SoftConstraintWeights _weights = new SoftConstraintWeights();

    public EvaluatorXml(ExamTimetablingData data)
    {
        _data = data;
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
        ResetCalculation();
        PrecomputeData();
        CheckHardConstraints(result);


        CalculateStudentConflicts(result);
        CalculateInstructorConflicts(result);
        CalculatePeriodPenalties(result);
        CalculateRoomPenalties(result);
        CalculateDistributionPenalties(result);
        CalculateRoomSizePenalties(result);
        // CalculateRotationPenalties(result);
        CalculateLargeExamPenalties(result);

        return result;
    }

    public void ResetCalculation()
    {
        _assignments.Clear();
        _examStudentCounts.Clear();
        _examInstructors.Clear();
    }

    private void CheckHardConstraints(EvaluationResult result)
    {
        // Check all exams are assigned
        foreach (var exam in _data.Exams)
        {
            Assignment assignment = exam.Assignment;
            if (assignment.Rooms.Count == 0 || assignment.Period == null)
            {
                result.HardConstraintViolations.Add($"Course {exam.Id} has no assignment");
                result.NoAssignmentCount++;
                result.IsValid = false;
            }
        }

        var roomPeriodUsage = new Dictionary<(Room, Period), int>();

        foreach (var exam in _data.Exams)
        {
            if (!_assignments.TryGetValue(exam, out var assignment)) continue;

            // Check valid period
            if (assignment.Period != null && !exam.AvailablePeriods.Any(p => p.Period == assignment.Period))
            {
                result.HardConstraintViolations.Add($"Course {exam.Id} assigned to invalid period {assignment.Period}");
                result.InvalidPeriodCount++;
                result.IsValid = false;
            }

            // Check room availability and capacity
            if (exam.MaxRooms == 0 && assignment.Rooms.Count > 0)
            {
                result.HardConstraintViolations.Add($"Course {exam.Id} should not have room assignments");
                result.ShouldNotAssignToRoomCount++;
                result.IsValid = false;
            }

            int totalCapacity = 0;
            foreach (var room in assignment.Rooms)
            {
                if (room == null)
                {
                    result.HardConstraintViolations.Add($"Course {exam.Id} assigned to invalid room {room}");
                    result.InvalidRoomCount++;
                    result.IsValid = false;
                    continue;
                }

                // Check room availability for period
                var roomPeriod = room.PeriodPreferences.FirstOrDefault(p => p.Period == assignment.Period);
                if (roomPeriod?.Available == false)
                {
                    result.HardConstraintViolations.Add($"Room {room} unavailable for exam {exam.Id} in period {assignment.Period}");
                    result.InavailableRoomPeriodCount++;
                    result.IsValid = false;
                }

                // Track room usage
                var key = (room, assignment.Period);
                roomPeriodUsage[key] = roomPeriodUsage.GetValueOrDefault(key, 0) + 1;

                totalCapacity += exam.AltSeating ? room.AltSize : room.Size;
            }

            // Check capacity
            if ((totalCapacity != 0) && _examStudentCounts.TryGetValue(exam, out var students) && totalCapacity < students)
            {
                result.HardConstraintViolations.Add($"Insufficient capacity for exam {exam.Id} ({students} students, {totalCapacity} seats)");
                result.InsufficientCapacityCount++;
                result.IsValid = false;
            }

            // Check max rooms
            if (assignment.Rooms.Count > exam.MaxRooms)
            {
                result.HardConstraintViolations.Add($"Course {exam.Id} exceeds max rooms ({exam.MaxRooms})");
                result.InvalidMaxRoomCount++;
                result.IsValid = false;
            }
        }

        // Check room period conflicts
        foreach (var kvp in roomPeriodUsage.Where(x => x.Value > 1))
        {
            result.HardConstraintViolations.Add($"Room {kvp.Key.Item1} in period {kvp.Key.Item2} has {kvp.Value} exams");
            result.RoomPeriodConflictCount++;
            result.IsValid = false;
        }

        /*
        // Check student conflicts
        foreach (var student in _data.Students)
        {
            var periods = student.Exams
                .Where(_assignments.ContainsKey)
                .Select(eid => _assignments[eid].Period)
                .Where(p => p != null)
                .ToList();

            if (periods.Distinct().Count() != periods.Count)
            {
                result.HardConstraintViolations.Add($"Student {student.Id} has exam conflicts");
                result.StudentConflictCount++;
                result.IsValid = false;
            }
        }
        */

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
                        result.ConstraintCount++;
                        result.IsValid = false;
                    }
                    break;

                case ConstraintType.SamePeriod:
                    var firstPeriod = _assignments[exams[0]].Period;
                    if (exams.Any(eid => _assignments[eid].Period != firstPeriod))
                    {
                        result.HardConstraintViolations.Add($"Constraint {constraint.Id} (SamePeriod) violated");
                        result.ConstraintCount++;
                        result.IsValid = false;
                    }
                    break;

                case ConstraintType.Precedence:
                    var periods = exams.Select(eid => _assignments[eid].Period).ToList();
                    var sortedPeriods = periods.OrderBy(p => p.Id).ToList();
                    if(!periods.SequenceEqual(sortedPeriods))
                    {
                        result.HardConstraintViolations.Add($"Constraint {constraint.Id} (Precedence) violated");
                        result.ConstraintCount++;
                        result.IsValid = false;
                    }
                    break;
            }
        }
    }


    private void CalculateStudentConflicts(EvaluationResult result)
    {
        foreach (var student in _data.Students)
        {
            var studentExams = student.Exams
                .Where(e => e.Assignment.Period != null)
                .Select(e => e.Assignment)
                .ToList();

            var periodGroups = studentExams.GroupBy(a => a.Period);
            result.DirectConflictPenalty += periodGroups.Count(g => g.Count() > 1) * _weights.DirectConflict;

            var examsByDay = studentExams.GroupBy(a => a.Period.Day);
            foreach (var dayGroup in examsByDay)
            {
                var dayExams = dayGroup.ToList();
                if (dayExams.Count >= 3)
                {
                    result.MoreThanTwoExamsPerDayPenalty += _weights.MoreThanTwoExamsPerDay;
                }

                var periods = dayExams.Select(a => a.Period).OrderBy(p => p.Id).ToList();
                for (int i = 1; i < periods.Count; i++)
                {
                    if (AreConsecutivePeriods(periods[i - 1], periods[i]))
                    {
                        result.BackToBackConflictPenalty += _weights.BackToBackConflict;

                        var maxDistance = CalculateMaxDistanceBetweenExams(
                            dayExams.First(a => a.Period == periods[i]).Rooms,
                            dayExams.First(a => a.Period == periods[i]).Rooms);

                        if (maxDistance > _weights.DistanceThreshold)
                        {
                            result.DistanceBackToBackConflictPenalty += _weights.DistanceBackToBackConflict;
                        }
                    }
                }
            }
        }
    }

    private void CalculateInstructorConflicts(EvaluationResult result)
    {
        foreach (var instructor in _data.Instructors)
        {
            var instructorExams = instructor.Exams
                .Where(e => e.Assignment.Period != null)
                .Select(e => e.Assignment)
                .ToList();

            var periodGroups = instructorExams.GroupBy(a => a.Period);
            result.InstructorDirectConflictPenalty += periodGroups.Count(g => g.Count() > 1) * _weights.DirectConflict;

            var examsByDay = instructorExams.GroupBy(a => a.Period.Day);
            foreach (var dayGroup in examsByDay)
            {
                var dayExams = dayGroup.ToList();
                if (dayExams.Count >= 3)
                {
                    result.InstructorMoreThanTwoExamsPerDayPenalty += _weights.MoreThanTwoExamsPerDay;
                }

                var periods = dayExams.Select(a => a.Period).OrderBy(pid => pid).ToList();
                for (int i = 1; i < periods.Count; i++)
                {
                    if (AreConsecutivePeriods(periods[i - 1], periods[i]))
                    {
                        result.InstructorBackToBackConflictPenalty += _weights.BackToBackConflict;

                        var maxDistance = CalculateMaxDistanceBetweenExams(
                            dayExams.First(a => a.Period == periods[i]).Rooms,
                            dayExams.First(a => a.Period == periods[i]).Rooms);

                        if (maxDistance > _weights.DistanceThreshold)
                        {
                            result.InstructorDistanceBackToBackConflictPenalty += _weights.DistanceBackToBackConflict;
                        }
                    }
                }
            }
        }
    }

    private void CalculatePeriodPenalties(EvaluationResult result)
    {
        foreach (var (exam, (period, _)) in _assignments.Where(a => a.Value.Period != null))
        {
            var periodPreference = exam.AvailablePeriods.FirstOrDefault(p => p.Period == period);

            if (periodPreference != null)
            {
                result.PeriodPreferencePenalty += periodPreference.Penalty * _weights.PeriodPenalty;
            }

            {
                result.PeriodPreferencePenalty += period.Penalty * _weights.PeriodPenalty;
            }
        }
    }

    private void CalculateRoomPenalties(EvaluationResult result)
    {
        var penalty = 0;
        foreach (var (exam, (period, rooms)) in _assignments)
        {
            foreach (var room in rooms)
            {
                var roomPreference = exam.AvailableRooms.FirstOrDefault(r => r.Room == room);
                if (roomPreference != null)
                {
                    penalty += roomPreference.Penalty * _weights.RoomPenalty;
                }

                var roomPeriodPreference = room.PeriodPreferences.FirstOrDefault(p => p.Period == period);
                if (roomPeriodPreference != null)
                {
                    penalty += roomPeriodPreference.Penalty * _weights.RoomPenalty;
                }

            }
        }
        result.RoomPreferencePenalty = penalty; 
    }

    private void CalculateDistributionPenalties(EvaluationResult result)
    {
        int penalty = 0;

        foreach (var constraint in _data.Constraints.Where(c => !c.IsHard))
        {
            var exams = constraint.Exams.Where(_assignments.ContainsKey).ToList();
            if (exams.Count < 2) continue;

            bool violated = false;
            switch (constraint.Type)
            {
                case ConstraintType.DifferentPeriod:
                    violated = exams.Select(e => e.Assignment.Period).Distinct().Count() != exams.Count;
                    break;

                case ConstraintType.SamePeriod:
                    var firstPeriod = exams[0].Assignment.Period;
                    violated = exams.Any(e => e.Assignment.Period != firstPeriod);
                    break;

                case ConstraintType.DifferentRoom:
                    var allRooms = exams.SelectMany(eid => _assignments[eid].Rooms).ToList();
                    violated = allRooms.Distinct().Count() != allRooms.Count;
                    break;

                case ConstraintType.SameRoom:
                    var firstRoom = exams[0].Assignment.Rooms.FirstOrDefault();
                    violated = exams.Any(eid => !exams[0].Assignment.Rooms.Contains(firstRoom));
                    break;

                case ConstraintType.Precedence:
                    var periods = exams.Select(eid => _assignments[eid].Period).ToList();
                    var sortedPeriods = periods.OrderBy(p => p.Id).ToList();
                    violated = !periods.SequenceEqual(sortedPeriods);
                    break;
            }

            if (violated)
            {
                penalty += constraint.Weight * _weights.DistributionPenalty;
            }
        }

        result.DistributionConstraintPenalty = penalty;
    }

    private void CalculateRoomSizePenalties(EvaluationResult result)
    {
        int penalty = 0;

        foreach (var (exam, (_, rooms)) in _assignments.Where(a => a.Value.Period != null))
        {
            int totalCapacity = 0;
            int studentCount = exam.Students.Count;
            foreach (var room in rooms)
            {
                totalCapacity += exam.AltSeating ? room.AltSize : room.Size;
            }

            // Penalize if total capacity is smaller than needed
            if (studentCount > totalCapacity)
            {
                penalty += (studentCount - totalCapacity) * _weights.RoomSize;
            }


        }

        result.RoomSizePenalty = penalty;
    }

    private void CalculateRotationPenalties(EvaluationResult result)
    {
        int penalty = 0;

        foreach (var (exam, (period, _)) in _assignments.Where(a => a.Value.Period != null))
        {
            if (exam.Average.HasValue)
            {
                penalty += period.Id * exam.Average.Value * _weights.RotationPenalty;
            }
        }

        result.RotationPenalty = penalty;
    }

    private void CalculateLargeExamPenalties(EvaluationResult result)
    {
        int penalty = 0;

        foreach (var (exam, (period, _)) in _assignments.Where(a => a.Value.Period != null))
        {
            if (
                exam.Students.Count > _weights.LargeExamThreshold && exam.Assignment.Period.Penalty > 0)
            {
                penalty += _weights.LargeExamPenalty * exam.Assignment.Period.Penalty;
            }
        }
        result.LargeExamPenalty = penalty;
    }


    private bool AreConsecutivePeriods(Period period1, Period period2)
    {
        // Same day and consecutive time slots
        return period1.Day == period2.Day &&
               Math.Abs(period1.Id - period2.Id) == 1;
    }

    private double CalculateMaxDistanceBetweenExams(List<Room> rooms1,
        List<Room> rooms2)
    {
        double maxDistance = 0;

        foreach (var room1 in rooms1)
        {
            foreach (var room2 in rooms2)
            {
                double distance = CalculateRoomDistance(room1, room2);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }
        }

        return maxDistance;
    }

    private double CalculateAverageRoomDistance(List<Room> rooms)
    {
        if (rooms.Count < 2) return 0;

        double totalDistance = 0;
        int pairCount = 0;

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                totalDistance += CalculateRoomDistance(rooms[i], rooms[j]);
                pairCount++;
            }
        }

        return pairCount > 0 ? totalDistance / pairCount : 0;
    }

    private double CalculateRoomDistance(Room room1, Room room2)
    {
        if (room1 == room2) return 0;

        try
        {
            var coords1 = room1.Coordinates.Split(',').Select(int.Parse).ToArray();
            var coords2 = room2.Coordinates.Split(',').Select(int.Parse).ToArray();

            double dx = coords2[0] - coords1[0];
            double dy = coords2[1] - coords1[1];

            return 10 * Math.Sqrt(dx * dx + dy * dy); // 10 * Euclidean distance
        }
        catch
        {
            return double.PositiveInfinity;
        }
    }
}