using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMPCodeDatabase;

public static partial class DatabaseManager
{
    public sealed class ManifestRoot
{
    public int SchemaVersion { get; set; }
    public string? GeneratedUtc { get; set; }

    // Supports either:
    //  - "generator": "SomeTool 1.2.3"
    //  - "generator": { "name": "SomeTool", "version": "1.2.3" }
    [JsonConverter(typeof(ManifestGeneratorConverter))]
    public ManifestGenerator? Generator { get; set; }

    public List<ManifestDatabase> Databases { get; set; } = new();
}

public sealed class ManifestGenerator
{
    public string? Name { get; set; }
    public string? Version { get; set; }
}

private sealed class ManifestGeneratorConverter : JsonConverter<ManifestGenerator?>
{
    public override ManifestGenerator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return null;

            // Best-effort parse: treat the last token as version if it looks version-ish.
            var parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return new ManifestGenerator { Name = parts[0] };

            var last = parts[^1];
            var name = string.Join(" ", parts[..^1]);
            return new ManifestGenerator { Name = name, Version = last };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var obj = doc.RootElement;

            var gen = new ManifestGenerator();

            if (obj.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                gen.Name = n.GetString();

            if (obj.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String)
                gen.Version = v.GetString();

            // If it was an object but empty/unknown, treat as null.
            if (string.IsNullOrWhiteSpace(gen.Name) && string.IsNullOrWhiteSpace(gen.Version))
                return null;

            return gen;
        }

        throw new JsonException("Invalid generator value.");
    }

    public override void Write(Utf8JsonWriter writer, ManifestGenerator? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        if (!string.IsNullOrWhiteSpace(value.Name))
            writer.WriteString("name", value.Name);
        if (!string.IsNullOrWhiteSpace(value.Version))
            writer.WriteString("version", value.Version);
        writer.WriteEndObject();
    }
}

public sealed class ManifestDatabase
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int FileCount { get; set; }
        public ManifestDownload? Download { get; set; }
        public List<ManifestFile> Files { get; set; } = new();
    }

    public sealed class ManifestDownload
    {
        public string Type { get; set; } = "";
        public string? Url { get; set; }
    }

    public sealed class ManifestFile
    {
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public string Sha1 { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public sealed record UpdateInfo(string DatabaseName, int ChangedFileCount, IReadOnlyList<string> ChangedFiles);

}
