// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace StackExchange.Redis.Extensions.Core.Implementations;

/// <inheritdoc/>
public class RedisClientFactory : IRedisClientFactory
{
    private readonly Dictionary<string, IRedisClient> redisCacheClients;
    private readonly string? defaultConnectionName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisClientFactory"/> class.
    /// </summary>
    /// <param name="connectionPools">The connection pools.</param>
    /// <param name="loggerFactory">The logger factory</param>
    /// <param name="serializer">The cache serializer</param>
    public RedisClientFactory(RedisConnectionPool[] connectionPools, ILoggerFactory? loggerFactory, ISerializer serializer)
    {
        redisCacheClients = new(connectionPools.Length);

        var poolManagerLogger = loggerFactory?.CreateLogger<RedisConnectionPoolManager>() ?? NullLogger<RedisConnectionPoolManager>.Instance;

        foreach (var pool in connectionPools)
        {
            var poolManager = new RedisConnectionPoolManager(pool, poolManagerLogger);
            var configuration = pool.Configuration;
            redisCacheClients.Add(configuration.Name!, new RedisClient(poolManager, serializer, configuration));

            if (configuration.IsDefault)
                defaultConnectionName = configuration.Name;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IRedisClient> GetAllClients() => redisCacheClients.Values;

    /// <inheritdoc/>
    public IRedisClient GetDefaultRedisClient()
    {
        return redisCacheClients[defaultConnectionName!];
    }

    /// <inheritdoc/>
    public IRedisClient GetRedisClient(string? name = null)
    {
        name ??= defaultConnectionName!;

        return redisCacheClients[name];
    }

    /// <inheritdoc/>
    public IRedisDatabase GetDefaultRedisDatabase()
    {
        return redisCacheClients[defaultConnectionName!].GetDefaultDatabase();
    }

    /// <inheritdoc/>
    public IRedisDatabase GetRedisDatabase(string? name = null)
    {
        name ??= defaultConnectionName!;

        return redisCacheClients[name].GetDefaultDatabase();
    }
}
