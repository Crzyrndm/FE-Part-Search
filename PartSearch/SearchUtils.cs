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
                    int i = 0;
                    return int.TryParse(ToParse, out i); // integer type values
                case CheckType.mass:
                case CheckType.crashTolerance:
                case CheckType.cost:
                case CheckType.maxTemp:
                    if (ToParse.Last() == '.')
                        return true;
                    float f = 0;
                    return float.TryParse(ToParse, out f); // float or double type values
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

        public static GUIContent[] buildContentFromEnum(Check check, Type enumType)
        {
            int arraySize = Enum.GetValues(enumType).Length;
            GUIContent[] content = new GUIContent[arraySize];
            for (int i = 0; i < arraySize; i++)
                content[i] = new GUIContent(Enum.Parse(enumType, i.ToString()).ToString());
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
    }
}