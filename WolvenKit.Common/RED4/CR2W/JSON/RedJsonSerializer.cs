#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;
using Semver;
using WolvenKit.Common.Conversion;
using WolvenKit.RED4.Types;

namespace WolvenKit.RED4.CR2W.JSON;

public static class RedJsonSerializer
{
    private static readonly ReferenceResolver<RedBuffer> s_bufferResolver;
    private static readonly ReferenceResolver<RedBaseClass> s_classResolver;

    private static readonly ConcurrentDictionary<int, JsonHeader> s_threadedStorage = new();

    static RedJsonSerializer()
    {
        s_bufferResolver = new();
        s_classResolver = new();

        Options = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true,
            MaxDepth = 2048,
            Converters =
            {
                new CBoolConverter(),
                new CDoubleConverter(),
                new CFloatConverter(),
                new CInt8Converter(),
                new CInt16Converter(),
                new CInt32Converter(),
                new CInt64Converter(),
                new CUInt8Converter(),
                new CUInt16Converter(),
                new CUInt32Converter(),
                new CUInt64Converter(),
                new CDateTimeConverter(),
                new CGuidConverter(),
                new CNameConverter(),
                new CRUIDConverter(),
                new CStringConverter(),
                new CVariantConverter(),
                new BufferConverterFactory(s_bufferResolver),
                new LocalizationStringConverter(),
                new CLegacySingleChannelCurveConverterFactory(),
                new MultiChannelCurveConverterFactory(),
                new ResourcePathConverter(),
                new NodeRefConverter(),
                new TweakDBIDConverter(),
                new CByteArrayConverter(),
                new CKeyValuePairConverter(),
                new ArrayConverterFactory(),
                new EnumConverterFactory(),
                new HandleConverterFactory(s_classResolver),
                new ResourceConverterFactory(),
                new ClassConverterFactory(s_classResolver),
                new Red4FileConverterFactory(s_classResolver),
                new SemVersionConverter(),
                new RedFileDtoConverter(s_classResolver),

                new ParseableBufferConverter(),
                new CollisionShapeConverter()
            }
        };
    }

    internal static void SetHeader(JsonHeader header) =>
        s_threadedStorage[Environment.CurrentManagedThreadId] = header;

    internal static void SetVersion(SemVersion version) =>
        s_threadedStorage[Environment.CurrentManagedThreadId] = new JsonHeader { WKitJsonVersion = version };

    internal static string GetDataType() =>
        s_threadedStorage[Environment.CurrentManagedThreadId].DataType;

    internal static bool IsOlderThen(string version) =>
        s_threadedStorage[Environment.CurrentManagedThreadId]
            .WKitJsonVersion
            .CompareSortOrderTo(SemVersion.Parse(version, SemVersionStyles.Strict)) < 0;

    internal static bool IsNewerThen(string version) =>
        IsNewerThen(SemVersion.Parse(version, SemVersionStyles.Strict));

    internal static bool IsNewerThen(SemVersion version) =>
        s_threadedStorage[Environment.CurrentManagedThreadId]
            .WKitJsonVersion
            .CompareSortOrderTo(version) > 0;

    internal static bool IsVersion(string verStr) =>
        s_threadedStorage[Environment.CurrentManagedThreadId].WKitJsonVersion == SemVersion.Parse(verStr, SemVersionStyles.Strict);

    public static JsonSerializerOptions Options { get; }

    public static string Serialize(object value, bool skipHeader = false)
    {
        s_bufferResolver.Begin();
        s_classResolver.Begin();

        if (skipHeader)
        {
            foreach (var c in Options.Converters)
            {
                if (c is RedFileDtoConverter red)
                {
                    red.SetSkipHeader(skipHeader);
                }
            }
        }

        var result = JsonSerializer.Serialize(value, Options);

        CleanUp();

        return result;
    }


    public static bool TryDeserialize<T>(string json, out T? result)
    {
        try
        {
            result = Deserialize<T>(json);
            return true;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }

    public static T? Deserialize<T>(string json)
    {
        s_bufferResolver.Begin();
        s_classResolver.Begin();

        SetHeader(new JsonHeader());

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        finally
        {
            CleanUp();
        }
    }

    public static object? Deserialize(Type type, JsonElement element)
    {
        s_bufferResolver.Begin();
        s_classResolver.Begin();

        SetHeader(new JsonHeader());

        if (element.ValueKind == JsonValueKind.Object && !element.TryGetProperty("$type", out _))
        {
            SetVersion(SemVersion.Parse("0.0.1", SemVersionStyles.Strict));
        }

        try
        {
            return element.Deserialize(type, Options);
        }
        finally
        {
            CleanUp();
        }
    }

    private static void CleanUp()
    {
        s_bufferResolver.End();
        s_classResolver.End();

        s_threadedStorage.TryRemove(Environment.CurrentManagedThreadId, out _);
    }
}
