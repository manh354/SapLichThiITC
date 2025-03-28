using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;

namespace SapLichThiITCCore
{
    public class DatasetXmlRaw
    {
        public class Period
        {
            public int Id { get; set; }
            public int Length { get; set; }
            public string Day { get; set; }
            public string Time { get; set; }
            public int Penalty { get; set; }
        }

        public class RoomPeriodPreference
        {
            public int PeriodId { get; set; }
            public bool Available { get; set; } = true;
            public int Penalty { get; set; }
        }

        public class Room
        {
            public int Id { get; set; }
            public int Size { get; set; }
            public int AltSize { get; set; }
            public string Coordinates { get; set; }
            public List<RoomPeriodPreference> PeriodPreferences { get; set; } = new List<RoomPeriodPreference>();
        }

        public class ExamPeriod
        {
            public int PeriodId { get; set; }
            public int Penalty { get; set; }
        }

        public class ExamRoom
        {
            public int RoomId { get; set; }
            public int Penalty { get; set; }
        }

        public class Assignment
        {
            public int PeriodId { get; set; }
            public List<int> RoomIds { get; set; } = new List<int>();
        }

        public class Exam
        {
            public int Id { get; set; }
            public int Length { get; set; }
            public bool AltSeating { get; set; }
            public int MinSize { get; set; }
            public int MaxRooms { get; set; } = 4;
            public int? Average { get; set; }
            public List<ExamPeriod> AvailablePeriods { get; set; } = new List<ExamPeriod>();
            public List<ExamRoom> AvailableRooms { get; set; } = new List<ExamRoom>();
            public Assignment Assignment { get; set; }
        }

        public class StudentPeriodAvailability
        {
            public int PeriodId { get; set; }
            public bool Available { get; set; }
        }

        public class Student
        {
            public int Id { get; set; }
            public List<int> ExamIds { get; set; } = new List<int>();
            public List<StudentPeriodAvailability> PeriodAvailabilities { get; set; } = new List<StudentPeriodAvailability>();
        }

        public class InstructorPeriodAvailability
        {
            public int PeriodId { get; set; }
            public bool Available { get; set; }
        }

        public class Instructor
        {
            public int Id { get; set; }
            public List<int> ExamIds { get; set; } = new List<int>();
            public List<InstructorPeriodAvailability> PeriodAvailabilities { get; set; } = new List<InstructorPeriodAvailability>();
        }

        public enum ConstraintType
        {
            DifferentPeriod,
            SameRoom,
            DifferentRoom,
            SamePeriod,
            Precedence
        }

        public class DistributionConstraint
        {
            public int Id { get; set; }
            public ConstraintType Type { get; set; }
            public bool IsHard { get; set; } = true;
            public int Weight { get; set; }
            public List<int> ExamIds { get; set; } = new List<int>();
        }

        public class ExamTimetablingData
        {
            public string Campus { get; set; }
            public string Term { get; set; }
            public string Year { get; set; }
            public string Created { get; set; }
            public List<Period> Periods { get; set; } = new List<Period>();
            public List<Room> Rooms { get; set; } = new List<Room>();
            public List<Exam> Exams { get; set; } = new List<Exam>();
            public List<Student> Students { get; set; } = new List<Student>();
            public List<Instructor> Instructors { get; set; } = new List<Instructor>();
            public List<DistributionConstraint> Constraints { get; set; } = new List<DistributionConstraint>();

            public static ExamTimetablingData Parse(string xmlFilePath)
            {
                var doc = XDocument.Load(xmlFilePath);
                var root = doc.Root;

                
                var data = new ExamTimetablingData
                {
                    Campus = (string)root.Attribute("campus"),
                    Term = (string)root.Attribute("term"),
                    Year = (string)root.Attribute("year"),
                    Created = (string)root.Attribute("created"),
                    Periods = ParsePeriods(root.Element("periods")),
                    Rooms = ParseRooms(root.Element("rooms")),
                    Exams = ParseExams(root.Element("exams")),
                    Students = ParseStudents(root.Element("students")),
                    Instructors = ParseInstructors(root.Element("instructors")),
                    Constraints = ParseConstraints(root.Element("constraints"))
                };

                return data;
            }

