using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace PartSearch
{
    using FilterExtensions;
    using FilterExtensions.ConfigNodes;
    using FilterExtensions.Utility;

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class PartSearch : MonoBehaviour
    {
        customSubCategory customTestCategory;
        PartCategorizer.Category testCategory;
        static ApplicationLauncherButton btn;

        List<string> PartCategories = new List<string>() { "None", "Pods", "Engines", "Fuel Tanks", "Command and Control", "Structural", "Aerodynamics", "Utility", "Science" };
        List<string> customChecks = new List<string>() { "adapter", "multicoupler" };
        
        List<string> folders = new List<string>();
        List<string> manufacturers = new List<string>();
        List<string> moduleNames = new List<string>();
        List<string> moduleTitles = new List<string>();
        List<string> profiles = new List<string>();
        List<string> techs = new List<string>();

        public void Start()
        {
            customTestCategory = new customSubCategory("testCategory", "");
            customTestCategory.filters.Add(new Filter(false));
            customTestCategory.filters[0].checks.Add(defaultCheck());
            if (btn == null)
                btn = ApplicationLauncher.Instance.AddModApplication(Toggle, Toggle, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, (Texture2D)GameDatabase.Instance.GetTexture("000_FilterExtensions/Icons/FilterCreator", false));
            StartCoroutine(initialise());
            populateLists();
        }

        void populateLists()
        {
            foreach (AvailablePart ap in PartLoader.Instance.parts)
            {
                if (Core.Instance.partPathDict.ContainsKey(ap.name))
                    folders.AddUnique(Core.Instance.partPathDict[ap.name].Split(new char[] { '/', '\\' })[0]);
                if (!string.IsNullOrEmpty(ap.manufacturer))
                    manufacturers.AddUnique(ap.manufacturer);
                if (ap.moduleInfos != null)
                {
                    foreach (AvailablePart.ModuleInfo m in ap.moduleInfos)
                        moduleTitles.AddUnique(m.moduleName);
                }
                if (ap.partPrefab != null && ap.partPrefab.Modules != null)
                {
                    foreach (PartModule m in ap.partPrefab.Modules)
                        moduleNames.AddUnique(m.ClassName);
                }
                if (!string.IsNullOrEmpty(ap.bulkheadProfiles))
                    profiles.AddUniqueRange(ap.bulkheadProfiles.Split(new string[2] { ",", " " }, StringSplitOptions.RemoveEmptyEntries));
                if (!string.IsNullOrEmpty(ap.TechRequired))
                    techs.AddUnique(ap.TechRequired);
            }
        }

        void Toggle()
        {
            showWindow = !showWindow;
        }

        IEnumerator initialise()
        {
            for (int i = 0; i < 14; i++) // FE will do a refresh after a count of 16. Deletion is done after a count of 12.
                yield return null;
            customTestCategory.initialise(PartCategorizer.Instance.filters[0]);
            testCategory = PartCategorizer.Instance.filters[0].subcategories.First(c => c.button.categoryName == "testCategory");
        }

        void refresh()
        {
            EditorPartList.Instance.Refresh();
        }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0) && showEditWindow && !windowsContainMouse())
            {
                showEditWindow = false;
            }
        }

        bool windowsContainMouse()
        {
            Vector2 guiMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (editWindowRect.Contains(guiMousePos) || windowRect.Contains(guiMousePos))
                return true;
            return false;
        }

        Rect windowRect = new Rect(300, 200, 300, 0);
        Rect editWindowRect = new Rect(0, 0, 450, 0);
        public void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (customTestCategory == null)
                return;
            if (showWindow)
                windowRect = GUILayout.Window(65874985, windowRect, drawWindow, "");
            if (showEditWindow && checkToEdit != null)
            {
                editWindowRect.x = windowRect.x + windowRect.width;
                editWindowRect.y = windowRect.y;
                editWindowRect = GUILayout.Window(45684123, editWindowRect, checkEditWindow, "");
            }
            if (tooltip != "" && showTooltip && showEditWindow)
                valueRect = GUILayout.Window(48510054, valueRect, tooltipWindow, "", GUI.skin.label);
        }

        bool showWindow = false;
        int selectedFilter = 0;
        /// <summary>
        /// Draws the window detailing an overview of the subcategory.
        /// </summary>
        void drawWindow(int id)
        {
            GUILayout.Label("Select filter to view/edit");
            if (customTestCategory.filters.Count == 0)
            {
                customTestCategory.filters.Add(new Filter(false));
                customTestCategory.filters[0].checks.Add(defaultCheck());
            }
            GUIContent[] filters = new GUIContent[customTestCategory.filters.Count];
            for (int i = 0; i < customTestCategory.filters.Count; i++)
                filters[i] = new GUIContent(i.ToString());
            GUILayout.BeginHorizontal();
            selectedFilter = GUILayout.SelectionGrid(selectedFilter, filters, filters.Length, GUILayout.Width(filters.Length * 40));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(40)))
            {
                customTestCategory.filters.Add(new Filter(false));
                customTestCategory.filters[customTestCategory.filters.Count - 1].checks.Add(defaultCheck());
                selectedFilter = customTestCategory.filters.Count - 1;
                refresh();
            }
            GUILayout.EndHorizontal();
            drawFilter(customTestCategory.filters[selectedFilter]);

            GUILayout.Space(20);
            if (GUILayout.Button("Log subcategory node"))
                Debug.Log(customTestCategory.toConfigNode());

            GUI.DragWindow();
        }

        /// <summary>
        /// Draws all the checks associated with the given Filter
        /// </summary>
        void drawFilter(Filter filter)
        {
            bool tmpBool = GUILayout.Toggle(filter.invert, "Invert filter result", GUI.skin.button);
            if (filter.invert != tmpBool)
            {
                filter.invert = tmpBool;
                refresh();
            }
            GUILayout.Space(20);
            GUILayout.Label("Checks");
            Check toremove = null;
            foreach (Check c in filter.checks)
            {
                if (drawCheck(c))
                {
                    toremove = c;
                    refresh();
                }
            }
            if (toremove != null)
            {
                filter.checks.Remove(toremove);
                refresh();
            }
            if (GUILayout.Button("Add Check"))
            {
                filter.checks.Add(defaultCheck());
                refresh();
            }
        }

        /// <summary>
        /// draws a toggle with the check details listed and a delete button
        /// </summary>
        /// <returns>true if the Check should be removed from the Filter</returns>
        bool drawCheck(Check check)
        {
            bool ret = false;
            GUILayout.BeginHorizontal();
            string s = "Type: " + check.type;
            if (check.type != CheckType.check)
                s += "\r\nValue: " + check.value;
            if (check.invert)
                s += "\r\nInvert: " + check.invert.ToString();
            if (check.checkUsesContains() && check.contains != true)
                s += "\r\nContains: " + check.contains.ToString();
            if (check.checkUsesEquality() && check.equality != Check.Equality.Equals)
                s += "\r\nEquality: " + check.equality.ToString();
            bool tmpbool = GUILayout.Toggle(check == checkToEdit && showEditWindow, s, GUI.skin.button);
            if (tmpbool != (check == checkToEdit && showEditWindow))
            {
                checkToEdit = check;
                showEditWindow = true;
                typeEdit = false;
                valueEdit = false;
            }
            if (GUILayout.Button("X", GUILayout.Height(60), GUILayout.Width(40)))
                ret = true;
            GUILayout.EndHorizontal();
            return ret;
        }

        bool showEditWindow = false;
        bool typeEdit = false;
        Check checkToEdit = null;
        /// <summary>
        /// The window for editing a selected Check. Type, value, invert, contains, and equality.
        /// </summary>
        void checkEditWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Check Type: ");
            typeEdit = GUILayout.Toggle(typeEdit, Check.getTypeString(checkToEdit.type), GUI.skin.button);
            GUILayout.EndHorizontal();
            if (typeEdit)
            {
                valueEdit = false;
                GUIContent[] content = new GUIContent[Enum.GetValues(typeof(CheckType)).Length];
                for (int i = 0; i < Enum.GetValues(typeof(CheckType)).Length; i++)
                    content[i] = new GUIContent(((CheckType)i).ToString());
                CheckType tmpType = (CheckType)GUILayout.SelectionGrid((int)checkToEdit.type, content, 4);
                if (tmpType != checkToEdit.type)
                {
                    checkToEdit.type = tmpType;
                    checkToEdit.value = SearchUtils.defaultValueForType(checkToEdit);
                    refresh();
                }
                GUILayout.Space(20);
            }
            if (checkToEdit.type != CheckType.check) // check type will need lots of special case handling...
            {
                valueDisplayEdit(checkToEdit);
                bool tmpbool = GUILayout.Toggle(checkToEdit.invert, "Invert Check", GUI.skin.button);
                if (checkToEdit.invert != tmpbool)
                {
                    checkToEdit.invert = tmpbool;
                    refresh();
                }

                if (checkToEdit.checkUsesContains())
                {
                    tmpbool = GUILayout.Toggle(checkToEdit.contains, "Contains", GUI.skin.button);
                    if (checkToEdit.contains != tmpbool)
                    {
                        checkToEdit.contains = tmpbool;
                        refresh();
                    }
                }
                if (checkToEdit.checkUsesEquality())
                {
                    GUIContent[] equalityContent = new GUIContent[3] { new GUIContent("="), new GUIContent("<"), new GUIContent(">") };
                    Check.Equality tmpEq = (Check.Equality)GUILayout.SelectionGrid((int)checkToEdit.equality, equalityContent, 3);
                    if (tmpEq != checkToEdit.equality)
                    {
                        checkToEdit.equality = tmpEq;
                        refresh();
                    }
                }
            }
        }

        bool valueEdit = false;
        /// <summary>
        /// drawing the value line according to check type
        /// </summary>
        void valueDisplayEdit(Check check)
        {
            string tmpstr = check.value;
            int tmpSel;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Value: ");
            switch (check.type)
            {
                // list valid resources to select from.
                case CheckType.propellant:
                case CheckType.resource:
                    
                    valueEdit = GUILayout.Toggle(valueEdit, check.value, GUI.skin.button);
                    GUILayout.EndHorizontal();
                    if (valueEdit)
                    {
                        if (typeEdit)
                            typeEdit = false;
                        tmpSel = GUILayout.SelectionGrid(-1, SearchUtils.buildContentFromList(check, Core.Instance.resources), 4);
                        if (tmpSel >= 0)
                            addRemoveItem(ref tmpstr, Core.Instance.resources[tmpSel]);
                    }
                    break;
                // list valid categories to select from
                case CheckType.category:
                    valueEdit = GUILayout.Toggle(valueEdit, check.value, GUI.skin.button);
                    GUILayout.EndHorizontal();
                    if (valueEdit)
                    {
                        if (typeEdit)
                            typeEdit = false;
                        tmpSel = GUILayout.SelectionGrid((int)PartCategories.IndexOf(check.value), SearchUtils.buildContentFromList(check, PartCategories), 4);
                        if (tmpSel >= 0)
                            addRemoveItem(ref tmpstr, PartCategories[tmpSel]);
                    }
                    break;
                default:
                    GUI.SetNextControlName("valueEntry");
                    tmpstr = GUILayout.TextField(check.value);
                    showTooltip = (GUI.GetNameOfFocusedControl() == "valueEntry");
                    valueRect = GUILayoutUtility.GetLastRect();
                    valueRect.x = editWindowRect.x + editWindowRect.width;
                    valueRect.y += editWindowRect.y - 3;
                    valueRect.width = 150;
                    GUILayout.EndHorizontal();
                    string ttStr = tmpstr.Split(',').Last().Trim();
                    string tmpttStr = populateTooltip(check, ttStr);
                    if (ttStr != tmpttStr && !tmpstr.Contains(tmpttStr))
                    {
                        if (tmpstr.Contains(','))
                            tmpstr = tmpstr.Substring(0, tmpstr.LastIndexOf(',') + 1) + tmpttStr;
                        else
                            tmpstr = tmpttStr;
                    }
                    break;
            }
            if (check.value != tmpstr && SearchUtils.parseValueForType(check, tmpstr))
            {
                check.value = tmpstr;
                if (tmpstr.Last() != '.') // enable floats to be written out without any other special tricks by the user
                    refresh();
            }
        }

        string populateTooltip(Check check, string match)
        {
            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.keyDown)
                return "";

            tooltip = "";
            int hintCount = 10;
            int count = 0;
            List<string> matches = new List<string>();
            switch (check.type)
            {
                case CheckType.folder: // tooltip shows all root folders that could be a match
                    foreach (string s in folders)
                    {
                        if (s.Contains(match))
                        {
                            tooltip += s + "\r\n";
                            matches.Add(s);
                            count++;
                        }
                        if (Event.current.keyCode == KeyCode.Tab && tooltip != "")
                            return tooltip.TrimEnd(new char[] { '\n', '\r' });
                        if (count >= hintCount)
                            break;
                    }
                    break;
                case CheckType.path: // tooltip shows all folders from the current directory that could match
                    foreach (KeyValuePair<string, string> kvp in Core.Instance.partPathDict)
                    {
                        int splitIndex = stringLastIndexOfOrDefault(match, "/");
                        if (kvp.Value.ToLower().StartsWith(match.ToLower()))
                        {
                            string toAdd = kvp.Value.Substring(splitIndex == 0 ? splitIndex : splitIndex + 1);
                            if (toAdd.Contains('/'))
                                toAdd = toAdd.Substring(0, toAdd.IndexOf('/') + 1);
                            if (!matches.Contains(toAdd))
                            {
                                tooltip += toAdd + "\r\n";
                                matches.Add(toAdd);
                                count++;
                            }
                        }
                        if (Event.current.keyCode == KeyCode.Tab && Event.current.type == EventType.keyDown && tooltip != "")
                            return match.Substring(0, splitIndex == 0 ? splitIndex : splitIndex + 1) + tooltip.TrimEnd(new char[] { '\n', '\r' });
                        if (count >= hintCount)
                            break;
                    }
                    break;
                case CheckType.manufacturer:
                    foreach (string s in manufacturers)
                    {
                        if (s.Contains(match))
                        {
                            tooltip += s + "\r\n";
                            matches.Add(s);
                            count++;
                        }
                        if (Event.current.keyCode == KeyCode.Tab && tooltip != "")
                            return tooltip.TrimEnd(new char[] { '\n', '\r' });
                        if (count >= hintCount)
                            break;
                    }
                    break;
                case CheckType.moduleName:
                    foreach (string s in moduleNames)
                    {
                        if (s.Contains(match))
                        {
                            tooltip += s + "\r\n";
                            matches.Add(s);
                            count++;
                        }
                        if (Event.current.keyCode == KeyCode.Tab && tooltip != "")
                            return tooltip.TrimEnd(new char[] { '\n', '\r' });
                        if (count >= hintCount)
                            break;
                    }
                    break;
                case CheckType.moduleTitle:
                    foreach (string s in moduleTitles)
                    {
                        if (s.Contains(match))
                        {
                            tooltip += s + "\r\n";
                            matches.Add(s);
                            count++;
                        }
                        if (Event.current.keyCode == KeyCode.Tab && tooltip != "")
                            return tooltip.TrimEnd(new char[] { '\n', '\r' });
                        if (count >= hintCount)
                            break;
                    }
                    break;
                case CheckType.partName:
                    foreach (AvailablePart ap in PartLoader.Instance.parts)
                    {
                        if (ap.name.Contains(match))
                        {
                            tooltip += ap.name + "\r\n";
                            matches.Add(ap.name);
                            count++;
                        }
                        if (Event.current.keyCode == KeyCode.Tab && tooltip != "")
                            return tooltip.TrimEnd(new char[] { '\n', '\r' });
                        if (count >= hintCount)
                            break;
                    }
                    break;
                case CheckType.partTitle:
                    foreach (AvailablePart ap in PartLoader.Instance.parts)
                    {
                        if (ap.title.Contains(match))
                        {
                            tooltip += ap.title + "\r\n";
                            matches.Add(ap.title);
                            count++;
                        }
                        if (Event.current.keyCode == KeyCode.Tab && tooltip != "")
                            return tooltip.TrimEnd(new char[] { '\n', '\r' });
                        if (count >= hintCount)
                            break;
                    }
                    break;
                case CheckType.profile:
                    foreach (string s in profiles)
                    {
                        if (s.Contains(match))
                        {
                            tooltip += s + "\r\n";
                            matches.Add(s);
                            count++;
                        }
                        if (Event.current.keyCode == KeyCode.Tab && tooltip != "")
                            return tooltip.TrimEnd(new char[] { '\n', '\r' });
                        if (count >= hintCount)
                            break;
                    }
                    break;
                case CheckType.tech:
                    foreach (string s in techs)
                    {
                        if (s.Contains(match))
                        {
                            tooltip += s + "\r\n";
                            matches.Add(s);
                            count++;
                        }
                        if (Event.current.keyCode == KeyCode.Tab && tooltip != "")
                            return tooltip.TrimEnd(new char[] { '\n', '\r' });
                        if (count >= hintCount)
                            break;
                    }
                    break;
                default:
                    tooltip = "";
                    break;
            }
            tooltip = tooltip.TrimEnd(new char[] { '\n', '\r' });
            return match;
        }

        int stringLastIndexOfOrDefault(string toSearch, string toFind)
        {
            if (toSearch.Contains(toFind))
                return toSearch.LastIndexOf(toFind);
            return 0;
        }

        /// <summary>
        /// add or remove a string item to/from a CSV list of string items
        /// </summary>
        /// <param name="stringToEdit">the current CSV list of string items</param>
        /// <param name="itemToChange">the string value to add/remove</param>
        void addRemoveItem(ref string stringToEdit, string itemToChange)
        {
            List<string> values = stringToEdit.Split(',').ToList();
            if (Event.current.button == 0) // left click to add
            {
                if (!values.Any(s => s.Trim() == itemToChange))
                    stringToEdit += ',' + itemToChange;
            }
            else if (Event.current.button == 1) // right click to remove
            {
                if (values.Any(s => s.Trim() == itemToChange))
                {
                    values.Remove(itemToChange);
                    stringToEdit = "";
                    if (values.Count > 0)
                    {
                        stringToEdit = values[0];
                        for (int i = 1; i < values.Count; i++)
                            stringToEdit += ',' + values[i];
                    }
                }
            }
        }

        Rect valueRect = new Rect();
        string tooltip = "";
        bool showTooltip = false;
        void tooltipWindow(int id)
        {
            GUIStyle ttStyle = new GUIStyle(GUI.skin.textArea);
            ttStyle.clipping = TextClipping.Overflow;
            GUILayout.Label(tooltip, ttStyle);
        }

        /// <summary>
        /// used for initialising things. Just provides a working check object to quickly use
        /// </summary>
        /// <returns>type:category, value:Pods</returns>
        Check defaultCheck()
        {
            return new Check("category", "Pods");
        }
    }
}
