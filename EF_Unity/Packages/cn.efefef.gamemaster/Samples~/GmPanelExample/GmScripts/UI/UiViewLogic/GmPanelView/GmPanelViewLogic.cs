/*
 * ================================================
 * Describe:        GM示例总面板
 * Author:          Alvin5100
 * CreationTime:    2026-11-24 18:36:41
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-25 13:49:52
 * ScriptVersion:   0.1
 * ================================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.GameMaster;
using EasyFramework.Ui;

namespace ExampleGame
{
    public struct SectionChanged
    {
        public string CurrentSection;
    }

    /// <summary>
    /// GM示例总面板
    /// </summary>
    public partial class GmPanelView : IUiEnable
    {
        void IUiView.Awake()
        {
            GmData.Init();
            EFC.GetOrCreate<EventManager>().Subscribe<SectionChanged>(OnSectionChanged);
        }

        public void Enable()
        {
            RefreshAll();
        }

        void IUiView.Quit()
        {
            GmData.SetToNull();
            EFC.GetOrCreate<EventManager>().UnsubscribeAll<SectionChanged>();
        }

        #region Event

        private void OnSectionChanged(SectionChanged section) => RefreshSection(section.CurrentSection);

        #endregion

        #region Helper

        private void RefreshAll()
        {
            GmData.Clear();
            string firstSection = "";
            List<GmCommandInfo> all = GameMasterManager.GetInfos();
            foreach (var item in all)
            {
                if (!GmData.SectionList.Contains(item.Section))
                {
                    if (string.IsNullOrEmpty(firstSection))
                        firstSection = item.Section;
                    GmData.SectionList.Add(item.Section);
                    GmData.Map.Add(item.Section, new List<GmCommandInfo>());
                }

                GmData.Map[item.Section].Add(item);
            }

            ScrList_SectionList.Initialize(GmData.SectionList.Count);
            RefreshSection(firstSection);
        }

        private void RefreshSection(string section)
        {
            GmData.CurrentCommandList = GmData.Map[section];
            ScrList_CommandList.Initialize(GmData.CurrentCommandList.Count);
            D.Log($"Section changed to: {section}, Count: {GmData.CurrentCommandList.Count}");
        }

        #endregion

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_CloseButton()
        {
            EFC.GetOrCreate<UiManager>().CloseView(this).Forget();
        }

        private void OnClickBtn_RefreshButton()
        {
            D.Emphasize($"OnClickBtn_RefreshButton()--------------------------");
            RefreshAll();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}