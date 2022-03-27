﻿/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;
using System.Reflection;
using Facebook.WitAi.Data.Entities;

namespace Facebook.WitAi.Windows
{
    public class WitEntityPropertyDrawer : WitPropertyDrawer
    {
        // Use name value for title if possible
        protected override string GetLocalizedText(SerializedProperty property, string key)
        {
            // Determine by ids
            switch (key)
            {
                    case LocalizedTitleKey:
                        string title = GetFieldStringValue(property, "name");
                        if (!string.IsNullOrEmpty(title))
                        {
                            return title;
                        }
                        break;
                    case "id":
                        return WitStyles.Texts.ConfigurationEntitiesIdLabel;
                    case "lookups":
                        return WitStyles.Texts.ConfigurationEntitiesLookupsLabel;
                    case "roles":
                        return WitStyles.Texts.ConfigurationEntitiesRolesLabel;
            }
            
            // Default to base
            return base.GetLocalizedText(property, key);
        }
        // Determine if should layout field
        protected override bool ShouldLayoutField(SerializedProperty property, FieldInfo subfield)
        {
            switch (subfield.Name)
            {
                case "name":
                case "keywords":
                    return false;
            }
            return base.ShouldLayoutField(property, subfield);
        }
    }
}
