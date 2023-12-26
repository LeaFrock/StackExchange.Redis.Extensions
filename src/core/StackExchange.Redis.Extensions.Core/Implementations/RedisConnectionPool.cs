// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

using Microsoft.Extensions.Logging;

using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;

namespace StackExchange.Redis.Extensions.Core.Implementations
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedisConnectionPool"/> class.
    /// </summary>
    /// <param name="configuration">The redis configuration.</param>
    public class RedisConnectionPool(RedisConfiguration configuration)
    {
        private readonly IConnectionMultiplexer[] connectionMultiplexers = InitConnectionMultiplexers(configuration);

        /// <summary>
        /// The redis configuration
        /// </summary>
        public RedisConfiguration Configuration { get; } = configuration;

        /// <summary>
        /// Initialize the pool of <see cref="IStateAwareConnection"/>
        /// </summary>
        /// <param name="logger">The logger</param>
        public IStateAwareConnection[] Initialize(ILogger logger)
        {
            var conns = new IStateAwareConnection[connectionMultiplexers.Length];
            for (var i = 0; i < conns.Length; i++)
                conns[i] = Configuration.StateAwareConnectionFactory.Invoke(connectionMultiplexers[i], logger);

            return conns;
        }

        private static IConnectionMultiplexer[] InitConnectionMultiplexers(RedisConfiguration config)
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfLessThan(config.PoolSize, 1);
#else
            if (config.PoolSize < 1)
                throw new ArgumentOutOfRangeException(nameof(config.PoolSize), "PoolSize must be greater than 0.");
#endif

            var connectionMultiplexers = new ConnectionMultiplexer[config.PoolSize];
            for (var i = 0; i < connectionMultiplexers.Length; i++)
            {
                var multiplexer = ConnectionMultiplexer.Connect(config.ConfigurationOptions);

                if (config.ProfilingSessionProvider != null)
                    multiplexer.RegisterProfiler(config.ProfilingSessionProvider);

                connectionMultiplexers[i] = multiplexer;
            }

            return connectionMultiplexers;
        }
    }
}
