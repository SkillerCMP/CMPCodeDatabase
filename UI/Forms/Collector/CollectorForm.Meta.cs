using System;
using System.Collections.Generic;
using System.Linq;

namespace CMPCodeDatabase
{
    public partial class CollectorControl
    {
        // Sidecar metadata per collected entry (not shown in UI)
        private readonly Dictionary<string, CMPCodeDatabase.Core.Models.CollectorItemMeta> collectorMetaMap =
            new(StringComparer.OrdinalIgnoreCase);

        public void AddItem(string name, string code, string? author, string? description)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            // Ensure the item exists in the collector (code map + UI list)
            if (!collectorCodeMap.ContainsKey(name))
                AddItem(name, code);

            // Store metadata (even if the entry already existed)
            if (string.IsNullOrWhiteSpace(author) && string.IsNullOrWhiteSpace(description))
                return;

            collectorMetaMap[name] = new CMPCodeDatabase.Core.Models.CollectorItemMeta(
                string.IsNullOrWhiteSpace(author) ? null : author!.Trim(),
                string.IsNullOrWhiteSpace(description) ? null : description!.Trim()
            );
        }

        private bool TryGetMeta(string name, out CMPCodeDatabase.Core.Models.CollectorItemMeta meta) =>
            collectorMetaMap.TryGetValue(name, out meta);

        private void ClearMeta()
        {
            try { collectorMetaMap.Clear(); } catch { }
        }

        private static IEnumerable<string> SplitPeople(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) yield break;
            foreach (var who in csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var s = who.Trim();
                if (s.Length > 0) yield return s;
            }
        }

        private static string SingleLineBasic(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            // Keep it simple for author=/description= fields.
            return string.Join(" ", s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        private string? GetPnachAuthorForEntry(string name)
        {
            if (TryGetMeta(name, out var meta) && !string.IsNullOrWhiteSpace(meta.Author))
                return SingleLineBasic(meta.Author);
            return null;
        }

        private string? GetPnachDescriptionForEntry(string name)
        {
            if (!CMPCodeDatabase.Core.Settings.AppSettings.Instance.PnachExportNotesAsDescription)
                return null;

            if (TryGetMeta(name, out var meta) && !string.IsNullOrWhiteSpace(meta.Description))
                return SingleLineBasic(meta.Description);

            return null;
        }
    }
}
