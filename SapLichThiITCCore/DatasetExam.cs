using System.Globalization;
using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiITCCore
{

    public class TimetablingData
    {
        public List<Exam> Exams { get; set; } = new List<Exam>();
        public List<Period> Periods { get; set; } = new List<Period>();
        public List<Room> Rooms { get; set; } = new List<Room>();
        public List<RoomHardConstraint> RoomHardConstraints { get; set; } = new List<RoomHardConstraint>();
        public List<PeriodHardConstraint> PeriodHardConstraints { get; set; } = new List<PeriodHardConstraint>();
        public List<InstitutionalWeighting> InstitutionalWeightings { get; set; } = new List<InstitutionalWeighting>();
    }
    public class TimetablingDataReader
    {
        public TimetablingData Read(string filePath)
        {
            var data = new TimetablingData();
            using (var reader = new StreamReader(filePath))
            {
                string currentSection = null;
                int examId = 0;
                int periodId = 0;
                int roomId = 0;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line.StartsWith("["))
                    {
                        currentSection = ParseSectionHeader(line);
                        continue;
                    }

                    switch (currentSection)
                    {
                        case "Exams":
                            ParseExamLine(line, examId++, data.Exams);
                            break;
                        case "Periods":
                            data.Periods.Add(ParsePeriodLine(line, periodId++));
                            break;
                        case "Rooms":
                            data.Rooms.Add(ParseRoomLine(line, roomId++));
                            break;
                        case "PeriodHardConstraints":
                            data.PeriodHardConstraints.Add(ParsePeriodHardConstraintLine(line));
                            break;
                        case "RoomHardConstraints":
                            data.RoomHardConstraints.Add(ParseRoomHardConstraintLine(line));
                            break;
                        case "InstitutionalWeightings":
                            data.InstitutionalWeightings.Add(ParseInstitutionalWeightingLine(line));
                            break;
                    }
                }
            }
            return data;
        }

        private string ParseSectionHeader(string line)
        {
            return line.Split(new[] { '[', ']', ':' }, StringSplitOptions.RemoveEmptyEntries)[0];
        }

        private void ParseExamLine(string line, int examId, List<Exam> exams)
        {
            var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => int.Parse(p.Trim())).ToList();
            exams.Add(new Exam
            {
                Id = examId,
                Duration = parts[0],
                StudentIds = parts.Skip(1).ToList()
            });
        }

        private Period ParsePeriodLine(string line, int periodId)
        {
            var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()).ToList();
            return new Period
            {
                Id = periodId,
                Date = DateTime.ParseExact(parts[0], "dd:MM:yyyy", CultureInfo.InvariantCulture),
                StartTime = TimeSpan.Parse(parts[1]),
                DurationMinutes = int.Parse(parts[2]),
                Penalty = int.Parse(parts[3])
            };
        }

        private Room ParseRoomLine(string line, int roomId)
        {
            var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => int.Parse(p.Trim())).ToList();
            return new Room
            {
                Id = roomId,
                Capacity = parts[0],
                Penalty = parts.Count > 1 ? parts[1] : 0
            };
        }

        private RoomHardConstraint ParseRoomHardConstraintLine(string line)
        {
            var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()).ToList();
            return new RoomHardConstraint
            {
                ExamId = int.Parse(parts[0]),
                Type = parts[1],
            };
        }

        private PeriodHardConstraint ParsePeriodHardConstraintLine(string line)
        {
            var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()).ToList();
            return new PeriodHardConstraint
            {
                ExamId1 = int.Parse(parts[0]),
                Type = parts[1],
                ExamId2 = parts.Count > 2 ? int.Parse(parts[2]) : 0
            };
        }

        private InstitutionalWeighting ParseInstitutionalWeightingLine(string line)
        {
            var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()).ToList();
            var weighting = new InstitutionalWeighting
            {
                Type = parts[0]
            };
            if (parts.Count > 1)
            {
                weighting.Weight = int.Parse(parts.Last());
                weighting.Parameters = parts.Skip(1).Take(parts.Count - 2).Select(int.Parse).ToList();
            }
            return weighting;
        }
    }

    public class DatasetExam
    {

        public class Exam
        {
            public int Id { get; set; }
            public int Duration { get; set; }
            public List<int> StudentIds { get; set; } = new List<int>();
            public override string ToString()
            {
                return $"id: {Id}, dur: {Duration}, studentCount: {StudentIds.Count}";
            }
        }

        public record Period
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan StartTime { get; set; }
            public int DurationMinutes { get; set; }
            public int Penalty { get; set; }

        }

        public class Room
        {
            public int Id { get; set; }
            public int Capacity { get; set; }
            public int Penalty { get; set; }
        }

        public class RoomHardConstraint
        {
            public int ExamId { get; set; }
            public string Type { get; set; } // e.g., "EXCLUSIVE"
        }

        public class PeriodHardConstraint
        {
            public int ExamId1 { get; set; }
            public string Type { get; set; }
            public int ExamId2 { get; set; }
        }

        public class InstitutionalWeighting
        {
            public string Type { get; set; }
            public List<int> Parameters { get; set; } = new List<int>();
            public int Weight { get; set; }
        }


    }
}
