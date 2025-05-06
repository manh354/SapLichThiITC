using SapLichThiITC;

namespace WebAppBootStrap2.Services
{
    public class PurdueSchedulingService
    {
        private SapLichThiITCAlgoNew.Lake _lake;

        public async Task<SapLichThiITCAlgoNew.Lake> RunScheduler(string filePath)
        {
            PipelineXml pipelineXml = new PipelineXml();
            await Task.Run( () => _lake = pipelineXml.Run(filePath));
            return _lake;
        }
        public SapLichThiITCAlgoNew.Lake GetLake()
        {
            return _lake;
        }
    }
}
