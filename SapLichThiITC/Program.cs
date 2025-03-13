using SapLichThiITCAlgo;
using SapLichThiITCCore;
using SapLichThiITCInputHelper;

namespace SapLichThiITC
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string filePath = "exam\\exam_comp_set8.exam";
            /*
            string folderPath = "xmlexams";
            DatasetMultipleInputXml datasetMultipleXml = new DatasetMultipleInputXml(folderPath).Run();
            */

            TimetablingDataReader timetablingDataReader = new();
            TimetablingData data =  timetablingDataReader.Read(filePath);
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
                I_lake = firstSolutionGenerator.I_lake ,
                I_exam_after = linkager.O_exam_after,
                I_exam_exclusive = linkager.O_exam_exclusive,
                I_exam_requires = linkager.O_exam_requires,
            };
            simulatedAnnealing.RunSimulatedAnnealing(data, boxGenerator.O_lake, 10, 0.55, 0.99, 3, 10); ;
            validator.Initialize().Run();
        }
    }
}
