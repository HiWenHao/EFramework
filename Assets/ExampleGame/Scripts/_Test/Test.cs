using EasyFramework;
using EasyFramework.UI;
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
                EF.Timer.AddCountdownEvent(1.0f, delegate
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
            if (false)                      //  if want to test, plase change false to true.
            {
                EAction timeAction = () =>
                {
                    D.Log("TimerEvent 3f");
                };
                EF.Timer.AddTimeEvent(1f, () => { D.Log("TimerEvent 1f"); });
                EF.Timer.AddTimeEvent(3f, timeAction);
                EF.Timer.AddTimeEvent(5f, () => { D.Log("TimerEvent 5f"); });
                EF.Timer.RemoveTimeEvent(timeAction);

                EAction countdownAction = () =>
                {
                    D.Error("countdownEvent 4f");
                };
                EF.Timer.AddCountdownEvent(2f, () => { D.Error("countdownEvent 2f"); });
                EF.Timer.AddCountdownEvent(4f, countdownAction);
                EF.Timer.AddCountdownEvent(6f, () => { D.Error("countdownEvent 6f"); });
                EF.Timer.RemoveCountdownEvent(countdownAction);
            }
            #endregion
        }
        // Update is called once per frame
        void Update()
        {
            return;
            #region SourcesManager Test     Q - Y
            if (Input.GetKeyDown(KeyCode.Q))
            {
                EF.Sources.PlayBGMByName("BGM", false);
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                EF.Sources.Play3DEffectSouceByName("Haoheng", new Vector3(0, 1, 1), () => { D.Log("The music is play done"); });
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                EF.Sources.Play2DEffectSouceByName("Haoheng", () => { D.Log("The music is play done 2D"); });
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                EF.Sources.Play2DEffectSouceByName("Haoheng", () => { D.Log("The music is play done 2DDDDD"); });
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                EF.Sources.PauseAll();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                EF.Sources.UnPauseAll();
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
                EF.Ui.ShowDialog("--------���������������ԣ�����OK", delegate 
                {
                    D.Log("OK action is callback");
                });
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                EF.Ui.ShowDialog("--------���������������ԣ�����NO", null, delegate
                {
                    D.Log("No action is callback");
                }, openCloseBG: false);
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                string msg = "����������ʾ������";
                for (int i = UnityEngine.Random.Range(0,15) - 1; i >= 0; i--)
                {
                    msg += "��";
                }
                EF.Ui.ShowPopup(msg);
            }
            #endregion

            #region TimeManager Test        L
            if (Input.GetKeyDown(KeyCode.L))
            {
                D.Log(EF.Timer.TotalTime + "<=== is Current time.  ");
                EF.Timer.AddCountdownEvent(2f, () => { D.Error("countdownEvent 2f"); });
            }
            #endregion

        }
    }
}