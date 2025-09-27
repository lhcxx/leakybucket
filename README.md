# Leaky Bucket Rate Limiter

A leaky bucket rate limiter implementation based on the leaky bucket algorithm, using C# and functional programming approach.

## Features

- **Functional Design**: Minimizes mutable state, each operation returns a new limiter instance
- **Time-based Leaking**: Buckets leak at a constant rate based on elapsed time
- **Per-user Buckets**: Each user ID has an independent bucket
- **Enhanced Features**: Handles time regression, large time intervals, thread safety, and other edge cases
- **Simple Design**: Single class implementation with all features, easy to use and maintain

## Core Interface

### Create Rate Limiter
```csharp
var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 5, leakRate: 1.0);
```

### Check if Request is Allowed
```csharp
var (allowed, newLimiter) = limiter.AllowRequest("user1", timestamp: 1234567890.0);
```

### Get Bucket State
```csharp
var bucketInfo = limiter.GetBucketState("user1");
```

## Usage Example

```csharp
// Create rate limiter: capacity 5, leak rate 1.0/second
var limiter = LeakyBucketRateLimiter.CreateRateLimiter(capacity: 5, leakRate: 1.0);

// User1 sends request
var (allowed1, limiter1) = limiter.AllowRequest("user1", timestamp: 0.0);
Console.WriteLine($"Request 1: {allowed1}"); // True

// User1 continues sending requests
var (allowed2, limiter2) = limiter1.AllowRequest("user1", timestamp: 0.1);
Console.WriteLine($"Request 2: {allowed2}"); // True

// User2 sends independent request
var (allowed3, limiter3) = limiter2.AllowRequest("user2", timestamp: 0.2);
Console.WriteLine($"Request 3: {allowed3}"); // True

// Check bucket states
var user1State = limiter3.GetBucketState("user1");
var user2State = limiter3.GetBucketState("user2");
Console.WriteLine($"User1: {user1State}");
Console.WriteLine($"User2: {user2State}");
```

## Algorithm Description

### Leaky Bucket Algorithm Principle
1. Each user has a bucket with a fixed capacity
2. When a request arrives, add 1 unit to the bucket
3. The bucket "leaks" at a constant rate (leaks specified units per second)
4. If adding a request would cause the bucket to overflow, reject the request

### Time Handling
- Supports floating-point timestamps (Unix seconds)
- Handles time regression scenarios
- Handles large time intervals (automatically empties bucket)

### Edge Cases
- **New Users**: Automatically creates empty buckets
- **Time Regression**: Doesn't update bucket state, directly checks capacity
- **Large Time Intervals**: If time interval is too long, bucket is completely emptied
- **Bucket Overflow**: Strictly checks capacity limits

## Test Scenarios

The project includes comprehensive test cases:

1. **Basic Functionality Tests**: Verify basic request allow/deny logic
2. **Burst Handling Tests**: Test handling of rapid consecutive requests
3. **Time-based Leaking Tests**: Verify time-based bucket leaking
4. **Multi-user Tests**: Verify independent bucket behavior for different users
5. **Edge Case Tests**: Test various extreme scenarios
6. **Time Regression Tests**: Handle timestamp regression situations
7. **Large Time Interval Tests**: Handle long periods without requests

## Project Structure

```
leakybucket/
├── LeakyBucketRateLimiter.cs    # Single rate limiter class
├── Program.cs                    # Main program
├── Tests/                        # Test directory
│   ├── RateLimiterTests.cs       # Main test class
│   └── DebugTest.cs             # Debug tests
└── .gitignore                   # Git ignore file
```

## Running the Project

```bash
# Build the project
dotnet build

# Run tests
dotnet run
```

## Design Decisions

### Simplified Architecture
- **Single Class Design**: All functionality concentrated in one `LeakyBucketRateLimiter` class
- **Complete Features**: Includes basic leaky bucket algorithm and all enhanced features
- **Easy Maintenance**: Reduces code complexity, improves readability

### Functional Approach
- Each operation returns a new limiter instance, avoiding shared mutable state
- Facilitates concurrent processing and testing
- Aligns with immutable data structure principles

### Enhanced Features
- **Time Regression Handling**: Handles clock synchronization issues in distributed systems
- **Large Time Interval Handling**: Automatically empties buckets for long-inactive users
- **Thread Safety**: Uses locking mechanisms to ensure concurrency safety
- **Idle Bucket Cleanup**: Supports cleaning up long-unused user buckets

### Performance Considerations
- Uses Dictionary to store user buckets, O(1) lookup time
- Minimizes object creation while maintaining functional characteristics
- Supports bucket state queries for monitoring and debugging

## Extension Suggestions

- Add persistence support (Redis, database)
- Implement distributed rate limiting
- Add monitoring and metrics collection
- Support dynamic capacity and leak rate adjustment
- Implement more complex supervision strategies