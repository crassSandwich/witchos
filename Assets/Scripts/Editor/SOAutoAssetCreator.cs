﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using crass;

namespace WitchOS
{
    public class SOAutoAssetCreator : MonoBehaviour
    {
        // for every type in this list, we find every non-abstract subclass of that type, and instantiate a single ScriptableObject asset for that type
        public static readonly List<Type> TYPES = new List<Type>
        {
            typeof(TerminalCommand), typeof(Spell)
        };

        public static readonly string BASE_ASSET_PATH = Path.Combine("Assets", "ScriptableObjects", "AutoGenerated");

        [MenuItem("Tools/Generate assets for SO Auto Asset classes")] // for manual use
        [UnityEditor.Callbacks.DidReloadScripts] // automatic trigger every compilation
        public static void AutoGenerateAssets ()
        {
            bool needToSave = false;

            string[] assetFolders = TYPES.Select(t => Path.Combine(BASE_ASSET_PATH, t.Name + "s")).ToArray();

            for (int i = 0; i < TYPES.Count; i++)
            {
                Type superType = TYPES[i];

                if (!superType.IsSubclassOf(typeof(ScriptableObject)))
                    throw new InvalidOperationException($"type {superType.Name} is not a ScriptableObject, so we can't make any assets for it; aborting SO creation");

                foreach (Type subType in Reflection.GetImplementations(superType))
                {
                    int numExistingAssets = AssetDatabase.FindAssets(subType.Name, assetFolders).Length;

                    if (numExistingAssets == 1) continue;
                    else if (numExistingAssets > 1) throw new InvalidOperationException($"there are too many instances of {subType.Name}; aborting SO creation");

                    var command = ScriptableObject.CreateInstance(subType);
                    AssetDatabase.CreateAsset(command, Path.Combine(assetFolders[i], subType.Name + ".asset"));

                    needToSave = true;
                }
            }

            // note: due to a known issue, the SaveAssets call throws an error in play mode
            // see https://issuetracker.unity3d.com/issues/assetdatabase-dot-saveassets-throws-an-exception-the-specified-path-is-not-of-a-legal-form-empty-while-in-play-mode
            if (needToSave) AssetDatabase.SaveAssets();
        }
    }
}
