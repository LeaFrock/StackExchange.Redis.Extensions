// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core.Implementations;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A set of extension methods that help you to confire StackExchangeRedisExtensions into your dependency injection
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Add StackExchange.Redis with its serialization provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="redisConfiguration">The redis configration.</param>
    /// <typeparam name="T">The typof of serializer. <see cref="ISerializer" />.</typeparam>
    public static IServiceCollection AddStackExchangeRedisExtensions<T>(
        this IServiceCollection services,
        RedisConfiguration redisConfiguration)
        where T : class, ISerializer
    {
        return services.AddStackExchangeRedisExtensions<T>(new[] { redisConfiguration });
    }

    /// <summary>
    /// Add StackExchange.Redis with its serialization provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="redisConfiguration">The redis configration.</param>
    /// <typeparam name="T">The typof of serializer. <see cref="ISerializer" />.</typeparam>
    public static IServiceCollection AddStackExchangeRedisExtensions<T>(
        this IServiceCollection services,
        IEnumerable<RedisConfiguration> redisConfiguration)
        where T : class, ISerializer
    {
        if (redisConfiguration is not RedisConfiguration[] configs)
            configs = redisConfiguration.ToArray();

        var defaultConnectionName = ValidateConfigurationNames(configs);
        System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(defaultConnectionName));

        // Initialize the connections before app runs
        var redisConnPools = configs
            //.AsParallel()
            .Select(c => new RedisConnectionPool(c))
            .ToArray();
        services.AddSingleton(redisConnPools);

        services.AddSingleton<IRedisClientFactory, RedisClientFactory>();
        services.AddSingleton<ISerializer, T>();

        services.AddSingleton((provider) => provider
            .GetRequiredService<IRedisClientFactory>()
            .GetDefaultRedisClient());

        services.AddSingleton((provider) => provider
            .GetRequiredService<IRedisClient>()
            .GetDefaultDatabase());

        return services;
    }

    private static string ValidateConfigurationNames(RedisConfiguration[] configs)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(configs);
#else
        if (configs is null)
            throw new ArgumentNullException(nameof(configs));
#endif
        var defaultConnectionName = default(string);

        switch (configs.Length)
        {
            case 0:
                throw new ArgumentException(nameof(configs), "Empty configuration.");

            case 1:
                configs[0].IsDefault = true;
                defaultConnectionName = CheckConfigName(configs[0]);

                break;

            default:
                {
                    var configNameSet = new HashSet<string>();
                    foreach (var configuration in configs)
                    {
                        var configName = CheckConfigName(configuration);

                        if (!configNameSet.Add(configName))
                            throw new ArgumentException($"{nameof(RedisConfiguration.Name)} must be unique");

                        if (configuration.IsDefault)
                        {
                            if (defaultConnectionName != null)
                                throw new ArgumentException("There is more than one default configuration. Only one default configuration is allowed.");

                            defaultConnectionName = configName;
                        }
                    }

                    if (defaultConnectionName is null)
                        throw new ArgumentException("There is no default configuration. At least one default configuration is required.");
                }
                break;
        }

        return defaultConnectionName;

        static string CheckConfigName(RedisConfiguration conf)
        {
            if (string.IsNullOrEmpty(conf.Name))
                conf.Name = Guid.NewGuid().ToString(); // Do we really need to output a warning log about this?

            return conf.Name;
        }
    }
}
