// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Models/CollectorItemMeta.cs
// Purpose: Sidecar metadata for Collector entries (used by PNACH export).
// Notes:
//  • This is NOT shown in the Collector UI.
//  • Exporters may optionally use it (currently PNACH only).
// Added: 2026-03-19
// ─────────────────────────────────────────────────────────────────────────────

namespace CMPCodeDatabase.Core.Models
{
    /// <summary>
    /// Optional metadata captured when a code is added to the Collector.
    /// </summary>
    public readonly record struct CollectorItemMeta(string? Author, string? Description);
}
