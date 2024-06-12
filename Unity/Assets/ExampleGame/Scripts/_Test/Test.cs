using EasyFramework;
using EasyFramework.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace GMTest
{
    public class Test : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            if (true)
            {
                EF.Ui.Push(new UiA());
            }

            #region Scroll Rect Pro text
            if (false)
            {
                EF.Timer.AddOnce(1.0f, delegate
                {
                    ScrollRectPro _roH = GameObject.Find("Scroll View Pro_H").GetComponent<ScrollRectPro>();
                    _roH.InIt(delegate (GameObject go, int idx)
                    {
                        go.GetComponentInChildren<Text>().text = idx.ToString();
                    }, 200);
                    _roH.GoToElementPosWithIndex(20);
                    ScrollRectPro _roV = GameObject.Find("Scroll View Pro_V").GetComponent<ScrollRectPro>();
                    _roV.InIt(delegate (GameObject go, int idx)
                    {
                        go.GetComponentInChildren<Text>().text = idx.ToString();
                    }, 200);
                    _roV.GoToElementPosWithIndex(20);
                });
            }
            #endregion

            //GM.SourcesManager.PlayBGMByName("BGM", true);
            #region TimeManager Test
            if (false)
            {
                Action timeAction = () =>
                {
                    D.Log("TimerEvent 3f");
                };
                EF.Timer.AddOnce(1f, () => { D.Log("TimerEvent 1f"); });
                //EF.Timer.Add(1f, 0f, 1, () => { D.Log("TimerEvent 1f"); });   //上边的可以写成这样: 只做一次循环，循环前延时1s
                int _timeId = EF.Timer.AddOnce(3f, timeAction);
                EF.Timer.AddOnce(5f, () => { D.Log("TimerEvent 5f"); });
                EF.Timer.RemoveAt(_timeId);
            }//  if want to test, plase change false to true.
            #endregion
        }
        // Update is called once per frame
        void Update()
        {
            return;
            #region SourcesManager Test     Q - Y
            if (Input.GetKeyDown(KeyCode.Q))
            {
                EF.Audio.PlayBGMByName("BGM", false);
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                EF.Audio.Play3DEffectSouceByName("Haoheng", new Vector3(0, 1, 1), () => { D.Log("The music is play done"); });
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                EF.Audio.Play2DEffectSouceByName("Haoheng", () => { D.Log("The music is play done 2D"); });
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                EF.Audio.Play2DEffectSouceByName("Haoheng", () => { D.Log("The music is play done 2DDDDD"); });
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                EF.Audio.PauseAll();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                EF.Audio.UnPauseAll();
            }
            #endregion

            #region UIManager Test          A - J
            if (Input.GetKeyDown(KeyCode.A))
            {
                EF.Ui.Push(new UiA(), true, "Push UiA");
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                EF.Ui.Push(new UiB(), true, "UiA push UiB   params", "1", "2", "3");
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                EF.Ui.PopAndPushTo(new UiC());
            }
            if (!Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F))
            {
                EF.Ui.Pop("Pop a ui page. ", "aaaa", "bbbb", "cccc");
            }
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F))
            {
                EF.Ui.Pop(true);
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                EF.Ui.ShowDialog("--------这是在做弹窗测试，请点击OK", delegate 
                {
                    D.Log("OK action is callback");
                });
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                EF.Ui.ShowDialog("--------这是在做弹窗测试，请点击NO", null, delegate
                {
                    D.Log("No action is callback");
                }, openCloseBG: false);
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                string msg = "这是在做提示窗测试";
                for (int i = UnityEngine.Random.Range(0,15) - 1; i >= 0; i--)
                {
                    msg += "啊";
                }
                EF.Ui.ShowPopup(msg);
            }
            #endregion

            #region TimeManager Test        L
            if (Input.GetKeyDown(KeyCode.L))
            {
                D.Log(EF.Timer.TotalTime + "<=== is Current time.  ");
                EF.Timer.AddOnce(2f, () => { D.Error("countdownEvent 2f"); });
            }
            #endregion

        }
    }
}
