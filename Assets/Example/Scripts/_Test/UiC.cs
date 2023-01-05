/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-09-13 20:37:03
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-09-13 20:37:03
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework;
using EasyFramework.UI;
using UnityEngine;
using UnityEngine.UI;
using XHTools;

namespace GMTest
{
    public class UiC : UIPageBase
    {
        Button btn_QuitC;
        Text txt_BExist, txt_TotalTime;
        Button btn_AddTimeEvent, btn_RemoveTimeEvent;
        Button btn_AddCountdownEvent1, btn_AddCountdownEvent3, btn_AddCountdownEvent5, btn_RemoveCountdownEvent;
        Slider sld_Timer;

        EAction timeEvent, CountdownEvent;
        public override void Awake(GameObject obj, params object[] args)
        {
            D.Warning("C init" + obj.transform.childCount);

            txt_BExist = obj.transform.Find("txt_BExist").GetComponent<Text>();
            btn_QuitC = obj.transform.Find("btn_QuitC").GetComponent<Button>();


            txt_TotalTime = obj.transform.Find("txt_TotalTime").GetComponent<Text>();
            btn_AddTimeEvent = obj.transform.Find("btn_AddTimeEvent").GetComponent<Button>();
            btn_RemoveTimeEvent = obj.transform.Find("btn_RemoveTimeEvent").GetComponent<Button>();
            btn_AddCountdownEvent1 = obj.transform.Find("btn_AddCountdownEvent1").GetComponent<Button>();
            btn_AddCountdownEvent3 = obj.transform.Find("btn_AddCountdownEvent3").GetComponent<Button>();
            btn_AddCountdownEvent5 = obj.transform.Find("btn_AddCountdownEvent5").GetComponent<Button>();
            btn_RemoveCountdownEvent = obj.transform.Find("btn_RemoveCountdownEvent").GetComponent<Button>();

            sld_Timer = obj.transform.Find("sld_Timer").GetComponent<Slider>();

            timeEvent = delegate { D.Warning("TimerEvent is done.   10f"); };
            CountdownEvent = delegate { D.Warning("CountdownEvent is done.   3f"); };

            #region AddListener
            btn_QuitC.onClick.AddListener(delegate
            {
                EF.Ui.Pop("C page is exit, also pass args to current page.");
            });

            btn_AddTimeEvent.onClick.AddListener(delegate
            {
                D.Warning("Add timerEvent with 10.0f");
                EF.Timer.AddTimeEvent(10f, timeEvent);
            });
            btn_RemoveTimeEvent.onClick.AddListener(delegate
            {
                EF.Timer.RemoveTimeEvent(timeEvent);
            });

            btn_AddCountdownEvent1.onClick.AddListener(delegate
            {
                D.Log("Add countdownEvent with 1.0f");
                EF.Timer.AddCountdownEvent(1.0f, delegate { D.Log("CountdownEvent is done.   1.0f"); });
            });
            btn_AddCountdownEvent3.onClick.AddListener(delegate
            {
                D.Warning("Add countdownEvent with 3.0f");
                EF.Timer.AddCountdownEvent(3.0f, CountdownEvent);
            });
            btn_AddCountdownEvent5.onClick.AddListener(delegate
            {
                D.Correct("Add countdownEvent with 5.0f");
                EF.Timer.AddCountdownEvent(5.0f, delegate { D.Correct("CountdownEvent is done.   5.0f"); });
            });
            btn_RemoveCountdownEvent.onClick.AddListener(delegate
            {
                EF.Timer.RemoveCountdownEvent(CountdownEvent);
            });

            sld_Timer.onValueChanged.AddListener(EF.Timer.SetTimeScale);
            #endregion

            txt_BExist.gameObject.SetActive((bool)args[0]);
            foreach (var item in args)
            {
                D.Log($"C enter  {item}");
            }
        }

        public override void Update()
        {
            txt_TotalTime.text = $"当前游戏已运行 {EF.Timer.TotalTime} s";
        }

        public override void OnPause(bool isPause, params object[] args)
        {
            D.Warning("C pause");
        }

        public override void Quit()
        {
            btn_QuitC.onClick.RemoveAllListeners();
            btn_QuitC = null;
            txt_BExist = null;
            D.Warning("C quit");
        }
    }
}
