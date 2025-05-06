using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiAlgoKaggle
{
    public class ValidatorColor
    {
        public required Dictionary<int, HashSet<Exam>> I_color_exams { get; set; }
        public ValidatorColor Initialize()
        {

            return this;
        }
        public ValidatorColor Run()
        {
            foreach (var (color, exams) in I_color_exams)
            {
                foreach (var exam1 in exams)
                {
                    foreach (var exam2 in exams)
                    {
                        if (exam1 != exam2 && exam1.StudentIds.Any(exam2.StudentIds.Contains))
                            Console.WriteLine("SAME STUDENT IN COLOR");
                    }
                }
            }
            return this;
        }
    }
}
