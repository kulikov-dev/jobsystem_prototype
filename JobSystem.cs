    /// <summary> Prototype of the simple job system to process jobs asynchronously. </summary>
    internal class JobSystem
    {
        /// <summary> Queue of all not processed jobs </summary>
        private readonly ConcurrentQueue<Task> jobsQueue = new ConcurrentQueue<Task>();
        /// <summary> Currently processing jobs </summary>
        private readonly List<Task> jobsInWork;
        /// <summary> Maximum count of simultaneous jobs in work </summary>
        private int jobsInWorkCount;

        /// <summary> Locker for jobs in work </summary>
        private readonly object jobsLocker = new object();
        /// <summary> Locker for queue checking </summary>
        private readonly object checkerLocker = new object();

        /// <summary> Constructor </summary>
        /// <param name="jobsInWorkCount"> Maximum count of simultaneous jobs in work </param>
        public JobSystem(int jobsInWorkCount)
        {
            this.jobsInWorkCount = jobsInWorkCount;
            jobsInWork = new List<Task>(jobsInWorkCount);

        }
        /// <summary> Constructor </summary>
        /// <param name="jobsInWorkCount"> Maximum count of simultaneous jobs in work </param>
        /// <param name="jobs"> Created jobs for processing </param>
        public JobSystem(int jobsInWorkCount, IEnumerable<Task> jobs)
            : this(jobsInWorkCount)
        {
            foreach (var job in jobs)
            {
                jobsQueue.Enqueue(job);
            }
        }

        /// <summary> Add new jobs for processing </summary>
        /// <param name="jobs"> Created jobs for processing </param>
        public void AddJob(IEnumerable<Task> jobs)
        {
            foreach (var task in jobs)
            {
                AddJob(task);
            }
        }
        /// <summary> Add new job for processing </summary>
        /// <param name="job"> Created job for processing </param>
        public void AddJob(Task job)
        {
            lock (jobsLocker)
            {
                if (jobsInWork.Count >= jobsInWorkCount)
                    jobsQueue.Enqueue(job);
                else
                    DoWork(job);
            }
        }

        /// <summary> Start job </summary>
        /// <param name="job"> Job for processing </param>
        private void DoWork(Task job)
        {
            jobsInWork.Add(job);
            job.Start();
            job.ContinueWith(JobFinished);
        }
        /// <summary> Job finished </summary>
        /// <param name="job"> Processed job </param>
        private void JobFinished(Task job)
        {
            lock (jobsLocker)
            {
                jobsInWork.Remove(job);
                CheckQueue();
            }
        }
        /// <summary> Check queue for waiting jobs and start them. </summary>
        private void CheckQueue()
        {
            lock (checkerLocker)
            {
                while (jobsQueue.Count > 0 && jobsInWork.Count < jobsInWorkCount)
                {
                    Task task;
                    if (jobsQueue.TryDequeue(out task))
                    {
                        DoWork(task);
                    }
                }
            }
        }
    }
