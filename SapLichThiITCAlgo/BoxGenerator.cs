using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetExam;

namespace SapLichThiITCAlgo
{
    public class BoxGenerator
    {
        public required TimetablingData I_data;
        public required Dictionary<Exam, HashSet<Exam>> I_exam_linkages;
        public Lake O_lake;
        public BoxGenerator Initialize()
        {
            return this;
        }
        public BoxGenerator Run()
        {
            var ponds = I_data.Periods
                .Select(x => new Pond()
                {
                    Exams = new(),
                    Period = x,
                    Puddles = I_data.Rooms
                    .Select(y => new Puddle()
                    {
                        Exams = new(),
                        Room = y
                    })
                    .ToList()
                })
                .ToList();
            O_lake = new Lake() { Linkages = I_exam_linkages, Ponds = ponds };

            return this;
        }
    }
}
