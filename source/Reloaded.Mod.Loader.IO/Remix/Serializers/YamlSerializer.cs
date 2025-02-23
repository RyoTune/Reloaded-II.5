using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace Reloaded.Mod.Loader.IO.Remix.Serializers;

internal static class YamlSerializer
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .DisableAliases()
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public static T DeserializeFile<T>(string filePath) => Deserializer.Deserialize<T>(File.ReadAllText(filePath))
        ?? throw new Exception($"Failed to deserialize file.\nFile: {filePath}");

    public static void SerializeFile<T>(string filePath, T obj) => File.WriteAllText(filePath, Serializer.Serialize(obj));
}