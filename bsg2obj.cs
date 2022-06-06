using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Modding;
using InternalModding;
using Modding.Common;
using UnityEngine;

namespace bsg2obj
{
	public class bsg2obj : ModEntryPoint
	{
        public static GameObject mod;
        public override void OnLoad()
        {
            mod = new GameObject("bsg2obj tool");
            UnityEngine.Object.DontDestroyOnLoad(mod);
            mod.AddComponent<exporterControl>();

            // Called when the mod is loaded.
        }
    }
}
