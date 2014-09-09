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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework;

namespace DotNetNuke.Entities.Tabs
{
    public class TabVersionMaker : ServiceLocator<ITabVersionMaker, TabVersionMaker>, ITabVersionMaker
    {
        public void DeleteVersion(int tabId, int createdByUserID, int version)
        {
            var tabVersions = TabVersionController.Instance.GetTabVersions(tabId).OrderByDescending(tv => tv.Version);
            if (tabVersions.FirstOrDefault().Version == version)
            {
                TabVersionController.Instance.DeleteTabVersion(tabId, tabVersions.FirstOrDefault().TabVersionId);
            }
            else
            {
                for (int i = 1; i < tabVersions.Count(); i++)
                {
                    if (tabVersions.ElementAtOrDefault(i).Version == version)
                    {
                        CreateSnapshotOverVersion(tabId, tabVersions.ElementAtOrDefault(i-1));
                        TabVersionController.Instance.DeleteTabVersion(tabId, tabVersions.ElementAtOrDefault(i).TabVersionId);
                        return;
                    }
                }
            }
        }

        public TabVersion RollBackVesion(int tabId, int createdByUserID, int version)
        {
            var rollbackDetails = CopyVersionDetails(GetVersionModulesInternal(tabId, version));
            
            var newVersion = CreateNewVersion(tabId, createdByUserID);
            TabVersionDetailController.Instance.SaveTabVersionDetail( new TabVersionDetail
            {
                PaneName = "none_resetAction",
                TabVersionId = newVersion.TabVersionId,
                Action = TabVersionDetailAction.Reset
            }, createdByUserID);

            foreach (var rollbackDetail in rollbackDetails)
            {
                rollbackDetail.TabVersionId = newVersion.TabVersionId;
                TabVersionDetailController.Instance.SaveTabVersionDetail(rollbackDetail, createdByUserID);
            }

            return newVersion;
        }

        private IEnumerable<TabVersionDetail> CopyVersionDetails(IEnumerable<TabVersionDetail> tabVersionDetails)
        {
            var result = new List<TabVersionDetail>();
            foreach (var tabVersionDetail in tabVersionDetails)
            {
                result.Add(new TabVersionDetail
                {
                    ModuleId = tabVersionDetail.ModuleId,
                    ModuleOrder = tabVersionDetail.ModuleOrder,
                    ModuleVersion = tabVersionDetail.ModuleVersion,
                    PaneName = tabVersionDetail.PaneName,
                    Action = tabVersionDetail.Action
                });
            }
            return result;
        }

        public TabVersion CreateNewVersion(int tabId, int createdByUserID) 
        {
            //TODO Get this value from Settings
            var maxVersionsAllowed = 5;
            var tabVersionsOrdered = TabVersionController.Instance.GetTabVersions(tabId).OrderByDescending(tv => tv.Version);
            var tabVersionCount = tabVersionsOrdered.Count();
            if (tabVersionCount >= maxVersionsAllowed)
            {
                //The last existing version is going to be deleted, therefore we need to add the snapshot to the previous one
                var snapShotTabVersion = tabVersionsOrdered.ElementAtOrDefault(maxVersionsAllowed - 2);
                CreateSnapshotOverVersion(tabId, snapShotTabVersion);
                DeleteOldVersions(tabVersionsOrdered, snapShotTabVersion);
            }

            return TabVersionController.Instance.CreateTabVersion(tabId, createdByUserID);
        }

        private void CreateSnapshotOverVersion(int tabId, TabVersion snapshoTabVersion)
        {
            var snapShotTabVersionDetails = GetVersionModulesInternal(tabId, snapshoTabVersion.Version);

            var existingTabVersionDetails = TabVersionDetailController.Instance.GetTabVersionDetails(snapshoTabVersion.TabVersionId);
            for (int i = existingTabVersionDetails.Count(); i > 0; i--)
            {
                var existingDetail = existingTabVersionDetails.ElementAtOrDefault(i - 1);
                if (snapShotTabVersionDetails.All(tvd => tvd.TabVersionDetailId != existingDetail.TabVersionDetailId))
                {
                    TabVersionDetailController.Instance.DeleteTabVersionDetail(existingDetail.TabVersionId, existingDetail.TabVersionDetailId);
                }
            }

            foreach (var tabVersionDetail in snapShotTabVersionDetails)
            {
                tabVersionDetail.TabVersionId = snapshoTabVersion.TabVersionId;
                TabVersionDetailController.Instance.SaveTabVersionDetail(tabVersionDetail);
            }

        }

