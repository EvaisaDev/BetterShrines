using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using R2API;
using R2API.AssetPlus;
using Object = UnityEngine.Object;

namespace Evaisa.BetterShrines
{
    public static class EvaResources
    {
        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = execAssembly.GetManifestResourceStream("Evaisa.BetterShrines.shrinebundle"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);

                FreezeIcon = bundle.LoadAsset<Object>("Assets/FreezeIcon.png");
                SmiteIcon = bundle.LoadAsset<Object>("Assets/SmiteIcon.png");
                IconPrefab = bundle.LoadAsset<Object>("Assets/IconDisplay.prefab");
                LightningAreaPrefab = bundle.LoadAsset<Object>("Assets/lightningarea.prefab");
                ShrineImpPrefab = bundle.LoadAsset<Object>("Assets/ImpShrine/ShrineImp.prefab");
                ShrineFallenPrefab = bundle.LoadAsset<Object>("Assets/ShrineOfTheFallen/ShrineFallen.prefab");
                BetterShrines.Print(ShrineImpPrefab.name + " was loaded!");
                BetterShrines.Print(ShrineFallenPrefab.name + " was loaded!");
            }
        }

        public static bool Loaded { get; private set; }

        public static Object SmiteIcon { get; private set; }

        public static Object FreezeIcon { get; private set; }

        public static Object IconPrefab { get; private set; }

        public static Object ShrineImpPrefab { get; private set; }

        public static Object ShrineFallenPrefab { get; private set; }

        public static Object LightningAreaPrefab { get; private set; }
    }
}
