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

        // keep track of the loaded list
        private static string lastLoadedList = "";

        public ChecklistManager()
        {
            Instance = this;
            checklist = null;
            categoryIndex = -1;
            Log.Active = true;
        }

        public static ChecklistManager Instance { get; private set; }

        public override void OnLoad(ConfigNode node)
        {
            lastLoadedList = "";
            
            ConfigNode state_node = node.GetNode(ChecklistConfig.STATE_NODE_NAME);
            if (state_node != null)
            {
                state_node.TryGetValue("list_name", ref lastLoadedList);
                Log.Write($"list_name={lastLoadedList}");
                state_node.TryGetValue("category_index", ref categoryIndex);
                if (!String.IsNullOrEmpty(lastLoadedList) && (lastLoadedList.Length == 4))
                {
                    LoadChecklist(lastLoadedList);

                    // make sure categoryIndex is still valid
                    if (categoryIndex < 0 || categoryIndex > checklist.Categories.Count - 1)
                        categoryIndex = 0;

                    // see if we have checked state
                    var category_nodes = node.GetNodes(ChecklistConfig.CATEGORY_NODE_NAME);
                    Log.Write($"Loaded {category_nodes.Length} category nodes");

                    if (category_nodes.Length > 0)
                    {
                        var checked_nodes = node.GetNodes(ChecklistConfig.CHECKED_NODE_NAME);
                        Log.Write($"Loaded {checked_nodes.Length} checked nodes");

                        foreach (ConfigNode item_node in checked_nodes)
                        {
                            int cat_id = -1;
                            item_node.TryGetValue("cat_id", ref cat_id);

                            if (cat_id >= 0)
                            {
                                // see if we have this in the category dictionary
                                var category_node = node.GetNode(ChecklistConfig.CATEGORY_NODE_NAME, "id", cat_id.ToString());
                                if (category_node != null)
                                {
                                    string cat_name = "";
                                    category_node.TryGetValue("name", ref cat_name);

                                    // see if the checklist still has this category
                                    ChecklistCategory category = FindCategory(cat_name);
                                    if (category != null)
                                    {
                                        // try to find the item in the category
                                        string item_caption = "";
                                        item_node.TryGetValue("caption", ref item_caption);
                                        if (!String.IsNullOrEmpty(item_caption))
                                        {
                                            ChecklistItem item = FindItem(category, item_caption);
                                            if (item != null)
                                                item.Checked = true; // lol ... all that for just this. *sigh*
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            if (!String.IsNullOrEmpty(lastLoadedList) && checklist != null)
            {
                // save the state node
                ConfigNode state_node = new ConfigNode(ChecklistConfig.STATE_NODE_NAME);
                state_node.AddValue("list_name", lastLoadedList);
                state_node.AddValue("category_index", categoryIndex);
                node.AddNode(state_node);

                int categoryID = 0;

                // list of categories that have checked nodes
                List<ChecklistCategory> checkedCategories = new List<ChecklistCategory>();
                for (int i = 0; i < checklist.Categories.Count; i++)
                {
                    ChecklistCategory category = checklist.Categories[i];

                    // create a list of all the checked items
                    List<ChecklistItem> checkedItems = new List<ChecklistItem>();
                    foreach (ChecklistItem item in category.Items)
                    {
                        if (item.Checked)
                            checkedItems.Add(item);
                    }

                    // if this list isn't empty then create a config node for the category
                    if (checkedItems.Count > 0)
                    {
                        var category_node = new ConfigNode(ChecklistConfig.CATEGORY_NODE_NAME);
                        category_node.AddValue("name", category.Name);
                        category_node.AddValue("id", categoryID);
                        node.AddNode(category_node);

                        // and then nodes for each of the checked items
                        foreach (ChecklistItem item in checkedItems)
                        {
                            var checked_node = new ConfigNode(ChecklistConfig.CHECKED_NODE_NAME);
                            checked_node.AddValue("cat_id", categoryID);
                            checked_node.AddValue("caption", item.Caption);
                            node.AddNode(checked_node);
                        }

                        categoryID++;
                    }
                }

            }
        }

        private ChecklistCategory FindCategory(string categoryName)
        {
            foreach (ChecklistCategory c in checklist.Categories)
            {
                if (c.Name.Equals(categoryName))
                    return c;
            }
            return null;
        }

        private ChecklistItem FindItem(ChecklistCategory category, string item_name)
        {
            foreach (ChecklistItem item in category.Items)
            {
                if (item.Caption == item_name)
                    return item;
            }
            return null;
        }

        public void LoadChecklist(String fileCode)
        {
            Log.Write($"Attempting to loading {fileCode}");

            checklist = null; // is this a destroy?
            categoryIndex = -1;
            lastLoadedList = "";

            // the code just has to be the first 4 characters of the name, with a .checklist ext
            string mask = fileCode + "*.checklist";
            string[] fileList = Directory.GetFiles(ChecklistConfig.CHECKLIST_FOLDER, mask);

            // if there are any there, take the first one. keep your 4-digits unique, folks.
            if (fileList.Length > 0)
            {
                Log.Write($"Loading {fileList[0]}");
                lastLoadedList = fileCode; // save this for persistence

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