        private void DeleteOldVersions(IEnumerable<TabVersion> tabVersionsOrdered, TabVersion snapShotTabVersion)
        {
            var oldVersions = tabVersionsOrdered.Where(tv => tv.Version < snapShotTabVersion.Version);
            for (int i = oldVersions.Count(); i > 0; i--)
            {
                var oldVersion = oldVersions.ElementAtOrDefault(i - 1);
                var oldVersionDetails = TabVersionDetailController.Instance.GetTabVersionDetails(oldVersion.TabVersionId);
                for (int j = oldVersionDetails.Count(); j > 0; j--)
                {
                    var oldVersionDetail = oldVersionDetails.ElementAtOrDefault(j - 1);
                    TabVersionDetailController.Instance.DeleteTabVersionDetail(oldVersionDetail.TabVersionId, oldVersionDetail.TabVersionDetailId);
                }
                TabVersionController.Instance.DeleteTabVersion(oldVersion.TabId, oldVersion.TabVersionId);
            }
        }

        public IEnumerable<ModuleInfo> GetVersionModules(int tabId, int version, bool ignoreCache = false)
        {
            //if we are not using the cache
            if (ignoreCache || Host.Host.PerformanceSetting == Globals.PerformanceSettings.NoCaching)
            {
                return convertToModuleInfo(GetVersionModulesInternal(tabId, version), ignoreCache);
            }

            string cacheKey = string.Format(DataCache.TabVersionModulesCacheKey, tabId, version);
            return CBO.GetCachedObject<List<ModuleInfo>>(new CacheItemArgs(cacheKey,
                                                                    DataCache.TabVersionModulesTimeOut,
                                                                    DataCache.TabVersionModulesPriority),
                                                            c =>
                                                            {
                                                                return convertToModuleInfo(GetVersionModulesInternal(tabId, version), ignoreCache);
                                                            });
        }

        public IEnumerable<ModuleInfo> GetLastUnPublishedVersionModules(int tabId)
        {
            var tab = TabVersionController.Instance.GetLastUnPublishedVersionModules(tabId);
            if (tab == null)
            {
                return CBO.FillCollection<ModuleInfo>(DataProvider.Instance().GetTabModules(tabId));
            }

            var tabVersionDetails = TabVersionDetailController.Instance.GetVersionHistory(tabId, tab.TabVersionId);
            return convertToModuleInfo(GetSnapShot(tabVersionDetails), true);
        }

        public IEnumerable<ModuleInfo> GetCurrentModules(int tabId, bool ignoreCache = false)
        {
            //if we are not using the cache
            if (ignoreCache || Host.Host.PerformanceSetting == Globals.PerformanceSettings.NoCaching)
            {
                return GetCurrentModulesInternal(tabId, true);
            }

            string cacheKey = string.Format(DataCache.TabVersionModulesCacheKey, tabId);
            return CBO.GetCachedObject<List<ModuleInfo>>(new CacheItemArgs(cacheKey,
                                                                    DataCache.TabVersionModulesTimeOut,
                                                                    DataCache.TabVersionModulesPriority),
                                                            c =>
                                                            {
                                                                return GetCurrentModulesInternal(tabId, true);
                                                            });
        }

        private IEnumerable<ModuleInfo> convertToModuleInfo(IEnumerable<TabVersionDetail> details, bool ignoreCache)
        {
            var modules = new List<ModuleInfo>();
            foreach (var detail in details)
            {
                var module = ModuleController.Instance.GetModule(detail.ModuleId, Null.NullInteger, ignoreCache);
                if (module == null)
                {
                    continue;
                }

                ModuleInfo cloneModule = module.Clone();
                cloneModule.ModuleVersion = detail.ModuleVersion;
                cloneModule.PaneName = detail.PaneName;
                cloneModule.ModuleOrder = detail.ModuleOrder;
                modules.Add(cloneModule);
            };

            return modules;
        }

