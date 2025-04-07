using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SapLichThiITCCore
{
    public class DatasetKaggle
    {
        public class Student
        {
            public Guid StudentId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string Address { get; set; }
            public string ProgramName { get; set; }
            public string Year { get; set; }
        }

        public class Timeslot
        {
            public int TimeslotId { get; set; }
            public string Day { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
        }

        public class Instructor
        {
            public Guid InstructorId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string Department { get; set; }
        }

        public class Course
        {
            public int CourseId { get; set; }
            public string CourseName { get; set; }
            public string Department { get; set; }
            public int Credits { get; set; }
            public string Description { get; set; }
        }

        public class Room
        {
            public int ClassroomId { get; set; }
            public string BuildingName { get; set; }
            public string RoomNumber { get; set; }
            public int Capacity { get; set; }
            public string RoomType { get; set; }
        }

        

    }
}
