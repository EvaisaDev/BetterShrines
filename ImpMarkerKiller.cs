using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace Evaisa.BetterShrines
{
    public class ImpMarkerKiller : MonoBehaviour
    {
        public void Update()
        {
            var markerComponent = GetComponent<PositionIndicator>();
            if(!markerComponent.targetTransform)
            {
                Evaisa.BetterShrines.BetterShrines.Print("Destroyed indicator!");
                DestroyImmediate(this.gameObject);
            }
        }
    }
}
