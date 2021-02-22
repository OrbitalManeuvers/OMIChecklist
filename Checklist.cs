using System;
using System.Collections.Generic;
using UnityEngine;

namespace OMIChecklist
{
	public class ChecklistItem
	{
		public bool Checked { get; set; }
		public string Caption { get; set; }

		public ChecklistItem(string caption)
		{
			this.Caption = caption;
			this.Checked = false;
		}
	}

	public class ChecklistCategory
	{
		private List<ChecklistItem> _Items;

		public string Name { get; set; }

		public List<ChecklistItem> Items
		{
			get
            {
				if (_Items == null)
				{
					_Items = new List<ChecklistItem>();
				}
				return _Items;
			}
		}

		public ChecklistCategory(string categoryName)
		{
			this.Name = categoryName;
		}

		public void AddItem(string caption)
		{
			Items.Add(new ChecklistItem(caption));
		}

		public void SetAll(bool isChecked)
        {
			foreach (ChecklistItem item in Items)
            {
				item.Checked = isChecked;
            }
        }
	}

	public class Checklist
	{
		private List<ChecklistCategory> _Categories;

		public string Title { get; set; }
		public List<ChecklistCategory> Categories
        {
			get
            {
				if (_Categories == null)
                {
					_Categories = new List<ChecklistCategory>();
                }
				return _Categories;
            }
        }

		public Checklist(List<string> lines)
		{
			Title = "";
			ChecklistCategory lastCategory = null;

			foreach (string line in lines)
			{
				string s = line.Trim();
				if (!String.IsNullOrEmpty(s))
				{
					// start of a category?
					if (s.StartsWith("["))
					{
						lastCategory = new ChecklistCategory(s.TrimStart('[').TrimEnd(']'));
						Categories.Add(lastCategory);
					}
					else
					{
						// if we haven't set the title yet, and we haven't started a category yet, then this is the title
						if ((lastCategory == null) && (this.Title == ""))
                        {
							this.Title = s;
                        }
						else if (lastCategory != null)
                        {
							lastCategory.AddItem(s);
						}
					}
				}
			}
		}
	}
}