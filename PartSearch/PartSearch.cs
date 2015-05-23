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
        // List<string> folders = new List<string>();

        public void Start()
        {
            customTestCategory = new customSubCategory("testCategory", "");
            customTestCategory.filters.Add(new Filter(false));
            customTestCategory.filters[0].checks.Add(defaultCheck());
            if (btn == null)
                btn = ApplicationLauncher.Instance.AddModApplication(Toggle, Toggle, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, (Texture2D)null);
            StartCoroutine(initialise());
        }

        void Toggle()
        {
            showWindow = !showWindow;
        }

        IEnumerator initialise()
        {
            for (int i = 0; i < 20; i++)
                yield return null;
            customTestCategory.initialise(PartCategorizer.Instance.filters[0]);
            testCategory = PartCategorizer.Instance.filters[0].subcategories.First(c => c.button.categoryName == "testCategory");
            FilterExtensions.Core.setSelectedCategory(); // refresh the stupid UI
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
        }

        bool showWindow = false;
        int selectedFilter = 0;
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
            // add check
        }

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
            }
            if (GUILayout.Button("X", GUILayout.Height(60), GUILayout.Width(40)))
                ret = true;
            GUILayout.EndHorizontal();
            return ret;
        }

        bool showEditWindow = false;
        bool typeEdit = false;
        Check checkToEdit = null;
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
        void valueDisplayEdit(Check check)
        {
            string tmpstr = "";
            GUILayout.BeginHorizontal();
            switch (check.type)
            {
                case CheckType.propellant:
                case CheckType.resource:
                    GUILayout.Label("Value: ");
                    valueEdit = GUILayout.Toggle(valueEdit, checkToEdit.value, GUI.skin.button);
                    GUILayout.EndHorizontal();
                    if (valueEdit)
                    {
                        if (typeEdit)
                            typeEdit = false;
                        tmpstr = Core.Instance.resources[GUILayout.SelectionGrid((int)Core.Instance.resources.IndexOf(check.value), SearchUtils.buildContentFromList(check, Core.Instance.resources), 4)];
                    }
                    break;
                case CheckType.category:
                    GUILayout.Label("Value: ");
                    valueEdit = GUILayout.Toggle(valueEdit, checkToEdit.value, GUI.skin.button);
                    GUILayout.EndHorizontal();
                    if (valueEdit)
                    {
                        if (typeEdit)
                            typeEdit = false;
                        tmpstr = PartCategories[GUILayout.SelectionGrid((int)PartCategories.IndexOf(check.value), SearchUtils.buildContentFromList(check, PartCategories), 4)];
                    }
                    break;
                default:
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Value: ");
                    tmpstr = GUILayout.TextField(checkToEdit.value);
                    GUILayout.EndHorizontal();
                    break;
            }
            if (tmpstr != "" && check.value != tmpstr && SearchUtils.parseValueForType(check, tmpstr))
            {
                check.value = tmpstr;
                if (tmpstr.Last() != '.') // things will go bonkers if I try parse an incomplete float
                    refresh();
            }
            
        }

        Check defaultCheck()
        {
            return new Check("category", "Pods");
        }
    }
}