            private static List<Period> ParsePeriods(XElement periodsElement)
            {
                return periodsElement?.Elements("period")
                    .Select(p => new Period
                    {
                        Id = (int)p.Attribute("id"),
                        Length = (int)p.Attribute("length"),
                        Day = (string)p.Attribute("day"),
                        Time = (string)p.Attribute("time"),
                        Penalty = (int)p.Attribute("penalty")
                    }).ToList() ?? new List<Period>();
            }

            private static List<Room> ParseRooms(XElement roomsElement)
            {
                return roomsElement?.Elements("room").Select(r =>
                {
                    var room = new Room
                    {
                        Id = (int)r.Attribute("id"),
                        Size = (int)r.Attribute("size"),
                        AltSize = (int)r.Attribute("alt"),
                        Coordinates = (string)r.Attribute("coordinates")
                    };

                    room.PeriodPreferences = r.Elements("examPeriod")
                        .Select(p => new RoomPeriodPreference
                        {
                            PeriodId = (int)p.Attribute("id"),
                            Available = (bool?)p.Attribute("available") ?? true,
                            Penalty = (int?)p.Attribute("penalty") ?? 0
                        }).ToList();

                    return room;
                }).ToList() ?? new List<Room>();
            }

            private static List<Exam> ParseExams(XElement examsElement)
            {
                return examsElement?.Elements("exam").Select(e =>
                {
                    var exam = new Exam
                    {
                        Id = (int)e.Attribute("id"),
                        Length = (int)e.Attribute("length"),
                        AltSeating = (bool)e.Attribute("alt"),
                        MinSize = (int?)e.Attribute("minSize") ?? 0,
                        MaxRooms = (int?)e.Attribute("maxRooms") ?? 4,
                        Average = (int?)e.Attribute("average"),
                        AvailablePeriods = e.Elements("period")
                            .Select(p => new ExamPeriod
                            {
                                PeriodId = (int)p.Attribute("id"),
                                Penalty = (int?)p.Attribute("penalty") ?? 0
                            }).ToList(),
                        AvailableRooms = e.Elements("room")
                            .Select(r => new ExamRoom
                            {
                                RoomId = (int)r.Attribute("id"),
                                Penalty = (int?)r.Attribute("penalty") ?? 0
                            }).ToList()
                    };

                    var assignment = e.Element("assignment");
                    if (assignment != null)
                    {
                        exam.Assignment = new Assignment
                        {
                            PeriodId = (int)assignment.Element("examPeriod").Attribute("id"),
                            RoomIds = assignment.Elements("room").Select(r => (int)r.Attribute("id")).ToList()
                        };
                    }

                    return exam;
                }).ToList() ?? new List<Exam>();
            }

            private static List<Student> ParseStudents(XElement studentsElement)
            {
                return studentsElement?.Elements("student").Select(s => new Student
                {
                    Id = (int)s.Attribute("id"),
                    ExamIds = s.Elements("exam").Select(e => (int)e.Attribute("id")).ToList(),
                    PeriodAvailabilities = s.Elements("examPeriod")
                        .Select(p => new StudentPeriodAvailability
                        {
                            PeriodId = (int)p.Attribute("id"),
                            Available = (bool?)p.Attribute("available") ?? false
                        }).ToList()
                }).ToList() ?? new List<Student>();
            }

            private static List<Instructor> ParseInstructors(XElement instructorsElement)
            {
                return instructorsElement?.Elements("instructor").Select(i => new Instructor
                {
                    Id = (int)i.Attribute("id"),
                    ExamIds = i.Elements("exam").Select(e => (int)e.Attribute("id")).ToList(),
                    PeriodAvailabilities = i.Elements("examPeriod")
                        .Select(p => new InstructorPeriodAvailability
                        {
                            PeriodId = (int)p.Attribute("id"),
                            Available = (bool?)p.Attribute("available") ?? false
                        }).ToList()
                }).ToList() ?? new List<Instructor>();
            }

