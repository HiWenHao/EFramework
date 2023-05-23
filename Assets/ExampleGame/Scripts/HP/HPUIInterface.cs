using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

enum HPState 
{
    ONADD,
    ONLOSE,
    ONDEFAULT
}
public class HPUIInterface : MonoBehaviour
{
    GameObject m_gHP;
    GameObject m_HPEffect;
    GameObject m_gUIRoot;
    Slider m_sHP;
    Slider m_sResHP;
    Vector3 m_currentHandPos = Vector3.zero;
    [SerializeField]HPState m_State;
    public float ResHPDonwSpeed = 5;
    float m_fHPTarget = -1.0f;
   

   
    private void Awake()
    {
        m_gUIRoot = transform.parent.gameObject;
        m_gHP = Instantiate(EF.Load.Load<GameObject>(EF.Projects.AppConst.UIPath + "UI_HP"), m_gUIRoot.transform);
        m_HPEffect = EF.Load.Load<GameObject>(EF.Projects.AppConst.UIPath + "HP_effect"); 
        m_sHP = m_gHP.transform.GetComponent<Slider>();
        m_sResHP = m_gHP.transform.Find("Fill Area/ResHP").GetComponent<Slider>();
    }
    // Start is called before the first frame update
    void Start()
    {
        init();
    }

    // Update is called once per frame
    void Update()
    {

        TEST();
        switch (m_State)
        {   
            case HPState.ONADD:
                //Do Nothing
                break;
            case HPState.ONLOSE:
                if ( m_sResHP.value >= m_fHPTarget)
                {
                    m_sResHP.value -= Time.deltaTime * ResHPDonwSpeed;
                }
                else
                {
                    m_State = HPState.ONDEFAULT;
                }
                break;
            case HPState.ONDEFAULT:
                //Do Nothing
                break;
            default:
                break;
        }
    }
    void init() 
    {
        m_sResHP.value = 100.0f;
        m_sHP.value = 100.0f;
        m_currentHandPos = m_sHP.handleRect.transform.position;
        m_State = HPState.ONDEFAULT;
    }
    public float GetResHPValu() 
    {
        return m_sResHP.value;
    }
    public void AddHP(float value, bool immediately = true) 
    {
        float _hp =  value;
        if (_hp > 100.0f)
            _hp = 100.0f;
        if (immediately)
        {
            m_sHP.value = _hp;
        }
       else
        {
            m_State = HPState.ONADD;
        }
        m_fHPTarget = _hp;
        if (m_fHPTarget>=m_sResHP.value)
        {
            m_sResHP.value = m_fHPTarget;
        }
        m_currentHandPos = m_sHP.handleRect.transform.position;

    }
    public void LoseHP(float value)
    {
        float _hp =  value;
        float _damage = m_sHP.value - value;
        if (_hp < 0)
            _hp = 0;
        m_sHP.value = _hp;
        if (m_sResHP.value > m_fHPTarget&& m_fHPTarget!=-1.0f)
        {
            m_sResHP.value = m_fHPTarget;
        }
        m_fHPTarget = _hp;
        //constantDown();
        creatDownEffect(_damage, m_sHP.handleRect.transform.position);
        m_currentHandPos = m_sHP.handleRect.transform.position;
        m_State = HPState.ONLOSE;
    }
    #region EFFECT
    void constantDown()
    {
        List<Vector3> _respawns = new List<Vector3>();
        List<Vector3> _offsets = new List<Vector3>();

        float _xx = m_currentHandPos.x - m_sHP.handleRect.transform.position.x;
        int _width = (int)(((1f / 100f) * 600f));
        int _num = (int)(_xx / _width);
        for (int i = 0; i < _num; i++)
        {
            Vector3 _spawnPos = m_currentHandPos - new Vector3(_width * i, 0, 0);
            Vector3 _offset = _spawnPos + new Vector3(40, -40, 0);
            _offsets.Add(_offset);
            _respawns.Add(_spawnPos);
        }
        StartCoroutine(iHPEffectSpawn(_respawns, _offsets));
    }

    void creatDownEffect(float width, Vector3 spawn)
    {
        float _width = (width / 100.0f) * 600f;
        GameObject _obj = Instantiate(m_HPEffect, m_gUIRoot.transform);
        _obj.transform.position = spawn;
        RectTransform _Rt = _obj.transform.GetComponent<RectTransform>();
        _Rt.sizeDelta = new Vector2(_width, 40f);
        Vector3 _offset = new Vector3(_obj.transform.position.x + 40, _obj.transform.position.y + 40, 0);
        StartCoroutine(iHPEffect(_obj, _offset));
    }
    IEnumerator iHPEffectSpawn(List<Vector3> _respawns, List<Vector3> _offsets)
    {
        for (int i = 0; i < _respawns.Count; i++)
        {
            yield return new WaitForSeconds(0.01f);
            creatDownEffect(1, _respawns[i]);

        }
    }
    IEnumerator iHPEffect(GameObject _rt, Vector3 _offset)
    {
        float _min = 1f;
        float _max = 2f;
        while (_rt.transform.position != _offset)
        {
            if (_max >= _min)
            {
                _max -= Time.deltaTime * 10;
            }
            _rt.transform.localScale = Vector3.one * _max;
            _rt.transform.position = Vector2.MoveTowards(_rt.transform.position, _offset, Time.deltaTime * 100);
            yield return null;

        }
        Destroy(_rt.gameObject);
    }

    #endregion
    //FOR TEST
    float ____HP = 100.0f;
    void TEST() 
    {
        int _hrut = UnityEngine.Random.Range(1, 5);
        if (Input.GetKeyDown(KeyCode.A))
        {
            
            ____HP -= _hrut;
            if (____HP<0)
            {
                ____HP = 0;
            }
            LoseHP(____HP);
           
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
         
            if (GetResHPValu()>=____HP )
            {
                ____HP += 1;
                if (____HP > 100)
                {
                    ____HP = 100;
                }
                AddHP(____HP, true);

            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            ____HP += 10f * Time.deltaTime;
            if (____HP > 100)
            {
                ____HP = 100;
            }
            AddHP(____HP, true);
        }
    }
   
}
