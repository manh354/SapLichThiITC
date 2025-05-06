using SapLichThiITCAlgo;
using SapLichThiITCAlgoNew;
using SapLichThiITCCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SapLichThiITC
{
    public class PipelineXml
    {
        public SapLichThiITCAlgoNew.Lake Run(string filePathXml)
        {
            DatasetXml.ExamTimetablingData data =
                    DatasetXml.ExamTimetablingData
                    .FromRawExamTimeTablingData(
                        DatasetXmlRaw.ExamTimetablingData.Parse(filePathXml));

            LinkagerXml linkager = new()
            {
                I_data = data,
                I_exams = data.Exams,
            };
            linkager.Initialize().Run();

            ColorerXml colorer = new()
            {
                I_exam_after = linkager.O_exam_after,
                I_exam_exclusive = linkager.O_exam_exclusive,
                I_exam_linkages = linkager.O_exam_linkages,
                I_exam_requires = linkager.O_exam_requires,
                I_exams = data.Exams,
            };
            colorer.Initialize().Run();

            BoxGeneratorXml boxGenerator = new()
            {
                I_data = data,
                I_exam_linkages = linkager.O_exam_linkages,
            };
            boxGenerator.Initialize().Run();

            FirstSolutionGeneratorXml generatorXml = new()
            {
                I_data = data,
                I_color_exams = colorer.O_color_exams,
                I_exam_linkages = linkager.O_exam_linkages,
                I_exam_after = linkager.O_exam_after,
                I_exam_exclusive = linkager.O_exam_exclusive,
                I_exam_requires = linkager.O_exam_requires,
                I_lake = boxGenerator.O_lake,
            };
            generatorXml.Initialize().Run();

            ValidatorXml validatorXml = new()
            {
                I_data = data,
                I_lake = boxGenerator.O_lake,
            };
            validatorXml.Initialize().Run();


            SimulatedAnnealingXml simulatedAnnealing = new()
            {
                I_exam_after = linkager.O_exam_after,
                I_exam_exclusive = linkager.O_exam_exclusive,
                I_exam_linkages = linkager.O_exam_linkages,
                I_exam_requires = linkager.O_exam_requires,
                I_lake = boxGenerator.O_lake,
            };
            simulatedAnnealing.RunSimulatedAnnealingShift(data, boxGenerator.O_lake, 100, 0.55, 0.9, 3, 5);
            simulatedAnnealing.RunSimulatedAnnealing(data, boxGenerator.O_lake, 10, 0.55, 0.99, 3, 10);

            validatorXml = new()
            {
                I_data = data,
                I_lake = boxGenerator.O_lake,
            };
            validatorXml.Initialize().Run();

            return boxGenerator.O_lake;
        }
    }

    public class PipelineNormal
    {
        public SapLichThiITCAlgo.Lake Run()
        {
            string filePath = "exam\\exam_comp_set8.exam";
            TimetablingDataReader timetablingDataReader = new();
            TimetablingData data = timetablingDataReader.Read(filePath);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Linkager linkager = new()
            {
                I_data = data,
                I_exams = data.Exams,
            };
            linkager.Initialize().Run();
            Colorer colorer = new()
            {
                I_exams = data.Exams,
                I_exam_linkages = linkager.O_exam_linkages,
                I_exam_requires = linkager.O_exam_requires,
                I_exam_after = linkager.O_exam_after,
                I_exam_exclusive = linkager.O_exam_exclusive,
            };
            colorer.Initialize().Run();
            ValidatorColor validatorColor = new()
            {
                I_color_exams = colorer.O_color_exams
            };
            validatorColor.Initialize().Run();
            BoxGenerator boxGenerator = new()
            {
                I_data = data,
                I_exam_linkages = linkager.O_exam_linkages,
            };
            boxGenerator.Initialize().Run();
            FirstSolutionGenerator firstSolutionGenerator = new()
            {
                I_data = data,
                I_color_exams = colorer.O_color_exams,
                I_lake = boxGenerator.O_lake,
                I_exam_after = linkager.O_exam_after,
                I_exam_exclusive = linkager.O_exam_exclusive,
                I_exam_linkages = linkager.O_exam_linkages,
                I_exam_requires = linkager.O_exam_requires,
            };

            firstSolutionGenerator.Initialize().Run();
            Validator validator = new()
            {
                I_data = data,
                I_lake = boxGenerator.O_lake,
            };
            validator.Initialize().Run();
            Solution solution = new Solution(boxGenerator.O_lake);
            Evaluator evaluator = new(data, solution);
            var dtf = evaluator.CalculateDistanceToFeasibility();
            Console.WriteLine($"distance to feasibility: {dtf}");
            var sp = evaluator.CalculateSoftPenalty();
            Console.WriteLine($"soft constraint: {sp}");


            SimulatedAnnealing simulatedAnnealing = new()
            {
                I_exam_linkages = linkager.O_exam_linkages,
                I_lake = firstSolutionGenerator.I_lake,
                I_exam_after = linkager.O_exam_after,
                I_exam_exclusive = linkager.O_exam_exclusive,
                I_exam_requires = linkager.O_exam_requires,
            };
            simulatedAnnealing.RunSimulatedAnnealingShift(data, boxGenerator.O_lake, 100, 0.55, 0.9, 3, 5);
            simulatedAnnealing.RunSimulatedAnnealing(data, boxGenerator.O_lake, 10, 0.55, 0.99, 3, 10);
            validator.Initialize().Run();

            return boxGenerator.O_lake;
        }
    }

}
