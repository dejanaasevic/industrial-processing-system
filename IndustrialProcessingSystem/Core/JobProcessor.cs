public class JobProcessor
{
    // Executes a job based on its type
    public async Task<int> ExecuteJob(Job job) {
        switch (job.Type)
        {
            case JobType.Prime:
                return await ProcessPrime(job.Payload);
            case JobType.IO:
                return await ProcessIO(job.Payload);
            default:
                throw new Exception("Unknown job type");
        }
    }

    // Processes an IO job by simulating a delay and returning a random result
    private async Task<int> ProcessIO(string payload)
    {
        int delay = int.Parse(payload.Split(":")[1].Replace("_", ""));
        await Task.Delay(delay);
        return new Random().Next(0, 101);
    }
    
    // Processes a prime counting job using maximum thread specified in the payload
    private async Task<int> ProcessPrime(string payload)
    {
        string[] parts = payload.Split(",");
        int max = int.Parse(parts[0].Split(':')[1].Replace("_", ""));
        int threads = int.Parse(parts[1].Split(":")[1]);
        threads = Math.Clamp(threads, 1, 8);

        return await Task.Run(() => CountPrimes(max, threads));
    }

    // Counts the number of prime numbers in a given range with parallel processing
    private int CountPrimes(int max, int threads)
    {
        int count = 0;
        ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = threads };
        Parallel.For(2, max, options, i =>
        {
            if (IsPrime(i))
            {
                Interlocked.Increment(ref count);
            }
        });
        return count;
    }

    // Checks if a number is prime
    private bool IsPrime(int x)
    {
        if (x <= 1) return false;
        if (x == 2) return true;
        if (x % 2 == 0) return false;
        for(int i = 3; i <= Math.Sqrt(x); i += 2)
        {
            if (x % i == 0) return false;
        }
        return true;
    }
}