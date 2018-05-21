using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AnimationPanel : MonoBehaviour
{
    public GameObject panel;

    public Texture2D iconPlay;
    public Texture2D iconPause;
    public RawImage imagePlayPause;

    public Slider slider;
    public Text text;

    public PdxAnimationPlayer player;
    
    bool tPose = false;
    bool firstChange = true;
    // Use this for initialization
    void Start()
    {
        panel.SetActive(false);
    }

    public void Hide()
    {
        panel.SetActive(false);
        player = null;
    }

    public void SetPlayer(PdxAnimationPlayer p)
    {
        firstChange = true;

        if (p == null)
        {
            Hide();
        }
        else
        {
            panel.SetActive(true);
            player = p;

            slider.minValue = 1;
            slider.maxValue = p.animationData.sampleCount;

            imagePlayPause.texture = iconPause;
        }
    }

    public void ClickPlayPause()
    {
        tPose = false;

        if (player.playing)
        {
            player.playing = false;
            imagePlayPause.texture = iconPlay;
        }
        else
        {
            player.playing = true;
            imagePlayPause.texture = iconPause;
        }
    }

    public void ClickStop()
    {
        player.SetTpose();
        player.playing = false;

        imagePlayPause.texture = iconPlay;

        tPose = true;
    }

    public void SliderChange()
    {
        if(!firstChange) player.SetFrame((int)slider.value);

        firstChange = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(player == null)
        {
            SetPlayer(null);
            return;
        }

        if (!panel.activeSelf) return;

        slider.SetValue(player.currentKey);
        text.text = tPose ? "TPose" : player.currentKey + "/" + player.animationData.sampleCount;
    }
}
