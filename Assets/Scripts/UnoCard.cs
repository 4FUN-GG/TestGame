using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnoCard : MonoBehaviour
{
    public UnoGame.UnoGameCardInfo cardInfo;


    public void SetCardInfo(UnoGame.UnoGameCardInfo cardInfo)
    {
        this.cardInfo = cardInfo;
        GetComponent<Image>().sprite = ColorStringToSprite(cardInfo.color);
        if (cardInfo.number  < 10)
        {
            TextMeshProUGUI upperNumber = transform.Find("Upper").GetComponent<TextMeshProUGUI>();
            upperNumber.text = cardInfo.number.ToString();

            TextMeshProUGUI middleNumber = transform.Find("Middle").GetComponent<TextMeshProUGUI>();
            middleNumber.text = cardInfo.number.ToString();
            middleNumber.color = ColorStringToColor(cardInfo.color);

            TextMeshProUGUI lowerNumber = transform.Find("Lower").GetComponent<TextMeshProUGUI>();
            lowerNumber.text = cardInfo.number.ToString();
        }
        else if(cardInfo.number == 13 || cardInfo.number == 14)
        {
            Image cardBackground = GetComponent<Image>();
            cardBackground.sprite = ColorStringToSprite(cardInfo.color);
        }
        else
        {
            Image middleImage = transform.Find("Middle").GetComponent<Image>();
            middleImage.color = ColorStringToColor(cardInfo.color);
        }
    }

    public Sprite ColorStringToSprite(string color)
    {
        switch (color)
        {
            case "Green":
                return Resources.Load<Sprite>("card_green_front");
            case "Blue":
                return Resources.Load<Sprite>("card_blue_front");
            case "Red":
                return Resources.Load<Sprite>("card_red_front");
            case "Yellow":
                return Resources.Load<Sprite>("card_yellow_front");
            default:
                return Resources.Load<Sprite>("card_black_front");
        }
    }

    public Color ColorStringToColor(string color)
    {
        switch (color)
        {
            case "Green":
                return new Color(0, 0.784f, 0.055f, 1);
            case "Blue":
                return new Color(0, 0.024f, 1, 1);
            case "Red":
                return new Color(0.929f, 0, 0, 1);
            case "Yellow":
                return new Color(1, 0.78f, 0, 1);
            default:
                return Color.black;
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
