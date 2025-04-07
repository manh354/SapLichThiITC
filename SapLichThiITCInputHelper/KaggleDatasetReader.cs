using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SapLichThiITCCore.DatasetKaggle;

namespace SapLichThiITCInputHelper
{
    public sealed class StudentMap : ClassMap<Student>
    {
        public StudentMap()
        {
            Map(m => m.StudentId).Name("student_id");
            Map(m => m.FirstName).Name("first_name");
            Map(m => m.LastName).Name("last_name");
            Map(m => m.Email).Name("email");
            Map(m => m.PhoneNumber).Name("phone_number");
            Map(m => m.Address).Name("address");
            Map(m => m.ProgramName).Name("program_name");
            Map(m => m.Year).Name("year");
        }
    }

    // Similar mappings for other classes
    public sealed class TimeslotMap : ClassMap<Timeslot>
    {
        public TimeslotMap()
        {
            Map(m => m.TimeslotId).Name("timeslot_id");
            Map(m => m.Day).Name("day");
            Map(m => m.StartTime).Name("start_time");
            Map(m => m.EndTime).Name("end_time");
        }
    }

    public sealed class InstructorMap : ClassMap<Instructor>
    {
        public InstructorMap()
        {
            Map(m => m.InstructorId).Name("instructor_id");
            Map(m => m.FirstName).Name("first_name");
            Map(m => m.LastName).Name("last_name");
            Map(m => m.Email).Name("email");
            Map(m => m.PhoneNumber).Name("phone_number");
            Map(m => m.Department).Name("department");
        }
    }

    public sealed class CourseMap : ClassMap<Course>
    {
        public CourseMap()
        {
            Map(m => m.CourseId).Name("course_id");
            Map(m => m.CourseName).Name("course_name");
            Map(m => m.Department).Name("department");
            Map(m => m.Credits).Name("credits");
            Map(m => m.Description).Name("description")
                .Convert(args => args.Row.GetField("description")?.Replace("\n", " ")); // Flatten multi-line descriptions
        }
    }

    public sealed class ClassroomMap : ClassMap<Room>
    {
        public ClassroomMap()
        {
            Map(m => m.ClassroomId).Name("classroom_id");
            Map(m => m.BuildingName).Name("building_name");
            Map(m => m.RoomNumber).Name("room_number");
            Map(m => m.Capacity).Name("capacity");
            Map(m => m.RoomType).Name("room_type");
        }
    }


    public class InputHandler
    {
        public List<Student> ReadStudents(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<StudentMap>();
            return csv.GetRecords<Student>().ToList();
        }

        public List<Timeslot> ReadTimeslots(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<TimeslotMap>();
            return csv.GetRecords<Timeslot>().ToList();
        }

        public List<Instructor> ReadInstructors(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<InstructorMap>();
            return csv.GetRecords<Instructor>().ToList();
        }

        public List<Course> ReadCourses(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<CourseMap>();
            return csv.GetRecords<Course>().ToList();
        }

        public List<Room> ReadClassrooms(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<ClassroomMap>();
            return csv.GetRecords<Room>().ToList();
        }

    }


    public class KaggleDatasetReader
    {
         
    }
}
