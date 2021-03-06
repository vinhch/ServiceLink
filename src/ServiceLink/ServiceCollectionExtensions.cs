﻿using System;
using Microsoft.Extensions.DependencyInjection;
using ServiceLink.Bus;

namespace ServiceLink
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseServiceLink<TSerializer, TDeserializer>(this IServiceCollection collection)
            where TSerializer : IBusSerializer
            where TDeserializer : IBusDeserializer
        {
            collection.AddTransient(typeof(Scoped<>));
            collection.AddSingleton<Publisher<TSerializer,TDeserializer>, Publisher<TSerializer, TDeserializer>>();
            collection.AddSingleton<IPublisher>(c => c.GetService<Publisher<TSerializer, TDeserializer>>());
            collection.AddSingleton<IWorker>(c => c.GetService<Publisher<TSerializer, TDeserializer>>());
            //collection.AddSingleton<IBusContractResolver>(_ => new AttributeContractResolver());
            return collection;
        }

        public static IServiceCollection MapPublisher<TSource, TTarget>(this IServiceCollection collection)
        {
            collection.AddTransient<IPublisher<TSource>>(
                c => new MappedPublisher<TSource, TTarget>(c.GetRequiredService<IPublisher<TTarget>>(), 
                c.GetRequiredService<IMapper<TSource, TTarget>>().Map));
            return collection;
        }

        public static IServiceCollection MapPublisher<TSource, TTarget>(this IServiceCollection collection, Func<TSource, TTarget> mapper)
        {
            collection.AddTransient<IPublisher<TSource>>(
                c => new MappedPublisher<TSource, TTarget>(c.GetRequiredService<IPublisher<TTarget>>(), mapper));
            return collection;
        }



    }
}