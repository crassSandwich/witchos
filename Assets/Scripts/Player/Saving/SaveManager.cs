﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace WitchOS
{
#if !UNITY_WEBGL
    [CreateAssetMenu(menuName = "WitchOS/Save Manager", fileName = "SaveManagerSystem.asset")]
    public class SaveManager : ScriptableObject
    {
        HashSet<SaveData> allDataObjects = new HashSet<SaveData>();

        public virtual void Register (SaveData saveData)
        {
            if (allDataObjects.Contains(saveData)) return;

            if (allDataObjects.Any(s => s.FileName == saveData.FileName))
            {
                throw new InvalidOperationException($"cannot register two save files with the same name ({saveData.FileName})");
            }

            allDataObjects.Add(saveData);
        }

        public virtual void SaveAllData ()
        {
            foreach (var dataObject in allDataObjects)
            {
                dataObject.WriteDataToFile();
            }
        }

        public virtual void DeleteAllSaveData ()
        {
            foreach (var dataObject in allDataObjects)
            {
                dataObject.DeleteSaveFile();
            }
        }
    }
#elif UNITY_WEBGL
    public class SaveManager : ScriptableObject
    {
        public virtual void Register (SaveData saveData) { }

        public virtual void SaveAllData () { }

        public virtual void DeleteAllSaveData () { }
    }
#endif // UNITY_WEBGL
}
