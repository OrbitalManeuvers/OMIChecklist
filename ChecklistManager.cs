using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using UnityEngine;

namespace OMIChecklist
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.EDITOR)]
    class ChecklistManager : ScenarioModule
    {
        private static int categoryIndex;
        private static Checklist checklist;

        public ChecklistManager()
        {
            Instance = this;
            checklist = null;
            categoryIndex = -1;
        }

        public static ChecklistManager Instance { get; private set; }

        public void LoadChecklist(String fileCode)
        {
            checklist = null; // is this a destroy?
            categoryIndex = -1;

            Log.Info($"fileCode={fileCode}");

            // the code just has to be the first 4 characters of the name, with a .checklist ext
            string mask = fileCode + "*.checklist";
            string[] fileList = Directory.GetFiles(ChecklistConfig.CHECKLIST_FOLDER, mask);

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

        public string ChecklistTitle()
        {
            return (checklist != null) ? checklist.Title : "<none>";
        }

        public List<ChecklistItem> CurrentChecklistItems()
        {
            return checklist?.Categories[categoryIndex].Items;
        }

        public void NextCategory()
        {
            if (categoryIndex < checklist?.Categories.Count - 1)
                categoryIndex++;
        }

        public void PrevCategory()
        {
            if (categoryIndex > 0 && checklist != null)
                categoryIndex--;
        }

        public string CurrentCategoryName()
        {
            return (checklist != null) ? checklist.Categories[categoryIndex].Name : "<none>";
        }

        public ChecklistCategory CurrentCategory()
        {
            return checklist?.Categories[categoryIndex];
        }

        public void CheckAll(bool isChecked)
        {
            if (checklist != null)
            {
                foreach (ChecklistItem item in checklist.Categories[categoryIndex].Items)
                {
                    item.Checked = isChecked;
                }
            }
        }

        public void Proceed()
        {
            if (checklist != null)
            {
                for (int i = 0; i < checklist.Categories[categoryIndex].Items.Count; i++)
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
    }
}
