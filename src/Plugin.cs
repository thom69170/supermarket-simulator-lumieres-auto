using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AutoLights
{
    public enum LightMode { Off, WhenStoreOpens, AtHour }

    [BepInPlugin("com.thomas.autolights", "Auto Lights", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static ManualLogSource L;
        internal static ConfigEntry<LightMode> Mode;
        internal static ConfigEntry<int> Hour;

        public override void Load()
        {
            L = Log;
            Mode = Config.Bind("General", "Mode", LightMode.WhenStoreOpens, "Off, WhenStoreOpens or AtHour");
            Hour = Config.Bind("General", "Hour", 8, new ConfigDescription("Game hour (0-23) used by AtHour mode", new AcceptableValueRange<int>(0, 23)));

            ClassInjector.RegisterTypeInIl2Cpp<AutoLightsBehaviour>();
            var go = new GameObject("AutoLights");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<AutoLightsBehaviour>();

            new Harmony("com.thomas.autolights").PatchAll(typeof(Patches));
            Log.LogInfo("Auto Lights loaded");
        }
    }

    internal static class Patches
    {
        [HarmonyPatch(typeof(EscapeMenuManager), "OnEnable")]
        [HarmonyPostfix]
        static void EscapeOnEnable(EscapeMenuManager __instance)
        {
            try { AutoLightsBehaviour.InjectButton(__instance); }
            catch (Exception e) { Plugin.L.LogError(e.ToString()); }
        }
    }

    public class AutoLightsBehaviour : MonoBehaviour
    {
        public AutoLightsBehaviour(IntPtr ptr) : base(ptr) { }

        const string ButtonName = "AutoLightsButton";
        const string Label = "Lumieres auto";
        static TMPro.TMP_Text labelTmp;
        static Text labelTxt;
        static bool panelOpen;
        int lastDay = -1;
        bool doneToday;
        Rect window = new Rect(Screen.width / 2f - 160, Screen.height / 2f - 130, 320, 260);

        internal static void InjectButton(EscapeMenuManager esc)
        {
            var menu = esc.m_Menu;
            var template = esc.m_SaveButton;
            if (menu == null || template == null) return;
            var parent = template.transform.parent;
            if (parent.Find(ButtonName) != null) return;

            var clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
            clone.name = ButtonName;
            clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            clone.SetActive(true);

            var tmp = clone.GetComponentInChildren<TMPro.TMP_Text>(true);
            labelTmp = tmp; labelTxt = null;
            if (tmp != null) tmp.text = Label;
            else
            {
                var txt = clone.GetComponentInChildren<Text>(true);
                labelTxt = txt; if (txt != null) txt.text = Label;
            }

            var btn = clone.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener((UnityAction)(() => panelOpen = true));
        }

        static bool blocked;
        static void SetBlock(bool b)
        {
            if (b == blocked) return;
            blocked = b;
            try
            {
                var esc = EscapeMenuManager.Instance;
                var g = esc != null ? esc.m_Menu : null;
                if (g == null) return;
                var cg = g.GetComponent<CanvasGroup>();
                if (cg == null && b) cg = g.AddComponent<CanvasGroup>();
                if (cg != null) { cg.blocksRaycasts = !b; cg.interactable = !b; }
            }
            catch (Exception e) { Plugin.L.LogError("SetBlock: " + e.Message); }
        }

        void OnGUI()
        {
            if (!panelOpen) return;
            var esc = EscapeMenuManager.Instance;
            if (esc == null || !esc.IsPauseMenuOpened) { panelOpen = false; return; }
            window = GUI.Window(48151, window, (GUI.WindowFunction)DrawWindow, "Lumieres automatiques");
        }

        static readonly System.Collections.Generic.HashSet<string> failed = new System.Collections.Generic.HashSet<string>();
        static T Safe<T>(string name, Func<T> f, T fallback)
        {
            try { return f(); }
            catch (Exception e)
            {
                if (failed.Add(name)) Plugin.L.LogError("GUI call failed: " + name + " -> " + e.Message);
                return fallback;
            }
        }

        void DrawWindow(int id)
        {
            Safe("Bg", () => { var old = GUI.color; GUI.color = new Color(0.08f, 0.12f, 0.16f, 1f); GUI.DrawTexture(new Rect(0, 0, 320, 260), Texture2D.whiteTexture); GUI.color = old; return 0; }, 0);
            float x = 16, w = 288, y = 34;
            Safe("Label1", () => { GUI.Label(new Rect(x, y, w, 22), "Allumer les lumieres :"); return 0; }, 0);
            y += 26;
            string[] names = { "Jamais", "A l'ouverture du magasin", "A une heure precise" };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                bool on = (int)Plugin.Mode.Value == idx;
                string text = (on ? "[x] " : "[  ] ") + names[idx];
                if (Safe("Button" + idx, () => GUI.Button(new Rect(x, y, w, 26), text), false)) Plugin.Mode.Value = (LightMode)idx;
                y += 30;
            }
            if (Plugin.Mode.Value == LightMode.AtHour)
            {
                Safe("Label2", () => { GUI.Label(new Rect(x, y, w, 22), "Heure : " + Plugin.Hour.Value + "h"); return 0; }, 0);
                y += 24;
                if (Safe("Minus", () => GUI.Button(new Rect(x, y, 60, 26), "-"), false)) Plugin.Hour.Value = Mathf.Max(0, Plugin.Hour.Value - 1);
                if (Safe("Plus", () => GUI.Button(new Rect(x + 70, y, 60, 26), "+"), false)) Plugin.Hour.Value = Mathf.Min(23, Plugin.Hour.Value + 1);
                y += 34;
            }
            if (Safe("Close", () => GUI.Button(new Rect(x, 222, w, 28), "Fermer"), false)) panelOpen = false;
        }

        void Update()
        {
            SetBlock(panelOpen);
            try
            {
                if (labelTmp != null && labelTmp.text != Label) labelTmp.text = Label;
                if (labelTxt != null && labelTxt.text != Label) labelTxt.text = Label;
            }
            catch { labelTmp = null; labelTxt = null; }
            try
            {
                var day = DayCycleManager.Instance;
                var lights = StoreLightManager.Instance;
                var status = StoreStatus.Instance;
                if (day == null || lights == null || status == null) return;

                if (day.CurrentDay != lastDay) { lastDay = day.CurrentDay; doneToday = false; }
                if (doneToday || Plugin.Mode.Value == LightMode.Off) return;

                bool trigger = Plugin.Mode.Value == LightMode.WhenStoreOpens
                    ? status.IsOpen
                    : day.CurrentHour >= Plugin.Hour.Value;
                if (!trigger) return;

                doneToday = true;
                if (!lights.TurnOn) lights.StoreLightSwitchTurnOn_RPC(true);
            }
            catch (Exception e) { Plugin.L.LogError(e.ToString()); enabled = false; }
        }
    }
}
