using KSP.UI.Screens;

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace OMIChecklist
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ChecklistDisplay_Flight: ChecklistDisplay
    {
        protected override bool IsActive()
        {
            return HighLogic.LoadedSceneIsFlight;
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class ChecklistDisplay_Editors : ChecklistDisplay
    {
        protected override bool IsActive()
        {
            return HighLogic.LoadedSceneIsEditor;
        }
    }

    //
    public class ChecklistDisplay : MonoBehaviour
    {
        private ApplicationLauncherButton _ChecklistButton;
        private static readonly Texture2D buttontexture = GameDatabase.Instance.GetTexture(ChecklistConfig.Icon_Active, false);

        private Rect _windowPosition = new Rect(300, 60, 400, 650);

        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _scrollStyle;
        private GUIStyle _toggleStyle;
        private GUIStyle _categoryStyle;
        private GUIStyle _displayStyle;
        private Vector2 _scrollPos = Vector2.zero;

        public static bool _windowVisible = false;
        private bool _initComplete = false;

        private string digits = "0000";
        private int selectionGridInt = -1;
        private string[] selectionStrings = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        void Awake()
        {
            _ChecklistButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS, buttontexture);
        }

        private void GuiOn()
        {
            _windowVisible = true;
        }

        private void GuiOff()
        {
            _windowVisible = false;
        }

        protected virtual bool IsActive()
        {
            return false;
        }

        public void Start()
        {
            if (!_initComplete)
            {
                InitStyles();
            }
        }

        private void InitStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window)
            {
                fixedWidth = 400f,
                fixedHeight = 500f
            };
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _toggleStyle = new GUIStyle(HighLogic.Skin.toggle);

            _categoryStyle = new GUIStyle(HighLogic.Skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            _displayStyle = new GUIStyle(HighLogic.Skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };

            _initComplete = true;
        }

        private void OnGUI()
        {
            if (!_windowVisible)
                return;
            if (!IsActive())
                return;
            OnDrawWindow();
        }

        private void OnDrawWindow()
        {
            _windowPosition = GUILayout.Window(1, _windowPosition, RenderWindow, "OMI Checklists", _windowStyle);
        }

        // ignoring this int because "i'll never have another window" and 640k is enough
        private void RenderWindow(int windowId)
        {
            // row 1: keypad, display, load/clear buttons
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            // yeah, what follows is pretty horrid. I mean, I did it as cleanly as I know how, but this mixing the
            // ui definition with the event handler code is a nightmare.
            
            // This mod needs to be converted to use a prefab, badly.

            selectionGridInt = GUILayout.SelectionGrid(selectionGridInt, selectionStrings, 5, GUILayout.Width(28 * 5));
            if (selectionGridInt != -1)
            {
                KeypadPressed();
            }

            GUILayout.Label(digits, _displayStyle, new[] { GUILayout.Width(45), GUILayout.Height(40) });

            GUILayout.BeginVertical();
            if (GUILayout.Button("Load", GUILayout.Width(80)))
            {
                // execute load with contents of display
                LoadChecklist(digits);
            }
            if (GUILayout.Button("Clear", GUILayout.Width(80)))
            {
                digits = "0000"; // formalize this
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical(); // row 1

            // checklist title
            GUILayout.BeginVertical();

            // this syntax is neat
            //string checklistTitle = ChecklistManager.Instance.CurrentCategoryName()
            string checklistTitle = ChecklistManager.Instance.ChecklistTitle();
            GUILayout.Label("Loaded: <b>" + checklistTitle + "</b>", _labelStyle);
            GUILayout.EndVertical();

            // row 2: category 
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(20)))
            {
                ChecklistManager.Instance.PrevCategory();
            }

            string categoryName = ChecklistManager.Instance.CurrentCategoryName();
            GUILayout.Label(categoryName, _categoryStyle, GUILayout.Width(150));

            if (GUILayout.Button(">", GUILayout.Width(20)))
            {
                ChecklistManager.Instance.NextCategory();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical(); // row 2 category

            // row 3: list items and buttons

            GUILayout.BeginHorizontal(); // list on the left, buttons on the right
            GUILayout.BeginVertical(); // scroll list
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, _scrollStyle, GUILayout.Width(300), GUILayout.Height(355));

            GUILayout.BeginVertical(); // the items
            List<ChecklistItem> items = ChecklistManager.Instance.CurrentChecklistItems();
            if (items != null)
            {
                foreach (ChecklistItem item in items)
                {
                    item.Checked = GUILayout.Toggle(item.Checked, item.Caption, _toggleStyle);
                }
            }
            GUILayout.EndVertical();  // items

            GUILayout.EndScrollView(); //

            GUILayout.EndVertical(); // scroll list, and left side

            GUILayout.BeginVertical(); // buttons, right side
            if (GUILayout.Button("Check All", GUILayout.Width(80)))
            {
                ChecklistManager.Instance.CheckAll(true);
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                ChecklistManager.Instance.CheckAll(false);
            }

            // breathe
            GUILayout.Label("", GUILayout.Height(25));

            // proceed
            if (GUILayout.Button("PRO", new[] { GUILayout.Width(80), GUILayout.Height(80) })) // whopper w/cheese
            {
                ChecklistManager.Instance.Proceed();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal(); // list and buttons

            GUI.DragWindow();
        }

        internal void OnDestroy()
        {
            if (_ChecklistButton == null)
                return;
            ApplicationLauncher.Instance.RemoveModApplication(_ChecklistButton);
            _ChecklistButton = null;
        }

        // this implements a sort of round-robin input method a little like the Gemini computer?
        void KeypadPressed()
        {
            selectionGridInt++; // 0-9 -> 1-10
            string newDigit = (selectionGridInt == 10) ? "0" : selectionGridInt.ToString();

            // reset so nothing is selected (docs are silent about -1 but it works as expected)
            selectionGridInt = -1;

            // shift left and add new digit on the end
            digits = digits.Substring(1, 3) + newDigit;
        }

        // once Load is clicked, the 4-digit code is passed here for loading the physical file if it exists
        void LoadChecklist(String fileCode)
        {
            digits = "0000";
            ChecklistManager.Instance.LoadChecklist(fileCode);
        }

    }
}
