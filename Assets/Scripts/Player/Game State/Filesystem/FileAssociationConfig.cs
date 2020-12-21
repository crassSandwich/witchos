﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchOS
{
    [CreateAssetMenu(menuName = "WitchOS/File Association Configuration", fileName = "NewFileAssociation.asset")]
    public class FileAssociationConfig : ScriptableObject
    {
        [Serializable]
        public class FileAssociationData
        {
            public string FullNameOfFileDataType;
            public WindowMetadata Metadata;
        }

        public List<FileAssociationData> Config;

        public WindowMetadata GetMetadataForFile (FileBase file)
        {
            return Config.FirstOrDefault(d => file.GetTypeOfData().FullName == d.FullNameOfFileDataType)?.Metadata;
        }
    }
}
