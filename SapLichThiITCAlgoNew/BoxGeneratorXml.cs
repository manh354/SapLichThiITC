using SapLichThiITCCore;
using SapLichThiITCInputHelper;
using static SapLichThiITCCore.DatasetXml;

namespace SapLichThiITCAlgoNew
{
    public class BoxGeneratorXml
    {
        public required ExamTimetablingData I_data;
        public required Dictionary<Exam, HashSet<Exam>> I_exam_linkages;
        public Lake O_lake;
        public BoxGeneratorXml Initialize()
        {
            return this;
        }
        public BoxGeneratorXml Run()
        {
            var ponds = I_data.Periods
                .Select(p => new Pond()
                {
                    Penalty = p.Penalty,
                    Exams = new(),
                    Period = p,
                    Puddles = I_data.Rooms
                    .Select(y => new Puddle()
                    {
                        Penalty = y.PeriodPreferences.FirstOrDefault(pp => pp.Period == p)?.Penalty ?? 0,
                        Available = y.PeriodPreferences.FirstOrDefault(pp => pp.Period == p)?.Available ?? true,
                        Exam = null,
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
