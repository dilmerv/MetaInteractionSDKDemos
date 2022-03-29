/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Data.Configuration;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.Windows
{
    public abstract class BaseWitWindow : EditorWindow
    {
        // Scroll offset
        private Vector2 ScrollOffset;

        // Override values
        protected abstract GUIContent Title { get; }
        protected virtual Texture2D HeaderIcon => WitStyles.HeaderIcon;
        protected virtual string HeaderUrl => WitStyles.WitUrl;

        // Window open
        protected virtual void OnEnable()
        {
            WitStyles.Init();
            titleContent = Title;
            WitConfigurationUtility.ReloadConfigurationData();
        }
        // Window close
        protected virtual void OnDisable()
        {
            ScrollOffset = Vector2.zero;
        }
        // Handle Layout
        protected virtual void OnGUI()
        {
            Vector2 size;
            WitEditorUI.LayoutWindow(titleContent.text, HeaderIcon, HeaderUrl, LayoutContent, ref ScrollOffset, out size);
        }
        // Draw content of window
        protected abstract void LayoutContent();
    }
}
