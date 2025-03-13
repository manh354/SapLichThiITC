using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SapLichThiITCCore
{
    public class DatasetXml
    {
        public class Parameter
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class Period
        {
            public int Id { get; set; }
            public int Length { get; set; }
            public string Day { get; set; }
            public string Time { get; set; }
            public int Penalty { get; set; }
        }

        public class RoomPeriod
        {
            public int PeriodId { get; set; }
            public int? Penalty { get; set; }
            public bool? Available { get; set; }
        }

        public class Room
        {
            public int Id { get; set; }
            public int Size { get; set; }
            public string Alt { get; set; }
            public string Coordinates { get; set; }
            public List<RoomPeriod> Periods { get; set; } = new List<RoomPeriod>();
        }

        public class RoomAssignment
        {
            public int RoomId { get; set; }
            public int? Penalty { get; set; }
        }

        public class Exam
        {
            public int Id { get; set; }
            public int Length { get; set; }
            public string Alt { get; set; }
            public int PrintOffset { get; set; }
            public int Average { get; set; }
            public List<int> PeriodIds { get; set; } = new List<int>();
            public List<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();
        }

        public class Student
        {
            public int Id { get; set; }
            public List<int> ExamIds { get; set; } = new List<int>();
        }

        public class Constraint
        {
            public int Id { get; set; }
            public List<int> ExamIds { get; set; } = new List<int>();
        }
    }
}
