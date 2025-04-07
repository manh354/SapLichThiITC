using SapLichThiITCAlgo;
using SapLichThiITCCore;
using static SapLichThiITCCore.DatasetKaggle;

namespace SapLichThiAlgoKaggle
{
    public class BoxGeneratorKaggle
    {
        public required TimetablingData I_data;
        public required Dictionary<
              , HashSet<Course>> I_exam_linkages;
        public Lake O_lake;
        public BoxGeneratorKaggle Initialize()
        {
            return this;
        }
        public BoxGeneratorKaggle Run()
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
