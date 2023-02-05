using Hangfire;
using HangFireSqlSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace HangFireSqlSample.Controllers
{
    [ApiController]
    [Route("api/people")]
    public class PeopleController : ControllerBase
    {
        private readonly IBackgroundJobClient backgroundJobClient;

        public PeopleController(IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }

        [HttpPost("create")]
        public ActionResult Create(string personName)
        {
            //backgroundJobClient.Enqueue<IPeopleRepository>(repository =>
            //    repository.CreatePerson(personName));
            RecurringJob.AddOrUpdate<ITimeService>("print-time", service => service.PrintNow(),
            Cron.Minutely);
            return Ok();
        }

        [HttpPost("schedule")]
        public ActionResult Schedule(string personName)
        {
            var jobId = backgroundJobClient.Schedule(() =>
                Console.WriteLine("The name is " + personName),
                TimeSpan.FromSeconds(5));

            backgroundJobClient.ContinueJobWith(jobId,
                () => Console.WriteLine($"The job {jobId} has finished"));

            return Ok();
        }
    }
}
