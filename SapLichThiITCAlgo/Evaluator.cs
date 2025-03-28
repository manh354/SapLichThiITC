using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiITCAlgo
{
    public class Solution
    {
        public Dictionary<int, int> ExamPeriodAssignments { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int> ExamRoomAssignments { get; set; } = new Dictionary<int, int>();
        public Solution(Lake lake)
        {
            ExamPeriodAssignments = new Dictionary<int, int>();
            ExamRoomAssignments = new Dictionary<int, int>();

            foreach (var pond in lake.Ponds)
            {
                foreach (var puddle in pond.Puddles)
                {
                    foreach (var exam in puddle.Exams)
                    {
                        ExamPeriodAssignments.Add(exam.Id, pond.Period.Id);
                        ExamRoomAssignments.Add(exam.Id, puddle.Room.Id);
                    }
                }
            }
        }
        public static Solution FromLake(Lake lake)
        {
            Solution solution = new Solution(lake);
            return solution;
        }
    }

    public class Evaluator
    {
        private TimetablingData _data;
        private Solution _solution;

        private int _distanceToFeasibility;
        private int _softPenalty;

        public int DistanceToFeasibility => _distanceToFeasibility;
        public int SoftPenalty => _softPenalty;

        private Dictionary<int, List<int>> P_studentExams = new();

        public Evaluator(TimetablingData data, Solution solution)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _solution = solution ?? throw new ArgumentNullException(nameof(solution));
        }

        public Evaluator(TimetablingData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public Evaluator Evaluate(Solution solution)
        {
            P_studentExams = BuildStudentExams();
            _solution = solution;
            _distanceToFeasibility = CalculateDistanceToFeasibility();
            _softPenalty = CalculateSoftPenalty();
            return this;
        }

        public int CalculateDistanceToFeasibility()
        {
            int conflicts = CalculateConflicts();
            int roomOccupancy = CalculateRoomOccupancy();
            int periodUtilization = CalculatePeriodUtilization();
            int periodRelated = CalculatePeriodRelatedViolations();
            int roomRelated = CalculateRoomRelatedViolations();

            return conflicts + roomOccupancy + periodUtilization + periodRelated + roomRelated;
        }

        public int CalculateSoftPenalty()
        {
            int twoInARow = CalculateTwoInARow();
            int twoInADay = CalculateTwoInADay();
            int periodSpread = CalculatePeriodSpread();
            int mixedDurations = CalculateMixedDurations();
            int frontLoad = CalculateFrontLoad();
            int periodPenalty = CalculatePeriodPenalty();
            int roomPenalty = CalculateRoomPenalty();

            int w2R = GetWeight("TWOINAROW");
            int w2D = GetWeight("TWOINADAY");
            int wPS = GetWeight("PERIODSPREAD");
            int wNMD = GetWeight("NONMIXEDDURATIONS");
            int wFL = GetWeight("FRONTLOAD");

            return twoInARow * w2R +
                   twoInADay * w2D +
                   periodSpread * wPS +
                   mixedDurations * wNMD +
                   frontLoad * wFL +
                   periodPenalty +
                   roomPenalty;
        }

        private int GetWeight(string constraintType)
        {
            var weighting = _data.InstitutionalWeightings.FirstOrDefault(iw => iw.Type == constraintType);
            return weighting?.Weight ?? 0;
        }

        private int CalculateConflicts()
        {
            Dictionary<int, List<int>> studentExams = new Dictionary<int, List<int>>();
            foreach (var exam in _data.Exams)
            {
                foreach (var studentId in exam.StudentIds)
                {
                    if (!studentExams.ContainsKey(studentId))
                        studentExams[studentId] = new List<int>();
                    studentExams[studentId].Add(exam.Id);
                }
            }

            int conflictCount = 0;
            foreach (var student in studentExams)
            {
                var exams = student.Value;
                for (int i = 0; i < exams.Count; i++)
                {
                    for (int j = i + 1; j < exams.Count; j++)
                    {
                        int exam1 = exams[i];
                        int exam2 = exams[j];
                        if (_solution.ExamPeriodAssignments.TryGetValue(exam1, out int p1) &&
                            _solution.ExamPeriodAssignments.TryGetValue(exam2, out int p2) &&
                            p1 == p2)
                        {
                            conflictCount++;
                        }
                    }
                }
            }
            return conflictCount;
        }

        private int CalculateRoomOccupancy()
        {
            var roomPeriodCounts = new Dictionary<(int roomId, int periodId), int>();
            foreach (var exam in _data.Exams)
            {
                if (_solution.ExamRoomAssignments.TryGetValue(exam.Id, out int roomId) &&
                    _solution.ExamPeriodAssignments.TryGetValue(exam.Id, out int periodId))
                {
                    var key = (roomId, periodId);
                    if (!roomPeriodCounts.ContainsKey(key))
                        roomPeriodCounts[key] = 0;
                    roomPeriodCounts[key] += exam.StudentIds.Count;
                }
            }

            int violations = 0;
            foreach (var entry in roomPeriodCounts)
            {
                var room = _data.Rooms.FirstOrDefault(r => r.Id == entry.Key.roomId);
                if (room != null && entry.Value > room.Capacity)
                    violations++;
            }
            return violations;
        }

        private int CalculatePeriodUtilization()
        {
            int violations = 0;
            foreach (var exam in _data.Exams)
            {
                if (_solution.ExamPeriodAssignments.TryGetValue(exam.Id, out int periodId))
                {
                    var period = _data.Periods.First(p => p.Id == periodId);
                    if (exam.Duration > period.DurationMinutes)
                        violations++;
                }
            }
            return violations;
        }

        private int CalculatePeriodRelatedViolations()
        {
            int violations = 0;
            foreach (var constraint in _data.PeriodHardConstraints)
            {
                if (!_solution.ExamPeriodAssignments.TryGetValue(constraint.ExamId1, out int p1) ||
                    !_solution.ExamPeriodAssignments.TryGetValue(constraint.ExamId2, out int p2))
                    continue;

                var period1 = _data.Periods.First(p => p.Id == p1);
                var period2 = _data.Periods.First(p => p.Id == p2);

                switch (constraint.Type)
                {
                    case "AFTER":
                        if (period1.Date < period2.Date || (period1.Date == period2.Date && period1.StartTime <= period2.StartTime))
                            violations++;
                        break;
                    case "EXAM_COINCIDENCE":
                        if (p1 != p2)
                            violations++;
                        break;
                    case "EXCLUSION":
                        if (p1 == p2)
                            violations++;
                        break;
                }
            }
            return violations;
        }

        private int CalculateRoomRelatedViolations()
        {
            int violations = 0;
            foreach (var constraint in _data.RoomHardConstraints)
            {
                if (!_solution.ExamRoomAssignments.TryGetValue(constraint.ExamId, out int roomId) ||
                    !_solution.ExamPeriodAssignments.TryGetValue(constraint.ExamId, out int periodId))
                    continue;

                foreach (var exam in _data.Exams.Where(e => e.Id != constraint.ExamId))
                {
                    if (_solution.ExamRoomAssignments.TryGetValue(exam.Id, out int otherRoomId) &&
                        _solution.ExamPeriodAssignments.TryGetValue(exam.Id, out int otherPeriodId) &&
                        otherRoomId == roomId && otherPeriodId == periodId)
                    {
                        violations++;
                    }
                }
            }
            return violations;
        }

        private int CalculateTwoInARow()
        {
            var studentExams = BuildStudentExams();
            int count = 0;
            foreach (var student in studentExams)
            {
                var sorted = student.Value
                    .Select(e => new { ExamId = e, Period = _data.Periods.First(p => p.Id == _solution.ExamPeriodAssignments[e]) })
                    .OrderBy(e => e.Period.Date)
                    .ThenBy(e => e.Period.StartTime)
                    .ToList();

                for (int i = 0; i < sorted.Count - 1; i++)
                {
                    var current = sorted[i].Period;
                    var next = sorted[i + 1].Period;
                    if (current.Date == next.Date && AreConsecutive(current, next))
                        count++;
                }
            }
            return count;
        }

        private bool AreConsecutive(Period a, Period b)
        {
            DateTime aEnd = a.Date.Add(a.StartTime).AddMinutes(a.DurationMinutes);
            DateTime bStart = b.Date.Add(b.StartTime);
            return aEnd == bStart;
        }

        private int CalculateTwoInADay()
        {
            var studentExams = P_studentExams;
            int count = 0;
            foreach (var student in studentExams)
            {
                var exams = student.Value
                    .Select(e => new { ExamId = e, Period = _data.Periods.First(p => p.Id == _solution.ExamPeriodAssignments[e]) })
                    .GroupBy(e => e.Period.Date)
                    .Where(g => g.Count() >= 2)
                    .SelectMany(g => g.ToList());

                var examList = exams.ToList();
                for (int i = 0; i < examList.Count; i++)
                {
                    for (int j = i + 1; j < examList.Count; j++)
                    {
                        if (!AreConsecutive(examList[i].Period, examList[j].Period))
                            count++;
                    }
                }
            }
            return count;
        }

        private int CalculatePeriodSpread()
        {
            int spread = GetPeriodSpreadParameter();
            var studentExams = P_studentExams;
            int count = 0;

            foreach (var student in studentExams)
            {
                var periods = student.Value
                    .Select(e => _data.Periods.FindIndex(p => p.Id == _solution.ExamPeriodAssignments[e]))
                    .OrderBy(idx => idx)
                    .ToList();

                for (int i = 0; i < periods.Count; i++)
                {
                    for (int j = i + 1; j < periods.Count; j++)
                    {
                        if (periods[j] - periods[i] <= spread)
                            count++;
                    }
                }
            }
            return count;
        }

        private int CalculateMixedDurations()
        {
            var roomPeriodDurations = new Dictionary<(int roomId, int periodId), HashSet<int>>();
            foreach (var exam in _data.Exams)
            {
                if (_solution.ExamRoomAssignments.TryGetValue(exam.Id, out int roomId) &&
                    _solution.ExamPeriodAssignments.TryGetValue(exam.Id, out int periodId))
                {
                    var key = (roomId, periodId);
                    if (!roomPeriodDurations.ContainsKey(key))
                        roomPeriodDurations[key] = new HashSet<int>();
                    roomPeriodDurations[key].Add(exam.Duration);
                }
            }
            return roomPeriodDurations.Count(kv => kv.Value.Count > 1);
        }

        private int CalculateFrontLoad()
        {
            var fl = _data.InstitutionalWeightings.FirstOrDefault(iw => iw.Type == "FRONTLOAD");
            if (fl == null || fl.Parameters.Count < 2) return 0;

            int numExams = fl.Parameters[0];
            int lastPeriodsCount = fl.Parameters[1];

            var frontLoadExams = _data.Exams
                .OrderByDescending(e => e.StudentIds.Count)
                .ThenBy(e => e.Id)
                .Take(numExams)
                .Select(e => e.Id)
                .ToList();

            var lastPeriods = _data.Periods
                .OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.StartTime)
                .Take(lastPeriodsCount)
                .Select(p => p.Id)
                .ToHashSet();

            return frontLoadExams.Count(e => _solution.ExamPeriodAssignments.TryGetValue(e, out int p) && lastPeriods.Contains(p));
        }

        private int CalculatePeriodPenalty()
        {
            return _data.Periods.Sum(p => _solution.ExamPeriodAssignments.Count(e => e.Value == p.Id) * p.Penalty);
        }

        private int CalculateRoomPenalty()
        {
            return _data.Rooms.Sum(r => _solution.ExamRoomAssignments.Count(e => e.Value == r.Id) * r.Penalty);
        }

        private Dictionary<int, List<int>> BuildStudentExams()
        {
            var studentExams = new Dictionary<int, List<int>>();
            foreach (var exam in _data.Exams)
            {
                foreach (var studentId in exam.StudentIds)
                {
                    if (!studentExams.ContainsKey(studentId))
                        studentExams[studentId] = new List<int>();
                    studentExams[studentId].Add(exam.Id);
                }
            }
            return studentExams;
        }

        private int GetPeriodSpreadParameter()
        {
            var ps = _data.InstitutionalWeightings.FirstOrDefault(iw => iw.Type == "PERIODSPREAD");
            return ps?.Parameters.FirstOrDefault() ?? 0;
        }
    }
}
