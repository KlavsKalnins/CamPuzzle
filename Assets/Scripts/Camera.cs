using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class Camera : MonoBehaviour
{
    public static Camera instance;
    private WebCamTexture webCamTexture;
    [SerializeField] private GameObject cameraHeader;
    [SerializeField] private GameObject cameraButton;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private List<string> deviceNames;
    public static Action<Texture2D> OnTookPhoto;
    [SerializeField] private int cropSize = 480; // pc 700
    [SerializeField] private int xPictureOffset = 0; // pc 300
    Vector3 rotationVector = new Vector3(0f, 0f, 0f);
    [SerializeField] private Texture2D finalImage;
    private void OnEnable()
    {
        instance = this;
        Application.RequestUserAuthorization(UserAuthorization.WebCam);
        webCamTexture = new WebCamTexture();
        rawImage.texture = webCamTexture;
        //
        var webCamDevices = WebCamTexture.devices;
        if (webCamDevices.Length > 1)
            webCamTexture.deviceName  = webCamDevices[1].name;
        FixRenderingObjRot();
    }

    private void FixRenderingObjRot()
    {
        webCamTexture.Play();
        rotationVector.z = -webCamTexture.videoRotationAngle;
        rawImage.rectTransform.localEulerAngles = rotationVector;
        PuzzleManager.instance.rotateTiles = webCamTexture.videoRotationAngle;
        //var rt = rawImage.gameObject.GetComponent<RectTransform>();
        //rt.sizeDelta = new Vector2(webCamTexture.width, webCamTexture.height);
    }
    
    void Start()
    {
        StartCoroutine(GetAllCams());
    }
    IEnumerator GetAllCams()
    {
        yield return new WaitForSeconds(2);
        if (WebCamTexture.devices.Length > 0)
        {
            for (int i = 0; i < WebCamTexture.devices.Length; i++)
            {
                Debug.Log("Dev: " + WebCamTexture.devices[i]);
                var device = WebCamTexture.devices[i];
                deviceNames.Add(device.name);
                GameObject inst = Instantiate(cameraButton, transform.position, quaternion.identity);
                inst.transform.SetParent(cameraHeader.transform);
                inst.GetComponent<Button>().onClick.AddListener(delegate { SetCameraDevice(device.name); });
            }
        }
    }

    private void SetCameraDevice(string device)
    {
        webCamTexture.Stop();
        Debug.Log("Name: " + device);
        webCamTexture.deviceName = device;
        FixRenderingObjRot();
    }
    public void TakePicture()
    {
        StartCoroutine(TakePhoto());
    }
    
    IEnumerator TakePhoto()
    {
        yield return new WaitForEndOfFrame();
        Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height);
        photo.SetPixels(webCamTexture.GetPixels());
        photo.Apply();
        
        byte[] bytes = photo.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + "/photo.png", bytes);
        Debug.Log(Application.persistentDataPath + "/photo.png" + "    #DONE");

        finalImage = ImageManipulation(photo);
        OnTookPhoto?.Invoke(finalImage);
        webCamTexture.Stop();
        GameManager.instance.ToggleGameObjects(gameObject,false);
    }

    private Texture2D ImageManipulation(Texture2D img)
    {
        // CROPING
        cropSize = img.height;
        xPictureOffset = (img.width - img.height) / 2; // fit x-axis middle
        Color[] c = img.GetPixels (xPictureOffset, 0, cropSize, cropSize);
        img = new Texture2D (cropSize, cropSize);
        img.SetPixels (c);
        img.Apply();
        // Rotating
        int rotAngle;
        if (PuzzleManager.instance.rotateTiles >= 270)
            rotAngle = 0;
        else
            rotAngle = PuzzleManager.instance.rotateTiles * 2;
        var rotTex = new Texture2D(cropSize,cropSize);
        rotTex = RotateTexture(img, rotAngle);
        
        // Inverting
        
        Texture2D imgInv = new Texture2D (cropSize, cropSize);
        for(int i=0;i<img.width;i++){
            for(int j=0;j<img.height;j++){
                imgInv.SetPixel(img.width-i-1, j, rotTex.GetPixel(i,j));
            }
        }
        imgInv.Apply();
        
        return imgInv;
    }

    void Update()
    {
    }
    public static Texture2D ToTexture2D(Texture texture)
    {
        return Texture2D.CreateExternalTexture(
            texture.width,
            texture.height,
            TextureFormat.RGB24,
            false, false,
            texture.GetNativeTexturePtr());
    }
    
    Texture2D RotateTexture(Texture2D tex, float angle)
    {
        Debug.Log("rotating");
        Texture2D rotImage = new Texture2D(tex.width, tex.height);
        int  x,y;
        float x1, y1, x2,y2;
 
        int w = tex.width;
        int h = tex.height;
        float x0 = rot_x (angle, -w/2.0f, -h/2.0f) + w/2.0f;
        float y0 = rot_y (angle, -w/2.0f, -h/2.0f) + h/2.0f;
 
        float dx_x = rot_x (angle, 1.0f, 0.0f);
        float dx_y = rot_y (angle, 1.0f, 0.0f);
        float dy_x = rot_x (angle, 0.0f, 1.0f);
        float dy_y = rot_y (angle, 0.0f, 1.0f);
       
       
        x1 = x0;
        y1 = y0;
 
        for (x = 0; x < tex.width; x++) {
            x2 = x1;
            y2 = y1;
            for ( y = 0; y < tex.height; y++) {
                //rotImage.SetPixel (x1, y1, Color.clear);          
 
                x2 += dx_x;//rot_x(angle, x1, y1);
                y2 += dx_y;//rot_y(angle, x1, y1);
                rotImage.SetPixel ( (int)Mathf.Floor(x), (int)Mathf.Floor(y), getPixel(tex,x2, y2));
            }
 
            x1 += dy_x;
            y1 += dy_y;
           
        }
 
        rotImage.Apply();
        return rotImage;
    }
    private Color getPixel(Texture2D tex, float x, float y)
    {
        Color pix;
        int x1 = (int) Mathf.Floor(x);
        int y1 = (int) Mathf.Floor(y);
 
        if(x1 > tex.width || x1 < 0 ||
           y1 > tex.height || y1 < 0) {
            pix = Color.clear;
        } else {
            pix = tex.GetPixel(x1,y1);
        }
       
        return pix;
    }
    private float rot_x (float angle, float x, float y) {
        float cos = Mathf.Cos(angle/180.0f*Mathf.PI);
        float sin = Mathf.Sin(angle/180.0f*Mathf.PI);
        return (x * cos + y * (-sin));
    }
    private float rot_y (float angle, float x, float y) {
        float cos = Mathf.Cos(angle/180.0f*Mathf.PI);
        float sin = Mathf.Sin(angle/180.0f*Mathf.PI);
        return (x * sin + y * cos);
    }
}