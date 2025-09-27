using LeakyBucket;

namespace LeakyBucket.Tests;

public class DebugTest
{
    public static void RunDebugTest()
    {
        Console.WriteLine("=== Debug Test ===");
        
        var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 3, leakRate: 1.0);
        var currentLimiter = limiter;
        
        Console.WriteLine("Testing burst with capacity=3, leakRate=1.0:");
        
        for (int i = 0; i < 5; i++)
        {
            var timestamp = i * 0.1;
            var (allowed, newLimiter) = currentLimiter.AllowRequest("user1", timestamp);
            currentLimiter = newLimiter;
            
            var bucketState = currentLimiter.GetBucketState("user1");
            Console.WriteLine($"Request {i+1} at {timestamp}: allowed={allowed}, bucket={bucketState?.CurrentLevel:F2}");
        }
    }
}
