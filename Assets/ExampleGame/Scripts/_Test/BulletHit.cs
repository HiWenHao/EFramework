using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletHit : MonoBehaviour
{
    // 原来的墙壁纹理
    public Texture2D m_oldwalltexture;

    // 新创建一个墙壁的纹理图片

    Texture2D m_newwalltexture;

    // 子弹的纹理图片
    public Texture2D[] m_bullettextures;
    private Texture2D m_bullettexture;



    float m_wallwidth;

    float m_wallheight;

    float m_bulletwidth;

    float m_bulletheight;

    float m_timer;

    //鼠标点击位置得到的Uv坐标 使用队里存储

    Queue UVque = new Queue();

    void Awake()
    {

        // 原来的墙壁纹理
        m_oldwalltexture = GetComponent<MeshRenderer>().materials[0].mainTexture as Texture2D;

        // 为了以后修改的纹理以后还能还原,我使用一个备份的纹理
        m_newwalltexture = Instantiate(m_oldwalltexture);

        //现在使用备份的纹理图片,这样就算修改也修改是备份的图片
        GetComponent<MeshRenderer>().materials[0].mainTexture = m_newwalltexture;

        // 拿到墙壁和子弹的纹理宽度和高度
        m_wallwidth = m_newwalltexture.width;
        m_wallheight = m_newwalltexture.height;
    }

    public void SetBulletRect(int no, int width, int height)
    {
        m_bullettexture = ScaleTexture(m_bullettextures[no], width, height);
        m_bulletwidth = m_bullettexture.width;

        m_bulletheight = m_bullettexture.height;
    }


    void Update()
    {

        // 鼠标位置--射线
        //鼠标左键实现效果1贴图
        if (Input.GetMouseButtonDown(0))
        {
            SetBulletRect(0, 32, 32);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject != null)
                {
                    StartCoroutine(DrawPixel(hit));
                }
            }
        }


        //鼠标右键实现效果2贴图
        if (Input.GetMouseButtonDown(1))
        {
            SetBulletRect(1, 32, 32);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject != null)
                {
                    StartCoroutine(DrawPixel(hit));
                }
            }
        }
    }


    IEnumerator DrawPixel(RaycastHit hit)
    {
        Vector2 uv = hit.textureCoord;

        UVque.Enqueue(uv);

        for (int i = 0; i < m_bulletwidth; i++)
        {
            for (int j = 0; j < m_bulletheight; j++)
            {
                float w = (uv.x * m_wallwidth - m_bulletwidth / 2) + i;
                float h = (uv.y * m_wallheight - m_bulletheight / 2) + j;

                Color bulletPixels = m_bullettexture.GetPixel(i, j);

                Color wallpixsls = m_newwalltexture.GetPixel((int)w, (int)h);

                //若透明通道则像素不变，使用墙面原像素，否则使用弹孔像素
                if (bulletPixels.a == 0)
                {
                    m_newwalltexture.SetPixel((int)w, (int)h, wallpixsls);
                }
                else
                {
                    m_newwalltexture.SetPixel((int)w, (int)h, bulletPixels);
                }
            }
        }

        m_newwalltexture.Apply();
        yield return m_newwalltexture;
    }

    Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);

        float incX = (1.0f / (float)targetWidth);
        float incY = (1.0f / (float)targetHeight);

        for (int i = 0; i < result.height; ++i)
        {
            for (int j = 0; j < result.width; ++j)
            {
                Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                result.SetPixel(j, i, newColor);
            }
        }

        result.Apply();
        return result;
    }
}
