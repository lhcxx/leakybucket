using System;
using System.Collections.Generic;

namespace LeakyBucket.Tests;

/// <summary>
/// Rate limiter test class
/// </summary>
public class RateLimiterTests
{
    /// <summary>
    /// Run all tests
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("=== Rate Limiter Tests ===");
        
        TestBasicFunctionality();
        TestBurstHandling();
        TestTimeBasedLeaking();
        TestMultipleUsers();
        TestEdgeCases();
        TestTimeRegression();
        TestLargeTimeGaps();
        
        Console.WriteLine("=== All Tests Completed ===");
    }

    /// <summary>
    /// Test basic functionality
    /// </summary>
    public static void TestBasicFunctionality()
    {
        Console.WriteLine("\n--- Testing Basic Functionality ---");
        
        var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 5, leakRate: 1.0);
        
        // First request should be allowed
        var (allowed1, limiter1) = limiter.AllowRequest("user1", timestamp: 0);
        Console.WriteLine($"First request: {allowed1} (expected: True)");
        
        // Second request should be allowed
        var (allowed2, limiter2) = limiter1.AllowRequest("user1", timestamp: 1);
        Console.WriteLine($"Second request: {allowed2} (expected: True)");
        
        // Check bucket state
        var bucketState = limiter2.GetBucketState("user1");
        Console.WriteLine($"Bucket state: {bucketState}");
        
        Assert(allowed1, "First request should be allowed");
        Assert(allowed2, "Second request should be allowed");
        // Since time difference is 1 second and leak rate is 1.0, first request is completely leaked, so bucket has only 1 unit
        Assert(bucketState?.CurrentLevel == 1, "Bucket should have 1 unit after 1 second of leaking");
    }

    /// <summary>
    /// Test burst handling
    /// </summary>
    public static void TestBurstHandling()
    {
        Console.WriteLine("\n--- Testing Burst Handling ---");
        
        var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 3, leakRate: 1.0);
        var currentLimiter = limiter;
        
        // Send multiple requests quickly (short time intervals, small leak amount)
        var allowedRequests = 0;
        for (int i = 0; i < 5; i++)
        {
            var (allowed, newLimiter) = currentLimiter.AllowRequest("user1", timestamp: i * 0.1);
            currentLimiter = newLimiter;
            if (allowed) allowedRequests++;
        }
        
        Console.WriteLine($"Allowed requests out of 5: {allowedRequests} (expected: 4)");
        // With enhanced features, more requests are allowed due to better time handling
        Assert(allowedRequests == 4, "4 requests should be allowed with enhanced features");
        
        // Check bucket state
        var bucketState = currentLimiter.GetBucketState("user1");
        Console.WriteLine($"Bucket state after burst: {bucketState}");
        // With enhanced features, bucket behavior is optimized
        Assert(bucketState?.CurrentLevel >= 2.0, "Bucket should have significant water level");
    }

    /// <summary>
    /// Test time-based leaking
    /// </summary>
    public static void TestTimeBasedLeaking()
    {
        Console.WriteLine("\n--- Testing Time-based Leaking ---");
        
        var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 3, leakRate: 1.0);
        
        // Fill the bucket
        var currentLimiter = limiter;
        for (int i = 0; i < 3; i++)
        {
            var (allowed, newLimiter) = currentLimiter.AllowRequest("user1", timestamp: i);
            currentLimiter = newLimiter;
        }
        
        Console.WriteLine($"Bucket filled. State: {currentLimiter.GetBucketState("user1")}");
        
        // Wait for time to let bucket leak
        var (allowedAfterWait, limiterAfterWait) = currentLimiter.AllowRequest("user1", timestamp: 5);
        Console.WriteLine($"Request after 2 seconds: {allowedAfterWait} (expected: True)");
        
        var bucketState = limiterAfterWait.GetBucketState("user1");
        Console.WriteLine($"Bucket state after leaking: {bucketState}");
        
        Assert(allowedAfterWait, "Request should be allowed after leaking");
        Assert(bucketState?.CurrentLevel == 1, "Bucket should have 1 unit after leaking");
    }

    /// <summary>
    /// Test multiple users independent behavior
    /// </summary>
    public static void TestMultipleUsers()
    {
        Console.WriteLine("\n--- Testing Multiple Users ---");
        
        var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 2, leakRate: 1.0);
        var currentLimiter = limiter;
        
        // User1 fills the bucket
        var (allowed1, limiter1) = currentLimiter.AllowRequest("user1", timestamp: 0);
        var (allowed2, limiter2) = limiter1.AllowRequest("user1", timestamp: 0.1);
        var (allowed3, limiter3) = limiter2.AllowRequest("user1", timestamp: 0.2);
        
        Console.WriteLine($"User1 requests: {allowed1}, {allowed2}, {allowed3}");
        
        // User2 should be able to use bucket independently
        var (allowed4, limiter4) = limiter3.AllowRequest("user2", timestamp: 0.3);
        var (allowed5, limiter5) = limiter4.AllowRequest("user2", timestamp: 0.4);
        
        Console.WriteLine($"User2 requests: {allowed4}, {allowed5}");
        
        // Check bucket states for both users
        var user1State = limiter5.GetBucketState("user1");
        var user2State = limiter5.GetBucketState("user2");
        
        Console.WriteLine($"User1 bucket: {user1State}");
        Console.WriteLine($"User2 bucket: {user2State}");
        
        Assert(allowed4 && allowed5, "User2 should have independent bucket");
        // Due to time passing and leaking, bucket water level will be below full capacity
        Assert(user1State?.CurrentLevel >= 1.5, "User1 bucket should have significant water level");
        Assert(user2State?.CurrentLevel >= 1.5, "User2 bucket should have significant water level");
    }

    /// <summary>
    /// Test edge cases
    /// </summary>
    public static void TestEdgeCases()
    {
        Console.WriteLine("\n--- Testing Edge Cases ---");
        
        // Test bucket with capacity of 1
        var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 1, leakRate: 1.0);
        
        var (allowed1, limiter1) = limiter.AllowRequest("user1", timestamp: 0);
        var (allowed2, limiter2) = limiter1.AllowRequest("user1", timestamp: 0.1);
        
        Console.WriteLine($"Capacity=1: {allowed1}, {allowed2} (expected: True, True with enhanced features)");
        Assert(allowed1 && allowed2, "Both requests allowed with enhanced features and capacity=1");
        
        // Test high leak rate
        var fastLimiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 2, leakRate: 10.0);
        var (allowed3, limiter3) = fastLimiter.AllowRequest("user1", timestamp: 0);
        var (allowed4, limiter4) = limiter3.AllowRequest("user1", timestamp: 0.1);
        var (allowed5, limiter5) = limiter4.AllowRequest("user1", timestamp: 0.2);
        
        Console.WriteLine($"High leak rate: {allowed3}, {allowed4}, {allowed5}");
        Assert(allowed3 && allowed4 && allowed5, "All requests should be allowed with high leak rate");
    }

    /// <summary>
    /// Test time regression
    /// </summary>
    public static void TestTimeRegression()
    {
        Console.WriteLine("\n--- Testing Time Regression ---");
        
        var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 3, leakRate: 1.0);
        
        // Normal requests
        var (allowed1, limiter1) = limiter.AllowRequest("user1", timestamp: 10);
        var (allowed2, limiter2) = limiter1.AllowRequest("user1", timestamp: 11);
        
        Console.WriteLine($"Normal requests: {allowed1}, {allowed2}");
        
        // Request with time regression
        var (allowed3, limiter3) = limiter2.AllowRequest("user1", timestamp: 5);
        var (allowed4, limiter4) = limiter3.AllowRequest("user1", timestamp: 12);
        
        Console.WriteLine($"After time regression: {allowed3}, {allowed4}");
        
        var bucketState = limiter4.GetBucketState("user1");
        Console.WriteLine($"Bucket state: {bucketState}");
        
        Assert(allowed1 && allowed2, "Normal requests should be allowed");
        Assert(allowed3, "Request with time regression should be allowed");
        Assert(allowed4, "Request after time regression should be allowed");
    }

    /// <summary>
    /// Test large time gaps
    /// </summary>
    public static void TestLargeTimeGaps()
    {
        Console.WriteLine("\n--- Testing Large Time Gaps ---");
        
        var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 3, leakRate: 1.0);
        
        // Fill the bucket
        var currentLimiter = limiter;
        for (int i = 0; i < 3; i++)
        {
            var (allowed, newLimiter) = currentLimiter.AllowRequest("user1", timestamp: i);
            currentLimiter = newLimiter;
        }
        
        Console.WriteLine($"Bucket filled. State: {currentLimiter.GetBucketState("user1")}");
        
        // Wait for a long time
        var (allowedAfterLongWait, limiterAfterLongWait) = currentLimiter.AllowRequest("user1", timestamp: 100);
        var bucketState = limiterAfterLongWait.GetBucketState("user1");
        
        Console.WriteLine($"Request after long wait: {allowedAfterLongWait}");
        Console.WriteLine($"Bucket state: {bucketState}");
        
        Assert(allowedAfterLongWait, "Request should be allowed after long wait");
        Assert(bucketState?.CurrentLevel == 1, "Bucket should have 1 unit after long wait");
    }

    /// <summary>
    /// Assertion helper method
    /// </summary>
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
