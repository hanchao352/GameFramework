// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2025 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] A welcome screen for <see cref="Animancer"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerReadMe
    /// 
    // [CreateAssetMenu]
    [AnimancerHelpUrl(typeof(AnimancerReadMe))]
    public class AnimancerReadMe : ReadMe
    {
        /************************************************************************************************************************/

        /// <summary>The release ID of the current version.</summary>
        /// <example><list type="bullet">
        /// <item>[ 1] = v1.0.0: 2018-05-02.</item>
        /// <item>[ 2] = v1.1.0: 2018-05-29.</item>
        /// <item>[ 3] = v1.2.0: 2018-08-14.</item>
        /// <item>[ 4] = v1.3.0: 2018-09-12.</item>
        /// <item>[ 5] = v2.0.0: 2018-10-08.</item>
        /// <item>[ 6] = v3.0.0: 2019-05-27.</item>
        /// <item>[ 7] = v3.1.0: 2019-08-12.</item>
        /// <item>[ 8] = v4.0.0: 2020-01-28.</item>
        /// <item>[ 9] = v4.1.0: 2020-02-21.</item>
        /// <item>[10] = v4.2.0: 2020-03-02.</item>
        /// <item>[11] = v4.3.0: 2020-03-13.</item>
        /// <item>[12] = v4.4.0: 2020-03-27.</item>
        /// <item>[13] = v5.0.0: 2020-07-17.</item>
        /// <item>[14] = v5.1.0: 2020-07-27.</item>
        /// <item>[15] = v5.2.0: 2020-09-16.</item>
        /// <item>[16] = v5.3.0: 2020-10-06.</item>
        /// <item>[17] = v6.0.0: 2020-12-04.</item>
        /// <item>[18] = v6.1.0: 2021-04-13.</item>
        /// <item>[19] = v7.0.0: 2021-07-29.</item>
        /// <item>[20] = v7.1.0: 2021-08-13.</item>
        /// <item>[21] = v7.2.0: 2021-10-17.</item>
        /// <item>[22] = v7.3.0: 2022-07-03.</item>
        /// <item>[23] = v7.4.0: 2023-01-26.</item>
        /// <item>[24] = v7.4.1: 2023-01-28.</item>
        /// <item>[25] = v7.4.2: 2023-01-31.</item>
        /// <item>[26] = v7.4.3: 2023-04-16.</item>
        /// <item>[27] = v8.0.0: 2024-08-17.</item>
        /// <item>[28] = v8.0.1: 2024-09-08.</item>
        /// <item>[29] = v8.0.2: 2024-11-02.</item>
        /// <item>[30] = v8.1.0: 2025-02-26.</item>
        /// <item>[31] = v8.1.1: 2025-05-26.</item>
        /// </list></example>
        public override int ReleaseNumber => 31;

        /// <inheritdoc/>
        public override string VersionName => Strings.DocsURLs.VersionName;

        /// <inheritdoc/>
        public override string PrefKey => nameof(Animancer);

        /// <inheritdoc/>
        public override string BaseProductName => Strings.ProductName;

        /// <inheritdoc/>
        public override string ProductName => Strings.ProductName + " Pro";

        /// <inheritdoc/>
        public override string DocumentationURL => Strings.DocsURLs.Documentation;

        /// <inheritdoc/>
        public override string ChangeLogURL => Strings.DocsURLs.ChangeLogURL;

        /// <inheritdoc/>
        public override string SamplesURL => Strings.DocsURLs.Samples;

        /// <inheritdoc/>
        public override string UpdateURL => Strings.DocsURLs.LatestVersion;

        /************************************************************************************************************************/

        public AnimancerReadMe() : base(
            new("Issues",
                "for questions, suggestions, and bug reports",
                Strings.DocsURLs.Issues),
            new("Discussions",
                "for general discussions, feedback, and news",
                Strings.DocsURLs.Discussions),
            new("Email",
                "for anything private",
                GetEmailURL(Strings.DocsURLs.DeveloperEmail, Strings.ProductName),
                Strings.DocsURLs.DeveloperEmail))
        {
            ExtraSamples = new LinkSection[]
            {
                new("Platformer Game Kit", null, "https://kybernetik.com.au/platformer"),
            };
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] A custom Inspector for <see cref="AnimancerReadMe"/>.</summary>
        [CustomEditor(typeof(AnimancerReadMe), editorForChildClasses: true)]
        public new class Editor : ReadMe.Editor
        {
            /************************************************************************************************************************/

            /// <summary>A callback to execute data migration.</summary>
            public static event Action<string> MigrateOldAssetData;

            /************************************************************************************************************************/

            /// <inheritdoc/>
            protected override void DoNewVersionDetails()
            {
                base.DoNewVersionDetails();

                if (MigrateOldAssetData == null)
                    return;

                var text = $"Migrate old asset data to {Target.BaseProductName} {Target.VersionName}";
                if (GUILayout.Button(text))
                    MigrateOldAssetData(text);
            }

            /************************************************************************************************************************/
        }
    }
}

#endif

