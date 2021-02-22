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
        private const string ModName = "OMIChecklist/";
        private const string Icon_Active = ModName + "Assets/checklist_active";
        private const string Icon_Inactive = ModName + "Assets/checklist_inactive";

        public static readonly String ROOT_PATH = KSPUtil.ApplicationRootPath;
        public static readonly String CONFIG_BASE_FOLDER = ROOT_PATH + "GameData/";
        public static readonly String CHECKLIST_FOLDER = CONFIG_BASE_FOLDER + ModName + "checklists/";

        private ApplicationLauncherButton _ChecklistButton;
        private static readonly Texture2D buttontexture = GameDatabase.Instance.GetTexture(Icon_Inactive, false);

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

        private string display = "0000";
        private int selectionGridInt = -1;
        private string[] selectionStrings = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        private Checklist checklist = null;
        private int categoryIndex = -1;

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
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.fixedWidth = 400f;
            _windowStyle.fixedHeight = 500f;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _toggleStyle = new GUIStyle(HighLogic.Skin.toggle);

            _categoryStyle = new GUIStyle(HighLogic.Skin.label);
            _categoryStyle.alignment = TextAnchor.MiddleCenter;
            _categoryStyle.fontStyle = FontStyle.Bold;

            _displayStyle = new GUIStyle(HighLogic.Skin.label);
            _displayStyle.alignment = TextAnchor.MiddleCenter;
            _displayStyle.fontStyle = FontStyle.Bold;
            _displayStyle.fontSize = 14;

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

            GUILayout.Label(display, _displayStyle, new[] { GUILayout.Width(45), GUILayout.Height(40) });

            GUILayout.BeginVertical();
            if (GUILayout.Button("Load", GUILayout.Width(80)))
            {
                // execute load with contents of display
                LoadChecklist(display);
            }
            if (GUILayout.Button("Clear", GUILayout.Width(80)))
            {
                display = "0000"; // formalize this
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical(); // row 1

            // checklist title
            GUILayout.BeginVertical();
            string checklistTitle = (checklist != null) ? checklist.Title : "<none>";
            GUILayout.Label("Loaded: <b>" + checklistTitle + "</b>", _labelStyle);
            GUILayout.EndVertical();

            // row 2: category 
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(20)))
            {
                // previous category
                if (checklist != null)
                {
                    if (categoryIndex > 0)
                        categoryIndex--;
                }
            }

            string category = (checklist != null) ? checklist.Categories[categoryIndex].Name : "<none>";
            GUILayout.Label(category, _categoryStyle, GUILayout.Width(150));

            if (GUILayout.Button(">", GUILayout.Width(20)))
            {
                // next category
                if (checklist != null)
                {
                    if (categoryIndex < checklist.Categories.Count - 1)
                        categoryIndex++;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical(); // row 2 category

            // row 3: list items and buttons

            GUILayout.BeginHorizontal(); // list on the left, buttons on the right
            GUILayout.BeginVertical(); // scroll list
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, _scrollStyle, GUILayout.Width(300), GUILayout.Height(355));

            GUILayout.BeginVertical(); // the items
            if (checklist != null)
            {
                int i;
                for (i = 0; i < checklist.Categories[categoryIndex].Items.Count; i++)
                {
                    ChecklistItem item = checklist.Categories[categoryIndex].Items[i];
                    item.Checked = GUILayout.Toggle(item.Checked, item.Caption, _toggleStyle);
                }
            }
            GUILayout.EndVertical();  // items

            GUILayout.EndScrollView(); //

            GUILayout.EndVertical(); // scroll list, and left side

            GUILayout.BeginVertical(); // buttons, right side
            if (GUILayout.Button("Check All", GUILayout.Width(80)))
            {
                if (checklist != null)
                {
                    checklist.Categories[categoryIndex].SetAll(true);
                }
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                if (checklist != null)
                {
                    checklist.Categories[categoryIndex].SetAll(false);
                }
            }
            
            // breathe
            GUILayout.Label("", GUILayout.Height(25));

            // proceed
            if (GUILayout.Button("PRO", new[] { GUILayout.Width(80), GUILayout.Height(80) })) // whopper w/cheese
            {
                if (checklist != null)
                {
                    int i;
                    for (i = 0; i < checklist.Categories[categoryIndex].Items.Count; i++)
                    {
                        if (!checklist.Categories[categoryIndex].Items[i].Checked)
                        {
                            // just the first one then bail
                            checklist.Categories[categoryIndex].Items[i].Checked = true;
                            break;
                        }
                    }
                }
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
            display = display.Substring(1, 3) + newDigit;
        }

        // once Load is clicked, the 4-digit code is passed here for loading the physical file if it exists
        void LoadChecklist(String fileCode)
        {
            // move to ResetChecklist() or something
            checklist = null; // is this a destroy? i feel like i'm throwing away memory here
            categoryIndex = -1;
            display = "0000";

            Log.Info($"fileCode={fileCode}");

            // the code just has to be the first 4 characters of the name, with a .checklist ext
            string mask = fileCode + "*.checklist";
            string[] fileList = Directory.GetFiles(CHECKLIST_FOLDER, mask);

            // if there are any there, take the first one. keep your 4-digits unique, folks.
            if (fileList.Length > 0)
            {
                Log.Info($"file is {fileList[0]}");

                // this code came-ish from x-science. i have no idea which file access method is "best" under
                // ksp, and I don't know how to wrangle ksp.io.textreader.createfor<me> stuff with the pathing and whatnot

                using (StreamReader reader = File.OpenText(fileList[0]))
                {
                    List<string> lines = new List<string>();

                    string line = "";
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }

                    checklist = new Checklist(lines); 
                    if (checklist.Categories.Count > 0)
                        categoryIndex = 0;

                    ScreenMessages.PostScreenMessage($"Loaded {checklist.Title}", 5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

    }
}
