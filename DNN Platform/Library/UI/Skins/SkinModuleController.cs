﻿#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2014
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Tabs.TabVersions;
using DotNetNuke.Framework;

namespace DotNetNuke.UI.Skins
{
    public class SkinModuleController: ServiceLocator<ISkinModuleController, SkinModuleController>, ISkinModuleController
    {
        public ArrayList GetConfiguredModules(TabInfo tab)
        {
            var objPaneModules = new Dictionary<string, int>();

            IEnumerable<ModuleInfo> modules = GetModules(tab);
            var configuredModules = new ArrayList();
            foreach (ModuleInfo configuringModule in modules)
            {
                ConfigureModule(configuringModule, tab);

                if (objPaneModules.ContainsKey(configuringModule.PaneName) == false)
                {
                    objPaneModules.Add(configuringModule.PaneName, 0);
                }
                configuringModule.PaneModuleCount = 0;
                if (!configuringModule.IsDeleted)
                {
                    objPaneModules[configuringModule.PaneName] = objPaneModules[configuringModule.PaneName] + 1;
                    configuringModule.PaneModuleIndex = objPaneModules[configuringModule.PaneName] - 1;
                }
                configuredModules.Add(configuringModule);
            }
            foreach (ModuleInfo module in configuredModules)
            {
                module.PaneModuleCount = objPaneModules[module.PaneName];
            }

            return configuredModules;
        }

        private void ConfigureModule(ModuleInfo cloneModule, TabInfo tab)
        {
            if (Null.IsNull(cloneModule.StartDate))
            {
                cloneModule.StartDate = DateTime.MinValue;
            }
            if (Null.IsNull(cloneModule.EndDate))
            {
                cloneModule.EndDate = DateTime.MaxValue;
            }
            if (String.IsNullOrEmpty(cloneModule.ContainerSrc))
            {
                cloneModule.ContainerSrc = tab.ContainerSrc;
            }

            cloneModule.ContainerSrc = SkinController.FormatSkinSrc(cloneModule.ContainerSrc, PortalSettings.Current);
            cloneModule.ContainerPath = SkinController.FormatSkinPath(cloneModule.ContainerSrc);
        }

        private IEnumerable<ModuleInfo> GetModules(TabInfo tab)
        {
            int urlVersion;
            bool validVersion = TabVersionUtils.TryGetUrlVersion(out urlVersion);
            var showVersionMode = validVersion;
            var editView = Globals.IsEditMode();

            IEnumerable<ModuleInfo> modules = null;
            if (showVersionMode)
            {
                modules = TabVersionMaker.Instance.GetVersionModules(tab.TabID, urlVersion, true);
            }
            else if (editView)
            {
                modules = TabVersionMaker.Instance.GetUnPublishedVersionModules(tab.TabID);
            }
            else
            {
                modules = tab.ChildModules.Select(kvp => kvp.Value.Clone());
            }
            return modules;
        }

        protected override Func<ISkinModuleController> GetFactory()
        {
            return ()  => new SkinModuleController();
        }
    }
}