            private static List<DistributionConstraint> ParseConstraints(XElement constraintsElement)
            {
                return constraintsElement?.Elements().Select(c =>
                {
                    var constraint = new DistributionConstraint
                    {
                        Id = (int)c.Attribute("id"),
                        Type = GetConstraintType(c.Name.LocalName),
                        IsHard = (bool?)c.Attribute("hard") ?? true,
                        ExamIds = c.Elements("exam").Select(e => (int)e.Attribute("id")).ToList()
                    };

                    if (!constraint.IsHard)
                    {
                        constraint.Weight = (int)c.Attribute("weight");
                    }

                    return constraint;
                }).ToList() ?? new List<DistributionConstraint>();
            }

            private static ConstraintType GetConstraintType(string elementName)
            {
                return elementName.ToLower() switch
                {
                    "different-period" => ConstraintType.DifferentPeriod,
                    "same-period" => ConstraintType.SamePeriod,
                    "different-room" => ConstraintType.DifferentRoom,
                    "same-room" => ConstraintType.SameRoom,
                    "precedence" => ConstraintType.Precedence,
                    _ => throw new ArgumentException($"Unknown constraint type: {elementName}")
                };
            }
        }
    }

    public class DatasetXml
    {

        public class ExamTimetablingData
        {
            public string Campus { get; set; }
            public string Term { get; set; }
            public string Year { get; set; }
            public string Created { get; set; }
            public List<Period> Periods { get; set; } = new List<Period>();
            public List<Room> Rooms { get; set; } = new List<Room>();
            public List<Exam> Exams { get; set; } = new List<Exam>();
            public List<Student> Students { get; set; } = new List<Student>();
            public List<Instructor> Instructors { get; set; } = new List<Instructor>();
            public List<DistributionConstraint> Constraints { get; set; } = new List<DistributionConstraint>();
            
            public static ExamTimetablingData FromRawExamTimeTablingData(DatasetXmlRaw.ExamTimetablingData examTimetablingData)
            {
                var periods = examTimetablingData.Periods.Select(p => Period.FromRawPeriod(p)).ToList();
                var rooms = examTimetablingData.Rooms.Select(r => Room.FromRawRoom(r, periods)).ToList();
                var exams = examTimetablingData.Exams.Select(e => Exam.FromRawExam(e, periods, rooms)).ToList();
                var students = examTimetablingData.Students.Select(s => Student.FromRawStudent(s, periods, exams)).ToList();
                var instructors = examTimetablingData.Instructors.Select(i => Instructor.FromRawInstructor(i, periods, exams)).ToList();
                var constraints = examTimetablingData.Constraints.Select(c => DistributionConstraint.FromDistributionConstraint(c, exams)).ToList();
                return new ExamTimetablingData()
                {
                    Campus = examTimetablingData.Campus,
                    Term = examTimetablingData.Term,
                    Year = examTimetablingData.Year,
                    Created = examTimetablingData.Created,
                    Periods = periods,
                    Rooms = rooms,
                    Exams = exams,
                    Students = students,
                    Instructors = instructors,
                    Constraints = constraints,
                };
            }
        }

        public class Period
        {
            public int Id { get; set; }
            public int Length { get; set; }
            public string Day { get; set; }
            public string Time { get; set; }
            public int Penalty { get; set; }
            public static Period FromRawPeriod(DatasetXmlRaw.Period period)
            {
                return new Period
                {
                    Id = period.Id,
                    Length = period.Length,
                    Day = period.Day,
                    Time = period.Time,
                    Penalty = period.Penalty,
                };
            }
        }

        public class RoomPeriodPreference
        {
            public Period Period { get; set; }
            public bool Available { get; set; } = true;
            public int Penalty { get; set; }
            public static RoomPeriodPreference FromRawPeriodPreference(DatasetXmlRaw.RoomPeriodPreference roomPeriodPreference, List<Period> periods)
            {
                return new RoomPeriodPreference
                {
                    Period = periods.First(p => p.Id == roomPeriodPreference.PeriodId),
                    Available = roomPeriodPreference.Available,
                    Penalty = roomPeriodPreference.Penalty,
                };
            }
        }