        private IEnumerable<ModuleInfo> GetCurrentModulesInternal(int tabId, bool ignoreCache)
        {
            var tabVersion = TabVersionController.Instance.GetCurrentTabVersion(tabId);

            if (tabVersion == null)
            {
                return CBO.FillCollection <ModuleInfo>(DataProvider.Instance().GetTabModules(tabId));
            }

            var tabVersionDetails = TabVersionDetailController.Instance.GetVersionHistory(tabId,tabVersion.Version);
            // TODO: delete the mock data.
            //tabVersionDetails = new List<TabVersionDetail>
            //{
            //     new TabVersionDetail {ModuleId = 368, ModuleOrder = 2,PaneName = "ContentPane", ModuleVersion = Null.NullInteger},
            //     new TabVersionDetail {ModuleId = 485, ModuleOrder = 1,PaneName = "ContentPane"},
            //     new TabVersionDetail {ModuleId = 483, ModuleOrder = 3,PaneName = "ContentPane", ModuleVersion = Null.NullInteger},
            //     new TabVersionDetail {ModuleId = 484, ModuleOrder = 1,PaneName = "leftPane", ModuleVersion = Null.NullInteger},
            //};

            return convertToModuleInfo(GetSnapShot(tabVersionDetails), ignoreCache);
        }

        private IEnumerable<TabVersionDetail> GetVersionModulesInternal(int tabId, int version)
        {
            var tabVersionDetails = TabVersionDetailController.Instance.GetVersionHistory(tabId, version);
            // TODO: delete the mock data.
            //tabVersionDetails = new List<TabVersionDetail>
            //{
            //     new TabVersionDetail {ModuleId = 368, ModuleOrder = 2,PaneName = "ContentPane", ModuleVersion = Null.NullInteger},
            //     new TabVersionDetail {ModuleId = 485, ModuleOrder = 1,PaneName = "ContentPane", ModuleVersion = 62},
            //     new TabVersionDetail {ModuleId = 483, ModuleOrder = 3,PaneName = "ContentPane", ModuleVersion = Null.NullInteger},
            //     new TabVersionDetail {ModuleId = 484, ModuleOrder = 1,PaneName = "leftPane", ModuleVersion = Null.NullInteger},
            //};

            return GetSnapShot(tabVersionDetails);
        }

        private static IEnumerable<TabVersionDetail> GetSnapShot(IEnumerable<TabVersionDetail> tabVersionDetails)
        {
            var versionModules = new Dictionary<int, TabVersionDetail>();
            foreach (var tabVersionDetail in tabVersionDetails)
            {
                switch (tabVersionDetail.Action)
                {
                    case TabVersionDetailAction.Added:
                    case TabVersionDetailAction.Modified:
                        if (versionModules.ContainsKey(tabVersionDetail.ModuleId))
                        {
                            versionModules[tabVersionDetail.ModuleId] = JoinVersionDetails(versionModules[tabVersionDetail.ModuleId], tabVersionDetail);
                        }
                        else
                        {
                            versionModules.Add(tabVersionDetail.ModuleId, tabVersionDetail);
                        }
                        break;
                    case TabVersionDetailAction.Deleted:
                        if (versionModules.ContainsKey(tabVersionDetail.ModuleId))
                        {
                            versionModules.Remove(tabVersionDetail.ModuleId);
                        }
                        break;
                    case TabVersionDetailAction.Reset:
                        versionModules.Clear();
                        break;
                }
            }

            return versionModules.Values.ToList();
        }

        private static TabVersionDetail JoinVersionDetails(TabVersionDetail tabVersionDetail, TabVersionDetail newVersionDetail)
        {
            //Movement changes have not ModuleVersion
            if (newVersionDetail.ModuleVersion == Null.NullInteger)
            {
                newVersionDetail.ModuleVersion = tabVersionDetail.ModuleVersion;
            }

            return newVersionDetail;
        }

        protected override Func<ITabVersionMaker> GetFactory()
        {
            return () => new TabVersionMaker();
        }
    }
}
