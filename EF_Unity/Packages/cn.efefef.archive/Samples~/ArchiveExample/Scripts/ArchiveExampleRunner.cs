/*
 * ================================================
 * Describe:      存档系统使用示例。Button 驱动，演示创建槽位、读写存档、
 *                自动保存、异常处理、删除操作。
 * Author:        Alvin8412
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-24 23:57:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Systems.Archive.Example
{
    public class ArchiveExampleRunner : MonoBehaviour
    {
        [Header("UI References")]
        public Text statusText;
        public Text slotInfoText;

        [Header("Demo Data")]
        [SerializeField] private int demoGold = 100;
        [SerializeField] private string demoItem = "Sword";
        
        [Header("Buttons")]
        [SerializeField]private Button _btnCreateSlot;
        [SerializeField]private Button _btnSaveData;
        [SerializeField]private Button _btnLoadData;
        [SerializeField]private Button _btnDeleteSlot;
        [SerializeField]private Button _btnListSlots;
        [SerializeField]private Button _btnAutoSaveDirty;

        private void Start()
        {
            _btnCreateSlot.onClick.AddListener(Btn_CreateSlot);
            _btnSaveData.onClick.AddListener(Btn_SaveData);
            _btnLoadData.onClick.AddListener(Btn_LoadData);
            _btnDeleteSlot.onClick.AddListener(Btn_DeleteSlot);
            _btnListSlots.onClick.AddListener(Btn_ListSlots);
            _btnAutoSaveDirty.onClick.AddListener(Btn_AutoSaveDirty);

            Log("ArchiveExample ready. Press buttons to test.");
        }

        private void Btn_CreateSlot()     => DemoCreateSlot().Forget();
        private void Btn_SaveData()       => DemoSaveData().Forget();
        private void Btn_LoadData()       => DemoLoadData().Forget();
        private void Btn_DeleteSlot()     => DemoDeleteSlot().Forget();
        private void Btn_ListSlots()      => DemoListSlots();
        private void Btn_AutoSaveDirty()  => DemoAutoSaveDirty().Forget();

        private async UniTask DemoCreateSlot()
        {
            try
            {
                int slotId = await ArchiveManager.Instance.CreateSlotAsync($"Demo_{DateTime.Now:HHmmss}");
                Log($"Slot created: {slotId}");
                RefreshSlots();
            }
            catch (Exception ex)
            {
                LogError($"Create failed: {ex.Message}");
            }
        }

        private async UniTask DemoSaveData()
        {
            try
            {
                var playerData = new PlayerSaveData
                {
                    playerName = "Hero",
                    level = 42,
                    gold = demoGold,
                    items = new[] { demoItem, "Potion_Red", "Amulet_Wisdom" },
                    position = new Vector3(100, 5, -30)
                };
                await ArchiveManager.Instance.SaveAsync("player_data", playerData);

                var settings = new GameSettingsData
                {
                    masterVolume = 0.8f,
                    language = "zh-CN",
                    enableVibration = true
                };
                await ArchiveManager.Instance.SaveAsync("game_settings", settings);

                Log($"Data saved to slot {ArchiveManager.Instance.ActiveSlot}.");
            }
            catch (Exception ex)
            {
                LogError($"Save failed: {ex.Message}");
            }
        }

        private async UniTask DemoLoadData()
        {
            try
            {
                var data = await ArchiveManager.Instance.LoadOrCreateAsync<PlayerSaveData>("player_data");
                Log($"Loaded: {data.playerName} Lv.{data.level} Gold:{data.gold} Items:{data.items.Length}");

                bool hasSettings = await ArchiveManager.Instance.ExistsAsync("game_settings");
                Log($"Has settings: {hasSettings}");

                if (hasSettings)
                {
                    var settings = await ArchiveManager.Instance.LoadAsync<GameSettingsData>("game_settings");
                    Log($"Volume: {settings.masterVolume}, Language: {settings.language}");
                }

                demoGold = data.gold;
                demoItem = data.items.Length > 0 ? data.items[0] : "None";
            }
            catch (ArchiveCorruptedException ex)
            {
                LogError($"Archive corrupted: {ex.FilePath}");
            }
            catch (Exception ex)
            {
                LogError($"Load failed: {ex.Message}");
            }
        }

        private async UniTask DemoDeleteSlot()
        {
            try
            {
                int slotId = ArchiveManager.Instance.ActiveSlot;
                await ArchiveManager.Instance.DeleteSlotAsync(slotId);
                Log($"Slot {slotId} deleted.");
                RefreshSlots();
            }
            catch (Exception ex)
            {
                LogError($"Delete failed: {ex.Message}");
            }
        }

        private void DemoListSlots()
        {
            var slots = ArchiveManager.Instance.GetAllSlots();
            Log($"=== All Slots ({slots.Length}) ===");
            foreach (var slot in slots)
                Log($"  Slot {slot.slotId}: {slot.slotName} | {slot.PlayTimeFormatted} | {slot.LastModifiedFormatted}");

            if (slots.Length > 0)
                ArchiveManager.Instance.ActiveSlot = slots[0].slotId;
        }

        private async UniTask DemoAutoSaveDirty()
        {
            demoGold += 50;
            ArchiveManager.Instance.MarkDirty("player_data");
            Log($"Marked 'player_data' as dirty (gold now {demoGold}). Auto-save will pick it up.");
            await UniTask.Delay(3000);
            var data = await ArchiveManager.Instance.LoadOrDefaultAsync<PlayerSaveData>("player_data");
            Log($"After potential auto-save: gold = {data?.gold ?? -1}");
        }

        private void RefreshSlots()
        {
            var slots = ArchiveManager.Instance.GetAllSlots();
            if (slotInfoText != null)
                slotInfoText.text = $"Slots: {slots.Length}\nActive: {ArchiveManager.Instance.ActiveSlot}";
        }

        private void Log(string msg)
        {
            Debug.Log($"[ArchiveExample] {msg}");
            if (statusText != null)
                statusText.text = $"[{DateTime.Now:HH:mm:ss}] {msg}\n{statusText.text}";
        }

        private void LogError(string msg)
        {
            Debug.LogError($"[ArchiveExample] {msg}");
            if (statusText != null)
                statusText.text = $"<color=red>[{DateTime.Now:HH:mm:ss}] {msg}</color>\n{statusText.text}";
        }
    }

    [Serializable]
    public class PlayerSaveData
    {
        public string playerName;
        public int level;
        public int gold;
        public string[] items;
        public Vector3 position;
    }

    [Serializable]
    public class GameSettingsData
    {
        public float masterVolume;
        public string language;
        public bool enableVibration;
    }
}
