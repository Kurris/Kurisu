using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace Kurisu.DataAccess.Extensions;

public static class ValueConversionExtensions
{
    /// <summary>
    /// json转换器
    /// </summary>
    /// <remarks>
    /// efCore7已经支持json列
    /// </remarks>
    /// <param name="propertyBuilder"></param>
    /// <typeparam name="T">List{Class}</typeparam>
    /// <returns></returns>
    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder)
        where T : class, new()
    {
        return propertyBuilder.HasJsonConversion(new T());
    }


    /// <summary>
    /// json转换器
    /// </summary>
    /// <remarks>
    /// efCore7已经支持json列
    /// </remarks>
    /// <param name="propertyBuilder"></param>
    /// <param name="defaultValue"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder, T defaultValue)
        where T : class, new()
    {
        var converter = new ValueConverter<T, string>
        (
            v => JsonConvert.SerializeObject(v),
            v => JsonConvert.DeserializeObject<T>(v) ?? defaultValue
        );

        var comparer = new ValueComparer<T>
        (
            (l, r) => JsonConvert.SerializeObject(l) == JsonConvert.SerializeObject(r),
            v => v == null ? 0 : JsonConvert.SerializeObject(v).GetHashCode(),
            v => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v))
        );

        propertyBuilder
            .HasConversion(converter, comparer)
            .HasColumnType("JSON");

        return propertyBuilder;
    }

    /// <summary>
    /// json转换器
    /// </summary>
    /// <remarks>
    /// efCore7已经支持json列
    /// </remarks>
    /// <param name="propertyBuilder"></param>
    /// <typeparam name="T">List{int} List{string}</typeparam>
    /// <returns></returns>
    public static PropertyBuilder<List<T>> HasJsonConversion<T>(this PropertyBuilder<List<T>> propertyBuilder)
    {
        return propertyBuilder.HasJsonConversion(new List<T>());
    }


    /// <summary>
    /// json转换器
    /// </summary>
    /// <remarks>
    /// efCore7已经支持json列
    /// </remarks>
    /// <param name="propertyBuilder"></param>
    /// <param name="defaultValue"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static PropertyBuilder<List<T>> HasJsonConversion<T>(this PropertyBuilder<List<T>> propertyBuilder, List<T> defaultValue)
    {
        var converter = new ValueConverter<List<T>, string>
        (
            v => JsonConvert.SerializeObject(v),
            v => JsonConvert.DeserializeObject<List<T>>(v) ?? defaultValue
        );

        var comparer = new ValueComparer<List<T>>(
            (l, r) => l.SequenceEqual(r),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()
        );

        propertyBuilder
            .HasConversion(converter, comparer)
            .HasColumnType("JSON");

        return propertyBuilder;
    }
}