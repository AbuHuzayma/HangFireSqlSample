using Hangfire;
using HangFireSqlSample.Entities;
using Serilog;

namespace HangFireSqlSample.Services
{
    public interface IPeopleRepository
    {
        Task CreatePerson(string personName);
    }

    public class PeopleRepository : IPeopleRepository
    {
        private readonly ILogger<PeopleRepository> logger;

        public PeopleRepository(ILogger<PeopleRepository> logger)
        {
            this.logger = logger;
        }
        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        public async Task CreatePerson(string personName)
        {
            Console.WriteLine($"Adding person {personName}");
            Log.Information($"Adding person {personName}");
            await Task.Delay(5000);
            Log.Information($"Adding person {personName}");
            Console.WriteLine($"Added the person {personName}");
        }
    }
}
