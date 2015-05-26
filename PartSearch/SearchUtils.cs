using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PartSearch
{
    using FilterExtensions.ConfigNodes;
    public static class SearchUtils
    {
        public static bool parseValueForType(Check check, string ToParse)
        {
            switch (check.type)
            {
                case CheckType.check:
                    return ToParse == ""; // check doesn't use value, should be empty
                case CheckType.custom:
                    // enum for custom types?
                    return true;
                case CheckType.category:
                case CheckType.folder:
                case CheckType.manufacturer:
                case CheckType.moduleName:
                case CheckType.moduleTitle:
                case CheckType.partName:
                case CheckType.partTitle:
                case CheckType.path:
                case CheckType.profile:
                case CheckType.propellant:
                case CheckType.resource:
                case CheckType.tech:
                    return true; // all string values
                case CheckType.crew:
                case CheckType.size:
                    {
                        string[] values = ToParse.Split(',');
                        int i = 0;
                        bool test = true;
                        foreach (string s in values)
                            test &= int.TryParse(s.Trim(), out i);
                        return test; // integer type values
                    }
                case CheckType.mass:
                case CheckType.crashTolerance:
                case CheckType.cost:
                case CheckType.maxTemp:
                    {
                        string[] values = ToParse.Split(',');
                        float f = 0;
                        bool test = true;
                        foreach (string s in values)
                        {
                            if (s.Trim().Last() == '.')
                                continue;
                            test &= float.TryParse(s.Trim(), out f);
                        }
                        return test; // float or double type values
                    }
                default:
                    return true;
            }
        }

        public static string defaultValueForType(Check check)
        {
            switch (check.type)
            {
                case CheckType.check:
                    return ""; // check doesn't use value, should be empty
                case CheckType.custom:
                    // enum for custom types?
                    return "";
                case CheckType.category:
                    return "Pods";
                case CheckType.folder:
                    return "Squad";
                case CheckType.manufacturer:
                    return "C7 Aerospace Division";
                case CheckType.moduleName:
                    return "ModuleCommand";
                case CheckType.moduleTitle:
                    return "Command";
                case CheckType.partName:
                    return "fuelTankSmallFlat";
                case CheckType.partTitle:
                    return "FL-T100";
                case CheckType.path:
                    return "Squad/Parts/FuelTank";
                case CheckType.profile:
                    return "srf";
                case CheckType.propellant:
                    return "LiquidFuel";
                case CheckType.resource:
                    return "Oxidizer";
                case CheckType.tech:
                    return "basicRocketry";
                case CheckType.crew:
                    return "0";
                case CheckType.size:
                    return "1";
                case CheckType.mass:
                    return "1";
                case CheckType.crashTolerance:
                    return "6";
                case CheckType.cost:
                    return "42";
                case CheckType.maxTemp:
                    return "1000";
                default:
                    return "";
            }
        }

        public static GUIContent[] buildContentFromEnum(Check check, Type toBuild)
        {
            int arraySize = Enum.GetValues(toBuild).Length;
            GUIContent[] content = new GUIContent[arraySize];
            for (int i = 0; i < arraySize; i++)
                content[i] = new GUIContent(Enum.Parse(toBuild, i.ToString()).ToString());
            return content;
        }

        public static GUIContent[] buildContentFromList(Check check, List<string> toBuild)
        {
            int arraySize = toBuild.Count;
            GUIContent[] content = new GUIContent[arraySize];
            for (int i = 0; i < arraySize; i++)
                content[i] = new GUIContent(toBuild[i]);
            return content;
        }

        public static string buildTooltipFromList(List<string> toCheck, string toMatch, int limit)
        {
            int count = 0;
            List<string> matches = new List<string>();
            string tooltip = "";
            foreach (string s in toCheck)
            {
                if (s.ToLower().Contains(toMatch.ToLower()) && !matches.Contains(toMatch))
                {
                    tooltip += s + "\r\n";
                    matches.Add(s);
                    count++;
                    if (count >= limit)
                        break;
                }
            }
            return tooltip;
        }

        public static string buildTooltipListFromPathDict(Dictionary<string,string> toCheck, string toMatch, int limit)
        {
            int count = 0;
            List<string> matches = new List<string>();
            string tooltip = "";
            foreach (KeyValuePair<string, string> kvp in toCheck)
            {
                int splitIndex = Math.Max(toMatch.LastIndexOf("/"), 0);
                if (kvp.Value.ToLower().Contains(toMatch.ToLower()))
                {
                    string toAdd = kvp.Value.Substring(splitIndex == 0 ? splitIndex : splitIndex + 1);
                    if (toAdd.Contains('/'))
                        toAdd = toAdd.Substring(0, toAdd.IndexOf('/') + 1);
                    if (!matches.Contains(toAdd))
                    {
                        tooltip += toAdd + "\r\n";
                        matches.Add(toAdd);
                        count++;
                        if (count >= limit)
                            break;
                    }
                }
            }
            return tooltip;
        }
    }
}