// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Infrastructure/SimpleContainer.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;

namespace CMPCodeDatabase.Core.Infrastructure
{
    public sealed class SimpleContainer
    {
        private readonly Dictionary<Type, Func<object>> _map = new();

        public void RegisterSingleton<T>(Func<T> factory) where T : class
        {
            T? instance = null;
            _map[typeof(T)] = () => instance ??= factory();
        }

        public T Resolve<T>() where T : class
        {
            if (_map.TryGetValue(typeof(T), out var f)) return (T)f();
            throw new InvalidOperationException($"Type not registered: {typeof(T).FullName}");
        }
    }
}
