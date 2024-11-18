using UnityEngine;

using System.Drawing;
using UnityEngine.Experimental.Rendering;
using QRCoder.Unity;
using QRCoder;
using UnityEngine.UI;

public class QRCodeDisplay : MonoBehaviour
{
    public RawImage image;

    void Start()
    {
    }

    public void SetQRData(string url, int playerId)
    {
        GenerateQRCode(url, playerId);
    }

    public void GenerateQRCode(string url, int playerId)
    {
        QRCodeGenerator qrGenerator = new QRCodeGenerator();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode($"{url}player?playerId={playerId}", QRCodeGenerator.ECCLevel.H);
        UnityQRCode qrCode = new UnityQRCode(qrCodeData);
        Texture2D qrCodeAsTexture2D = qrCode.GetGraphic(20);
        image.texture = qrCodeAsTexture2D;
    }
}
