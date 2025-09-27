using LeakyBucket;
using LeakyBucket.Tests;

namespace LeakyBucket;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Leaky Bucket Rate Limiter Demo ===");
        
        // Run all tests
        RateLimiterTests.RunAllTests();
        
        Console.WriteLine("\n=== Demo Completed ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
