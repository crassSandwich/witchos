﻿using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace WitchOS
{
    [Serializable, DataContract]
    public class SaveableSpellReference : SaveableScriptableObjectReference<Spell> { }
}
