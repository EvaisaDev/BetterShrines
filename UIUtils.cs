using Mono.Cecil;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Evaisa.BetterShrines
{
    class UIUtils
    {
        public static GameObject CreateCanvasImage(Vector3 Position, Vector2 Scale, string iconFile)
        {

            HUD[] HudObjects = GameObject.FindObjectsOfType(typeof(HUD)) as HUD[];
            GameObject MainCanvas = HudObjects[0].gameObject;

            GameObject MainHolder = MainCanvas.transform.Find("MainContainer").Find("MainUIArea").gameObject;

            GameObject imageObject = new GameObject("CanvasImage");
            imageObject.AddComponent<Image>();
            var tex = ChanceShrine.ResourcesCached.Load<Texture2D>(iconFile);
          
            imageObject.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            imageObject.GetComponent<RectTransform>().sizeDelta = new Vector2(tex.width, tex.height);
            imageObject.GetComponent<RectTransform>().SetParent(MainCanvas.transform);
            imageObject.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            imageObject.SetActive(true);
            imageObject.GetComponent<RectTransform>().ForceUpdateRectTransforms();


            var distance = (Camera.main.transform.position - Position).magnitude;
            var size = Scale * 100 / (100 + distance);

            //   imageObject.transform.SetParent(MainHolder.transform);
            /*
            var imageObject = new GameObject("CanvasImage");

            var rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.SetParent(MainCanvas.GetComponent<RectTransform>(), false);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = new Vector2(0f, 0f);
            rectTransform.sizeDelta = size;

            Image image = imageObject.AddComponent<Image>();

            
            image.sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
            imageObject.transform.SetParent(MainHolder.transform);*/

            //return imageObject;

            return null;
        }
    }
}
