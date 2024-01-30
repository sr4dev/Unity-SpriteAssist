using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    /// <summary>
    /// The include/exclude mode for SpriteAssist.
    /// </summary>
    public enum SpriteAssistInclusionMode
    {
        /// <summary>
        /// All sprites except those listed in settings will be processed
        /// </summary>
        Exclude,

        /// <summary>
        /// Only sprites explicitly matched by those in settings will be processed
        /// </summary>
        Include
    }
}
