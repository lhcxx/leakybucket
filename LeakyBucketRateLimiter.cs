namespace LeakyBucket;

/// <summary>
/// Leaky bucket rate limiter with enhanced features
/// </summary>
public class LeakyBucketRateLimiter
{
    /// <summary>
    /// Bucket capacity
    /// </summary>
    public double Capacity { get; }
    
    /// <summary>
    /// Leak rate (units leaked per second)
    /// </summary>
    public double LeakRate { get; }
    
    /// <summary>
    /// User bucket state dictionary
    /// </summary>
    private readonly Dictionary<string, BucketState> _userBuckets;
    private readonly object _lock = new object();

    public LeakyBucketRateLimiter(double capacity, double leakRate)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive", nameof(capacity));
        if (leakRate <= 0)
            throw new ArgumentException("Leak rate must be positive", nameof(leakRate));
            
        Capacity = capacity;
        LeakRate = leakRate;
        _userBuckets = new Dictionary<string, BucketState>();
    }

    /// <summary>
    /// Create rate limiter
    /// </summary>
    /// <param name="capacity">Bucket capacity</param>
    /// <param name="leakRate">Leak rate (per second)</param>
    /// <returns>New rate limiter</returns>
    public static LeakyBucketRateLimiter CreateRateLimiter(double capacity, double leakRate)
    {
        return new LeakyBucketRateLimiter(capacity, leakRate);
    }

    /// <summary>
    /// Check if request is allowed
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="timestamp">Timestamp (Unix seconds)</param>
    /// <returns>Tuple: (allowed, new limiter state)</returns>
    public (bool allowed, LeakyBucketRateLimiter newLimiter) AllowRequest(string userId, double timestamp)
    {
        lock (_lock)
        {
            var newLimiter = new LeakyBucketRateLimiter(Capacity, LeakRate);
            
            // Copy all user bucket states
            foreach (var kvp in _userBuckets)
            {
                newLimiter._userBuckets[kvp.Key] = new BucketState(kvp.Value);
            }

            // Get or create user bucket
            if (!newLimiter._userBuckets.TryGetValue(userId, out var bucketState))
            {
                bucketState = new BucketState(0, timestamp);
                newLimiter._userBuckets[userId] = bucketState;
            }

            // Handle time regression case
            if (timestamp < bucketState.LastUpdateTime)
            {
                // Time regression: don't update bucket state, check current capacity directly
                var canAdd = bucketState.CurrentLevel < Capacity;
                if (canAdd)
                {
                    bucketState.CurrentLevel += 1;
                }
                bucketState.LastUpdateTime = Math.Max(bucketState.LastUpdateTime, timestamp);
                return (canAdd, newLimiter);
            }

            // Calculate time difference and update bucket state
            var timeDelta = timestamp - bucketState.LastUpdateTime;
            
            // Handle very large time intervals
            if (timeDelta > Capacity / LeakRate * 2)
            {
                // If time interval is too long, bucket should be completely empty
                bucketState.CurrentLevel = 0;
                bucketState.LastUpdateTime = timestamp;
            }
            else
            {
                // Normal leak calculation
                var leakedAmount = timeDelta * LeakRate;
                bucketState.CurrentLevel = Math.Max(0, bucketState.CurrentLevel - leakedAmount);
                bucketState.LastUpdateTime = timestamp;
            }

            // Check if new request can be added
            var canAddRequest = bucketState.CurrentLevel < Capacity;
            if (canAddRequest)
            {
                bucketState.CurrentLevel += 1;
            }

            return (canAddRequest, newLimiter);
        }
    }

    /// <summary>
    /// Get user bucket state
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Bucket info or null (if user doesn't exist)</returns>
    public BucketInfo? GetBucketState(string userId)
    {
        lock (_lock)
        {
            if (!_userBuckets.TryGetValue(userId, out var bucketState))
            {
                return null;
            }

            return new BucketInfo
            {
                CurrentLevel = bucketState.CurrentLevel,
                Capacity = Capacity,
                LeakRate = LeakRate,
                LastUpdateTime = bucketState.LastUpdateTime
            };
        }
    }

    /// <summary>
    /// Get all user bucket states
    /// </summary>
    /// <returns>Dictionary of all user bucket states</returns>
    public Dictionary<string, BucketInfo> GetAllBucketStates()
    {
        lock (_lock)
        {
            var result = new Dictionary<string, BucketInfo>();
            foreach (var kvp in _userBuckets)
            {
                result[kvp.Key] = new BucketInfo
                {
                    CurrentLevel = kvp.Value.CurrentLevel,
                    Capacity = Capacity,
                    LeakRate = LeakRate,
                    LastUpdateTime = kvp.Value.LastUpdateTime
                };
            }
            return result;
        }
    }

    /// <summary>
    /// Clean up user buckets that have been idle for too long (optional feature)
    /// </summary>
    /// <param name="currentTime">Current time</param>
    /// <param name="maxIdleTime">Maximum idle time</param>
    public void CleanupIdleBuckets(double currentTime, double maxIdleTime = 3600) // Default 1 hour
    {
        lock (_lock)
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in _userBuckets)
            {
                if (currentTime - kvp.Value.LastUpdateTime > maxIdleTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _userBuckets.Remove(key);
            }
        }
    }
}

/// <summary>
/// Bucket state (internal use)
/// </summary>
public class BucketState
{
    public double CurrentLevel { get; set; }
    public double LastUpdateTime { get; set; }

    public BucketState(double currentLevel, double lastUpdateTime)
    {
        CurrentLevel = currentLevel;
        LastUpdateTime = lastUpdateTime;
    }

    public BucketState(BucketState other)
    {
        CurrentLevel = other.CurrentLevel;
        LastUpdateTime = other.LastUpdateTime;
    }
}

/// <summary>
/// Bucket info (public interface)
/// </summary>
public class BucketInfo
{
    public double CurrentLevel { get; set; }
    public double Capacity { get; set; }
    public double LeakRate { get; set; }
    public double LastUpdateTime { get; set; }

    public override string ToString()
    {
        return $"BucketInfo(Level={CurrentLevel:F2}/{Capacity}, LeakRate={LeakRate}, LastUpdate={LastUpdateTime:F2})";
    }
}