        public class Room
        {
            public int Id { get; set; }
            public int Size { get; set; }
            public int AltSize { get; set; }
            public string Coordinates { get; set; }
            public List<RoomPeriodPreference> PeriodPreferences { get; set; } = new List<RoomPeriodPreference>();
            public static Room FromRawRoom(DatasetXmlRaw.Room room, List<Period> periods)
            {
                return new Room
                {
                    Id = room.Id,
                    Size = room.Size,
                    AltSize = room.AltSize,
                    Coordinates = room.Coordinates,
                    PeriodPreferences = room.PeriodPreferences.Select(pr =>
                    {
                        return RoomPeriodPreference.FromRawPeriodPreference(pr, periods);
                    }).ToList(),
                };
            }
        }
        public class ExamPeriod
        {
            public Period Period { get; set; }
            public int Penalty { get; set; }

            public static ExamPeriod FromRawPeriod(DatasetXmlRaw.ExamPeriod examPeriod, List<Period> periods)
            {
                return new ExamPeriod
                {
                    Period = periods.First(p => p.Id == examPeriod.PeriodId),
                    Penalty = examPeriod.Penalty,
                };
            }
        }

        public class ExamRoom
        {
            public Room Room { get; set; }
            public int Penalty { get; set; }
            public static ExamRoom FromRawExamRoom(DatasetXmlRaw.ExamRoom examRoom, List<Room> rooms)
            {
                return new ExamRoom
                {
                    Room = rooms.First(r => r.Id == examRoom.RoomId),
                    Penalty = examRoom.Penalty,
                };
            }
        }

        public class Assignment
        {
            public Period Period { get; set; }
            public List<Room> Rooms { get; set; } = new List<Room>();
            public static Assignment FromRawAssignment(DatasetXmlRaw.Assignment assignment, List<Period> periods, List<Room> rooms)
            {
                if(assignment == null) return new();
                return new Assignment
                {
                    Period = periods.First(p => p.Id == assignment.PeriodId),
                    Rooms = assignment.RoomIds.Select(
                        ri => rooms.First(r => r.Id == ri)
                    ).ToList()
                };
            }

            public void Assign(Period period, Room room)
            {
                Rooms = [room];
                Period = period;
            }

            public void Clear()
            {
                Rooms.Clear();
                Period = null;
            }
        }


        public class Exam
        {
            public int Id { get; set; }
            public int Length { get; set; }
            public bool AltSeating { get; set; }
            public int MinSize { get; set; }
            public int MaxRooms { get; set; } = 4;
            public int? Average { get; set; }
            public HashSet<ExamPeriod> AvailablePeriods { get; set; } = new ();
            public HashSet<ExamRoom> AvailableRooms { get; set; } = new ();
            public Assignment Assignment { get; set; } = new();
            public HashSet<Student> Students { get; set; } = new();
            public HashSet<Period> StudentUnavailablePeriods { get; set; } = new();
            public HashSet<Instructor> Instructors { get; set; } = new();
            public HashSet<Period> InstructorUnavailablePeriods { get; set; } = new();
            public HashSet<DistributionConstraint> Constraints { get; set; } = new();
            public override string ToString()
            {
                return $"id: {Id}, size: {Students.Count}, length: {Length}";

            }

            public void AddToExam(Student student)
            {
                Students.Add(student);
                StudentUnavailablePeriods.UnionWith(
                    student.PeriodAvailabilities
                    .Where(pa => pa.Available)
                    .Select(s => s.Period));
            }

            public void AddToExam(Instructor instructor)
            {
                Instructors.Add(instructor);
                InstructorUnavailablePeriods.UnionWith(
                    instructor.PeriodAvailabilities
                    .Where(pa => pa.Available)
                    .Select(s => s.Period));
            }
            public void AddToExam(DistributionConstraint constraint)
            {
                Constraints.Add(constraint);
            }
            public static Exam FromRawExam(DatasetXmlRaw.Exam rawExam, List<Period> periods, List<Room> rooms)
            {
                return new Exam()
                {
                    Id = rawExam.Id,
                    Length = rawExam.Length,
                    AltSeating = rawExam.AltSeating,
                    MinSize = rawExam.MinSize,
                    MaxRooms = rawExam.MaxRooms,
                    Average = rawExam.Average,
                    AvailablePeriods = rawExam.AvailablePeriods.Select(
                        ap => ExamPeriod.FromRawPeriod(ap, periods)
                        ).ToHashSet(),
                    AvailableRooms = rawExam.AvailableRooms.Select(
                        ar => ExamRoom.FromRawExamRoom(ar, rooms)
                        ).ToHashSet(),
                    Assignment = Assignment.FromRawAssignment(rawExam.Assignment, periods, rooms),
                };
            }
        }

