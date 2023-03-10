# HangFireSqlSample
Hangfire is an open-source framework for .NET that makes it easy to perform background processing of long-running tasks, such as sending emails or updating databases. Here's how you can use Hangfire in your .NET application:

# Background process Types

# Fire-and-Forget Jobs
Fire-and-forget jobs are executed only once and almost immediately after creation.
````c#
var jobId = BackgroundJob.Enqueue(
    () => Console.WriteLine("Fire-and-forget!"));
````
# Delayed Jobs
Delayed jobs are executed only once too, but not immediately, after a certain time interval.
````c#
var jobId = BackgroundJob.Schedule(
    () => Console.WriteLine("Delayed!"),
    TimeSpan.FromDays(7));
````
# Recurring Jobs
Recurring jobs fire many times on the specified CRON schedule.
````c#
RecurringJob.AddOrUpdate(
    "myrecurringjob",
    () => Console.WriteLine("Recurring!"),
    Cron.Daily);
 ````
# Continuations
Continuations are executed when its parent job has been finished.
````c#
BackgroundJob.ContinueJobWith(
    jobId,
    () => Console.WriteLine("Continuation!"));
````
# Batches Pro
Batch is a group of background jobs that is created atomically and considered as a single entity.
````c#
var batchId = BatchJob.StartNew(x =>
{
    x.Enqueue(() => Console.WriteLine("Job 1"));
    x.Enqueue(() => Console.WriteLine("Job 2"));
});
````
# Batch Continuations Pro
Batch continuation is fired when all background jobs in a parent batch finished.
````c#
BatchJob.ContinueBatchWith(batchId, x =>
{
    x.Enqueue(() => Console.WriteLine("Last Job"));
});
````

# Installations 

Install the Hangfire package in your .NET project using NuGet.
Add Hangfire services to your ASP.NET Core application using Startup.cs:
````c#
public void ConfigureServices(IServiceCollection services)
{
   services.AddHangfire(configuration => configuration
      .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
      .UseSimpleAssemblyNameTypeSerializer()
      .UseDefaultTypeSerializer()
      .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection")));
}
````
Add Hangfire middleware to your ASP.NET Core application using Startup.cs:
````c#
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
   app.UseHangfireDashboard();
}
````
Create a background job:
````c#
BackgroundJob.Enqueue(() => Console.WriteLine("Hello, Hangfire!"));
````
Start the Hangfire background process:
````c#
app.UseHangfireServer();
````

# Disable Concurrent Execution

For more information about Sql Configrution and Options [hangfire.io](https://docs.hangfire.io/en/latest/configuration/using-sql-server.html).

````c#
// Add Hangfire SqlServerStorageOptions.
    services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
````
    
----
> **SlidingInvisibilityTimeout** is used to indicate how long a BackgroundJob execution is allowed to run for without status change (success/failure) before Hangfire decides the BackgroundJob execution was not successful and needs to be made visible to the HangfireServer for processing again. The idea being that if a HangfireServer starts processing a BackgroundJob and then gets killed without being able to report back that the BackgroundJob failed then there would need to be a retry of that BackgroundJob.
You probably shouldn???t change SlidingInvisibilityTimeout unless you have a really compelling reason. For example, if you set it to 5 minutes and then have a BackgroundJob that runs for 6 minutes, Hangfire is going to end up queuing up multiple instances of that BackgroundJob because it hasn???t completed fast enough.

> **QueuePollInterval** is how long the server is going to wait in between checking the database for new BackgroundJobs to process. Setting this value will result in Jobs being picked up off the queue faster, but also more load on your SQL Server.
**This will disable global locks, which will prevent multiple Hangfire servers from processing the same job simultaneously. By setting QueuePollInterval to TimeSpan.Zero, the servers will not poll the queue at the same time, effectively preventing concurrent execution**.

----
Also, you can use the **SkipConcurrentExecutionAttribute** custom attribute to prevent multiple instances of the same job from running simultaneously. To use this attribute, simply apply this class in your application and add it to the method that represents the background job:
````c#
    public class SkipConcurrentExecutionAttribute : JobFilterAttribute, IServerFilter, IElectStateFilter
    {
        private readonly int _timeoutSeconds;
        private const string DistributedLock = "DistributedLock";

        public SkipConcurrentExecutionAttribute(int timeOutSeconds)
        {
            if (timeOutSeconds < 0) throw new ArgumentException("Timeout argument value should be greater that zero.");
            this._timeoutSeconds = timeOutSeconds;
        }
        public void OnPerformed(PerformedContext filterContext)
        {
            if (!filterContext.Items.ContainsKey(DistributedLock))
                throw new InvalidOperationException("Can not release a distributed lock: it was not acquired.");

            var distributedLock = (IDisposable)filterContext.Items[DistributedLock];
            distributedLock?.Dispose();
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            var resource = String.Format(
                               "{0}.{1}",
                              filterContext.BackgroundJob.Job.Type.FullName,
                              filterContext.BackgroundJob.Job.Method.Name);

            var timeOut = TimeSpan.FromSeconds(_timeoutSeconds);
            try
            {
                var distributedLock = filterContext.Connection.AcquireDistributedLock(resource, timeOut);
                filterContext.Items[DistributedLock] = distributedLock;
            }
            catch { filterContext.Canceled = true; }
        }

        public void OnStateElection(ElectStateContext context)
        {
            //if (context.CandidateState as FailedState != null)
            //{

            //}
        }
    }
````
In this example, create attribute and set to have a timeout of 0 seconds . This means that if another instance of MyJob is already running, any subsequent calls to this job will be placed in a separate queue and will wait for the first instance to finish before starting. 


# Best Practices

**Make your background methods reentrant**

Reentrancy means that a method can be interrupted in the middle of its execution and then safely called again. The interruption can be caused by many different things (i.e. exceptions, server shut-down), and Hangfire will attempt to retry processing many times.

You can have many problems, if you don???t prepare your jobs to be reentrant. For example, if you are using an email sending background job and experience an error with your SMTP service, you can end with multiple emails sent to the addressee.

**Instead of doing this:**
````c#
public void Method()
{
    _emailService.Send("person@example.com", "Hello!");
}
````
**Consider doing this:**
````c#
public void Method(int deliveryId)
{
    if (_emailService.IsNotDelivered(deliveryId))
    {
        _emailService.Send("person@example.com", "Hello!");
        _emailService.SetDelivered(deliveryId);
    }
}
````
