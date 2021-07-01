// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// Shared logic for parsing tokens from route values and querystring values.
    /// </summary>
    internal abstract class UrlValueConstraint
    {
        private static readonly ConcurrentDictionary<Type, UrlValueConstraint> _cachedInstances = new();

        public static bool TryGetByTargetType(Type targetType, [MaybeNullWhen(false)] out UrlValueConstraint result)
        {
            if (!_cachedInstances.TryGetValue(targetType, out result))
            {
                result = Create(targetType);
                if (result is null)
                {
                    return false;
                }

                _cachedInstances.TryAdd(targetType, result);
            }

            return true;
        }

        private static UrlValueConstraint? Create(Type targetType)
        {
            if (targetType == typeof(string))
            {
                return new TypedUrlValueConstraint<string>((ReadOnlySpan<char> str, out string result) =>
                {
                    result = str.ToString();
                    return true;
                });
            }
            else if (targetType == typeof(bool))
            {
                return new TypedUrlValueConstraint<bool>(bool.TryParse);
            }
            else if (targetType == typeof(DateTime))
            {
                return new TypedUrlValueConstraint<DateTime>((ReadOnlySpan<char> str, out DateTime result) =>
                    DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result));
            }
            else if (targetType == typeof(decimal))
            {
                return new TypedUrlValueConstraint<decimal>((ReadOnlySpan<char> str, out decimal result) =>
                    decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result));
            }
            else if (targetType == typeof(double))
            {
                return new TypedUrlValueConstraint<double>((ReadOnlySpan<char> str, out double result) =>
                    double.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result));
            }
            else if (targetType == typeof(float))
            {
                return new TypedUrlValueConstraint<float>((ReadOnlySpan<char> str, out float result) =>
                    float.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result));
            }
            else if (targetType == typeof(Guid))
            {
                return new TypedUrlValueConstraint<Guid>(Guid.TryParse);
            }
            else if (targetType == typeof(int))
            {
                return new TypedUrlValueConstraint<int>((ReadOnlySpan<char> str, out int result) =>
                    int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result));
            }
            else if (targetType == typeof(long))
            {
                return new TypedUrlValueConstraint<long>((ReadOnlySpan<char> str, out long result) =>
                    long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result));
            }

            return null;
        }

        public abstract bool TryParseUntyped(ReadOnlySpan<char> value, [MaybeNullWhen(false)] out object result);

        public abstract bool TryAppendListValue(object? existingList, string value, [MaybeNullWhen(false)] out object updatedList);

        public abstract object ToArray(object value);

        private class TypedUrlValueConstraint<T> : UrlValueConstraint
        {
            public delegate bool TryParseDelegate(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out T result);

            private readonly TryParseDelegate _parser;

            public TypedUrlValueConstraint(TryParseDelegate parser)
            {
                _parser = parser;
            }

            public override bool TryParseUntyped(ReadOnlySpan<char> value, [MaybeNullWhen(false)] out object result)
            {
                if (_parser(value, out var typedResult))
                {
                    result = typedResult!;
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }

            public override bool TryAppendListValue(object? existingList, string value, [MaybeNullWhen(false)] out object updatedList)
            {
                if (_parser(value, out var typedResult))
                {
                    var list = existingList as List<T> ?? new List<T>();
                    list.Add(typedResult);
                    updatedList = list;
                    return true;
                }
                else
                {
                    updatedList = null;
                    return false;
                }
            }

            public override object ToArray(object value)
                => ((List<T>)value).ToArray();

            public override string ToString() => typeof(T) switch
            {
                var x when x == typeof(bool) => "bool",
                var x when x == typeof(DateTime) => "datetime",
                var x when x == typeof(decimal) => "decimal",
                var x when x == typeof(double) => "double",
                var x when x == typeof(float) => "float",
                var x when x == typeof(Guid) => "guid",
                var x when x == typeof(int) => "int",
                var x when x == typeof(long) => "long",
                var x => x.Name.ToLowerInvariant()
            };
        }
    }
}