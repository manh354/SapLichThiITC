using SapLichThiITCCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static SapLichThiITCCore.DatasetXml;

namespace SapLichThiITCInputHelper
{
    public class DatasetInputXml
    {
        public string I_filePath = string.Empty;
        public DatasetInputXml(string filePath)
        {
            I_filePath = filePath;
        }

        public DatasetInputXml _depricatedRun()
        {
            XDocument doc = XDocument.Load(I_filePath);

            // Read parameters
            var parameters = doc.Descendants("property")
                .Select(p => new
                {
                    Name = p.Attribute("name")?.Value,
                    Value = p.Attribute("value")?.Value
                }).ToList();

            Console.WriteLine("Parameters:");
            foreach (var param in parameters)
            {
                Console.WriteLine($"{param.Name}: {param.Value}");
            }

            // Read periods
            var periods = doc.Descendants("periods").Elements("period")
                .Select(p => new
                {
                    Id = p.Attribute("id")?.Value,
                    Length = p.Attribute("length")?.Value,
                    Day = p.Attribute("day")?.Value,
                    Time = p.Attribute("time")?.Value,
                    Penalty = p.Attribute("penalty")?.Value
                }).ToList();

            Console.WriteLine("\nPeriods:");
            foreach (var period in periods)
            {
                Console.WriteLine($"ID: {period.Id}, Day: {period.Day}, Time: {period.Time}, Penalty: {period.Penalty}");
            }

            // Read rooms
            var rooms = doc.Descendants("room")
                .Select(r => new
                {
                    Id = r.Attribute("id")?.Value,
                    Size = r.Attribute("size")?.Value,
                    Alt = r.Attribute("alt")?.Value,
                    Coordinates = r.Attribute("coordinates")?.Value,
                    Periods = r.Elements("period").Select(p => new
                    {
                        PeriodId = p.Attribute("id")?.Value,
                        Penalty = p.Attribute("penalty")?.Value,
                        Available = p.Attribute("available")?.Value
                    }).ToList()
                }).ToList();

            Console.WriteLine("\nRooms:");
            foreach (var room in rooms)
            {
                Console.WriteLine($"Room ID: {room.Id}, Size: {room.Size}, Alt: {room.Alt}, Coordinates: {room.Coordinates}");
                foreach (var period in room.Periods)
                {
                    Console.WriteLine($"  Period ID: {period.PeriodId}, Penalty: {period.Penalty}, Available: {period.Available}");
                }
            }

            // Read exams
            var exams = doc.Descendants("exam")
                .Select(e => new
                {
                    Id = e.Attribute("id")?.Value,
                    Length = e.Attribute("length")?.Value,
                    Alt = e.Attribute("alt")?.Value,
                    PrintOffset = e.Attribute("printOffset")?.Value,
                    Average = e.Attribute("average")?.Value,
                    Periods = e.Elements("period").Select(p => p.Attribute("id")?.Value).ToList(),
                    Rooms = e.Elements("room").Select(r => new
                    {
                        RoomId = r.Attribute("id")?.Value,
                        Penalty = r.Attribute("penalty")?.Value
                    }).ToList()
                }).ToList();

            Console.WriteLine("\nExams:");
            foreach (var exam in exams)
            {
                Console.WriteLine($"Exam ID: {exam.Id}, Length: {exam.Length}, Alt: {exam.Alt}, PrintOffset: {exam.PrintOffset}, Average: {exam.Average}");
                Console.WriteLine("  Periods: " + string.Join(", ", exam.Periods));
                Console.WriteLine("  Rooms:");
                foreach (var room in exam.Rooms)
                {
                    Console.WriteLine($"    Room ID: {room.RoomId}, Penalty: {room.Penalty}");
                }
            }

            return this;
        }

        public DatasetInputXml Run()
        {
            XDocument doc = XDocument.Load(I_filePath);

            // Parse parameters
            List<Parameter> parameters = doc.Root.Element("parameters")?
                .Elements("property")
                .Select(p => new Parameter
                {
                    Name = (string)p.Attribute("name"),
                    Value = (string)p.Attribute("value")
                }).ToList() ?? new List<Parameter>();

            // Parse periods
            List<Period> periods = doc.Root.Element("periods")?
                .Elements("period")
                .Select(p => new Period
                {
                    Id = (int)p.Attribute("id"),
                    Length = (int)p.Attribute("length"),
                    Day = (string)p.Attribute("day"),
                    Time = (string)p.Attribute("time"),
                    Penalty = (int)p.Attribute("penalty")
                }).ToList() ?? new List<Period>();

            // Parse rooms
            List<Room> rooms = doc.Root.Element("rooms")?
                .Elements("room")
                .Select(r => new Room
                {
                    Id = (int)r.Attribute("id"),
                    Size = (int)r.Attribute("size"),
                    Alt = (string)r.Attribute("alt"),
                    Coordinates = (string)r.Attribute("coordinates"),
                    Periods = r.Elements("period")
                        .Select(rp => new RoomPeriod
                        {
                            PeriodId = (int)rp.Attribute("id"),
                            Penalty = rp.Attribute("penalty") != null ? (int?)rp.Attribute("penalty") : null,
                            Available = rp.Attribute("available") != null ? (bool?)XmlConvert.ToBoolean(rp.Attribute("available").Value) : null
                        }).ToList()
                }).ToList() ?? new List<Room>();

            // Parse exams
            List<Exam> exams = doc.Root.Element("exams")
                .Elements("exam")
                .Select(e => new Exam
                {
                    Id = (int)e.Attribute("id"),
                    Length = (int)e.Attribute("length"),
                    Alt = (string)e.Attribute("alt"),
                    PrintOffset = (int)e.Attribute("printOffset"),
                    Average = (int)e.Attribute("average"),
                    PeriodIds = e.Elements("period").Select(p => (int)p.Attribute("id")).ToList(),
                    RoomAssignments = e.Elements("room")
                        .Select(ra => new RoomAssignment
                        {
                            RoomId = (int)ra.Attribute("id"),
                            Penalty = ra.Attribute("penalty") != null ? (int?)ra.Attribute("penalty") : null
                        }).ToList()
                }).ToList();

            // Parse students
            List<Student> students = doc.Root.Element("students")
                .Elements("student")
                .Select(s => new Student
                {
                    Id = (int)s.Attribute("id"),
                    ExamIds = s.Elements("exam").Select(e => (int)e.Attribute("id")).ToList()
                }).ToList();

            // Parse constraints
            List<Constraint> constraints = doc.Root.Element("constraints")?
                .Elements("different-period")
                .Select(c => new Constraint
                {
                    Id = (int)c.Attribute("id"),
                    ExamIds = c.Elements("exam").Select(e => (int)e.Attribute("id")).ToList()
                }).ToList() ?? new List<Constraint>();

            // Use the parsed data as needed

            return this;
        }


    }
}
