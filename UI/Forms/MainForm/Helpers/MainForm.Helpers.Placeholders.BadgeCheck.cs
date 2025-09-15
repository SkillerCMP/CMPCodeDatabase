// Optional tiny wrapper used by MainForm.Database post-pass.
// Lets Database code stay simple while using the declared-mod-aware, angle-safe logic.
using System.Collections.Generic;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        private static bool ShouldShowModBadgeNormalized(string codeText, ISet<string> declaredModNames)
            => HasUnresolvedPlaceholders_ModsAware(codeText, declaredModNames);
    }
}
