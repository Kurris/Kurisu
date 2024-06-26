﻿using Kurisu.Aspect.Reflection.Reflectors;

namespace Kurisu.Aspect.Reflection.Extensions;

internal static class CustomAttributeExtensions
{
    private static readonly Attribute[] _empty = Array.Empty<Attribute>();

    public static Attribute[] GetCustomAttributes(this ICustomAttributeReflectorProvider customAttributeReflectorProvider)
    {
        if (customAttributeReflectorProvider == null)
        {
            throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        }

        var customAttributeReflectors = customAttributeReflectorProvider.CustomAttributeReflectors;
        var customAttributeLength = customAttributeReflectors.Length;
        if (customAttributeLength == 0)
        {
            return _empty;
        }

        var attrs = new Attribute[customAttributeLength];
        for (var i = 0; i < customAttributeLength; i++)
        {
            attrs[i] = customAttributeReflectors[i].Invoke();
        }

        return attrs;
    }

    public static Attribute[] GetCustomAttributes(this ICustomAttributeReflectorProvider customAttributeReflectorProvider, Type attributeType)
    {
        if (customAttributeReflectorProvider == null)
        {
            throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        }

        if (attributeType == null)
        {
            throw new ArgumentNullException(nameof(attributeType));
        }

        var customAttributeReflectors = customAttributeReflectorProvider.CustomAttributeReflectors;
        var customAttributeLength = customAttributeReflectors.Length;
        if (customAttributeLength == 0)
        {
            return _empty;
        }

        var checkedAttrs = new Attribute[customAttributeLength];
        var @checked = 0;
        var attrToken = attributeType.TypeHandle;
        for (var i = 0; i < customAttributeLength; i++)
        {
            var reflector = customAttributeReflectors[i];
            if (reflector.Tokens.Contains(attrToken))
                checkedAttrs[@checked++] = reflector.Invoke();
        }

        if (customAttributeLength == @checked)
        {
            return checkedAttrs;
        }

        var attrs = new Attribute[@checked];
        Array.Copy(checkedAttrs, attrs, @checked);
        return attrs;
    }

    public static TAttribute[] GetCustomAttributes<TAttribute>(this ICustomAttributeReflectorProvider customAttributeReflectorProvider)
        where TAttribute : Attribute
    {
        if (customAttributeReflectorProvider == null)
        {
            throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        }

        var customAttributeReflectors = customAttributeReflectorProvider.CustomAttributeReflectors;
        var customAttributeLength = customAttributeReflectors.Length;
        if (customAttributeLength == 0)
        {
            return Array.Empty<TAttribute>();
        }

        var checkedAttrs = new TAttribute[customAttributeLength];
        var @checked = 0;
        var attrToken = typeof(TAttribute).TypeHandle;
        for (var i = 0; i < customAttributeLength; i++)
        {
            var reflector = customAttributeReflectors[i];
            if (reflector.Tokens.Contains(attrToken))
                checkedAttrs[@checked++] = (TAttribute)reflector.Invoke();
        }

        if (customAttributeLength == @checked)
        {
            return checkedAttrs;
        }

        var attrs = new TAttribute[@checked];
        Array.Copy(checkedAttrs, attrs, @checked);
        return attrs;
    }

    public static Attribute GetCustomAttribute(this ICustomAttributeReflectorProvider customAttributeReflectorProvider, Type attributeType)
    {
        if (customAttributeReflectorProvider == null)
        {
            throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        }

        if (attributeType == null)
        {
            throw new ArgumentNullException(nameof(attributeType));
        }

        var customAttributeReflectors = customAttributeReflectorProvider.CustomAttributeReflectors;
        var customAttributeLength = customAttributeReflectors.Length;
        if (customAttributeLength == 0)
        {
            return null;
        }

        var attrToken = attributeType.TypeHandle;
        for (var i = 0; i < customAttributeLength; i++)
        {
            var reflector = customAttributeReflectors[i];
            if (reflector.Tokens.Contains(attrToken))
            {
                return customAttributeReflectors[i].Invoke();
            }
        }

        return null;
    }

    public static TAttribute GetCustomAttribute<TAttribute>(this ICustomAttributeReflectorProvider customAttributeReflectorProvider)
        where TAttribute : Attribute
    {
        return (TAttribute)GetCustomAttribute(customAttributeReflectorProvider, typeof(TAttribute));
    }

    public static bool IsDefined(this ICustomAttributeReflectorProvider customAttributeReflectorProvider, Type attributeType)
    {
        if (customAttributeReflectorProvider == null)
        {
            throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        }

        if (attributeType == null)
        {
            throw new ArgumentNullException(nameof(attributeType));
        }

        var customAttributeReflectors = customAttributeReflectorProvider.CustomAttributeReflectors;
        var customAttributeLength = customAttributeReflectors.Length;
        if (customAttributeLength == 0)
        {
            return false;
        }

        var attrToken = attributeType.TypeHandle;
        for (var i = 0; i < customAttributeLength; i++)
        {
            if (customAttributeReflectors[i].Tokens.Contains(attrToken))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsDefined<TAttribute>(this ICustomAttributeReflectorProvider customAttributeReflectorProvider) where TAttribute : Attribute
    {
        return IsDefined(customAttributeReflectorProvider, typeof(TAttribute));
    }
}