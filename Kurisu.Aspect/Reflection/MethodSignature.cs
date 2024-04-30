﻿using System.Collections.Concurrent;
using System.Reflection;
using Kurisu.Aspect.Core.Utils;

namespace Kurisu.Aspect.Reflection;

internal struct MethodSignature
{
    private static readonly ConcurrentDictionary<Tuple<MethodBase, string>, int> _signatures = new();

    private readonly int _signature;
    public int Value => _signature;

    public string Name { get; set; }

    public MethodSignature(MethodBase method)
        : this(method, method?.Name)
    {
    }

    public MethodSignature(MethodBase method, string name)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        Name = name;
        _signature = _signatures.GetOrAdd(Tuple.Create(method, name), GetSignatureCode);
    }

    public override bool Equals(object obj)
    {
        if (obj is MethodSignature signature)
        {
            return _signature == signature._signature;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return _signature;
    }

    public static bool operator !=(MethodSignature signature, MethodSignature other)
    {
        return signature._signature != other._signature;
    }

    public static bool operator ==(MethodSignature signature, MethodSignature other)
    {
        return signature._signature == other._signature;
    }

    private static int GetSignatureCode(Tuple<MethodBase, string> pair)
    {
        var method = pair.Item1;
        var name = pair.Item2 ?? method.Name;

        unchecked
        {
            var signatureCode = name.GetHashCode();
            var parameterTypes = method.GetParameterTypes();
            if (parameterTypes.Length > 0)
            {
                signatureCode = (signatureCode * 397) ^ parameterTypes.Length.GetHashCode();
                foreach (var parameterType in parameterTypes)
                {
                    if (parameterType.IsGenericParameter)
                    {
                        continue;
                    }

                    if (parameterType.GetTypeInfo().IsGenericType)
                    {
                        signatureCode = GetSignatureCode(signatureCode, parameterType);
                    }
                    else
                    {
                        signatureCode = (signatureCode * 397) ^ parameterType.GetHashCode();
                    }
                }
            }

            if (method.IsGenericMethod)
            {
                signatureCode = (signatureCode * 397) ^ method.GetGenericArguments().Length.GetHashCode();
            }

            return signatureCode;
        }
    }

    private static int GetSignatureCode(int signatureCode, Type genericType)
    {
        signatureCode = (signatureCode * 397) ^ genericType.GetGenericTypeDefinition().GetHashCode();
        signatureCode = (signatureCode * 397) ^ genericType.GenericTypeArguments.Length.GetHashCode();
        foreach (var argument in genericType.GenericTypeArguments)
        {
            if (argument.IsGenericParameter)
            {
                continue;
            }

            if (argument.GetTypeInfo().IsGenericType)
            {
                signatureCode = GetSignatureCode(signatureCode, argument);
            }
            else
            {
                signatureCode = (signatureCode * 397) ^ argument.GetHashCode();
            }
        }

        return signatureCode;
    }
}