        public class StudentPeriodAvailability
        {
            public Period Period { get; set; }
            public bool Available { get; set; }
            public static StudentPeriodAvailability FromRawStudentPeriodAvailability(DatasetXmlRaw.StudentPeriodAvailability studentPeriodAvailability, List<Period> periods)
            {
                return new StudentPeriodAvailability
                {
                    Period = periods.First(p => p.Id == studentPeriodAvailability.PeriodId),
                    Available = studentPeriodAvailability.Available,
                };
            }
        }

        public class Student
        {
            public int Id { get; set; }
            public List<Exam> Exams { get; set; } = new List<Exam>();
            public List<StudentPeriodAvailability> PeriodAvailabilities { get; set; } = new List<StudentPeriodAvailability>();

            public Student AddThisToClasses()
            {
                this.Exams.ForEach(e => e.AddToExam(this));
                return this;
            }
            public static Student FromRawStudent(DatasetXmlRaw.Student student, List<Period> periods, List<Exam> exams)
            {
                return new Student
                {
                    Id = student.Id,
                    Exams = student.ExamIds.Select(eid => exams.First(e => e.Id == eid)).ToList(),
                    PeriodAvailabilities = student.PeriodAvailabilities.Select(
                        pa => StudentPeriodAvailability.FromRawStudentPeriodAvailability(pa, periods)
                        ).ToList()
                }.AddThisToClasses();
            }
        }


        public class InstructorPeriodAvailability
        {
            public Period Period { get; set; }
            public bool Available { get; set; }
            public static InstructorPeriodAvailability FromRawInstructorPeriodAvailability(DatasetXmlRaw.InstructorPeriodAvailability instructorPeriodAvailability, List<Period> periods)
            {
                return new()
                {
                    Available = instructorPeriodAvailability.Available,
                    Period = periods.First(p => p.Id == instructorPeriodAvailability.PeriodId),
                };
            }
        }

        public class Instructor
        {
            public int Id { get; set; }
            public List<Exam> Exams { get; set; } = new List<Exam>();
            public List<InstructorPeriodAvailability> PeriodAvailabilities { get; set; } = new List<InstructorPeriodAvailability>();

            public Instructor AddThisToClasses()
            {
                this.Exams.ForEach(e => e.AddToExam(this));
                return this;
            }
            public static Instructor FromRawInstructor(DatasetXmlRaw.Instructor student, List<Period> periods, List<Exam> exams)
            {
                return new Instructor
                {
                    Id = student.Id,
                    Exams = student.ExamIds.Select(eid => exams.First(e => e.Id == eid)).ToList(),
                    PeriodAvailabilities = student.PeriodAvailabilities.Select(
                        pa => InstructorPeriodAvailability.FromRawInstructorPeriodAvailability(pa, periods)
                        ).ToList()
                }.AddThisToClasses();
            }

        }

        public enum ConstraintType
        {
            DifferentPeriod,
            SameRoom,
            DifferentRoom,
            SamePeriod,
            Precedence
        }

        public class DistributionConstraint
        {
            public int Id { get; set; }
            public ConstraintType Type { get; set; }
            public bool IsHard { get; set; } = true;
            public int Weight { get; set; }
            public List<Exam> Exams { get; set; } = new List<Exam>();

            public static DistributionConstraint FromDistributionConstraint(DatasetXmlRaw.DistributionConstraint distributionConstraint, List<Exam> exams)
            {
                return new DistributionConstraint()
                {
                    Id = distributionConstraint.Id,
                    Exams = distributionConstraint.ExamIds
                        .Select(eid => exams.First(e => e.Id == eid)).ToList(),
                    Weight = distributionConstraint.Weight,
                    IsHard = distributionConstraint.IsHard,
                    Type = (ConstraintType)distributionConstraint.Type
                }.AddToClasses();
            }

            public DistributionConstraint AddToClasses()
            {
                this.Exams.ForEach(e => e.AddToExam(this));
                return this;
            }
        }
    }
}