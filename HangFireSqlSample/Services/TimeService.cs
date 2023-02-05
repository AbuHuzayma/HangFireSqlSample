using Hangfire;
using Serilog;

namespace HangFireSqlSample.Services
{
    public interface ITimeService
    {
        Task PrintNow();
    }

    public class TimeService : ITimeService
    {
        private readonly ILogger<TimeService> logger;

        public TimeService(ILogger<TimeService> logger)
        {
            this.logger = logger;
        }
        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        public async Task PrintNow()
        {
            Guid guid= Guid.NewGuid();
            Console.WriteLine("Start => "+ guid+" =>"+DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
            await Task.Delay(10000);
            Console.WriteLine("End => " + guid + " =>" + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
        }
    }
}
