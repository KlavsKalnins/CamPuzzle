using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;
using Random = System.Random;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager instance;
    [SerializeField] private Texture2D image;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private GameObject puzzleUI;
    public List<Texture2D> imageTiles;
    [SerializeField] private int pieces = 8;
    [SerializeField] private float padding = 2;
    [SerializeField] private int resizedTextureInt = 270;
    [SerializeField] private GridLayoutGroup puzzleLayoutGroup;
    [SerializeField] private GameObject puzzleInstantObj;
    [SerializeField] private List<GameObject> createdPieceList;
    [SerializeField] private List<GameObject> selectedPieceList;
    [SerializeField] private TMP_Text completedText;
    public int rotateTiles;
    [SerializeField] private List<Button> pieceButtons;
    
    Vector2 touchStartPos;
    Vector2 touchEndPos;
    int st = 0;

    private void OnEnable()
    {
        instance = this;
        Camera.OnTookPhoto += GetPuzzleTexture;
    }
    private void OnDisable()
    {
        DestroyChildren();
        Camera.OnTookPhoto -= GetPuzzleTexture;
    }
    private void GetPuzzleTexture(Texture2D img)
    {
        Debug.Log("SUBSCRIBER GETPUZZLE");
        resizedTextureInt = 1080 / pieces;
        puzzleUI.SetActive(true);
        image = img;
        //rawImage.texture = image;
        image = Resize(img, resizedTextureInt,resizedTextureInt);
        SplitImageIntoTiles();
    }

    private void SplitImageIntoTiles()
    {
        imageTiles.Clear();
        float w = image.width;
        float h = image.height;
        Debug.Log("W:" + w + " /H: " + h);
        int width = (int)w / pieces;
        int height = (int)h / pieces;
        Debug.Log("TileW:" + width + " /TileH: " + height);
        
        bool perfectWidth = image.width % width == 0;
        bool perfectHeight = image.height % height == 0;
 
        int lastWidth = width;
        if(!perfectWidth)
        {
            lastWidth = image.width - ((image.width / width) * width);
        }
 
        int lastHeight = height;
        if(!perfectHeight)
        {
            lastHeight = image.height - ((image.height / height) * height);
        }
 
        int widthPartsCount = image.width / width + (perfectWidth ? 0 : 1);
        int heightPartsCount = image.height / height + (perfectHeight ? 0 : 1);

        int index = 0;
        for (int i = 0; i < widthPartsCount; i++)
        {
            for(int j = 0; j < heightPartsCount; j++)
            {
                int tileWidth = i == widthPartsCount - 1 ? lastWidth : width;
                int tileHeight = j == heightPartsCount - 1 ? lastHeight : height;
 
                Texture2D g = new Texture2D(tileWidth, tileHeight);
                g.SetPixels(image.GetPixels(i * width, j * height, tileWidth, tileHeight));
                g.Apply();
                imageTiles.Add(g);
                
                Sprite newSprite = Sprite.Create(image, new Rect(i*width, j*height, width, height), new Vector2(0.5f, 0.5f));
                GameObject n = Instantiate(puzzleInstantObj,transform.position,quaternion.identity);
                n.name = index.ToString();
                createdPieceList.Add(n);
                Image img = n.GetComponent<Image>();
                img.sprite = newSprite;
                Button nb = n.GetComponent<Button>();
                nb.onClick.AddListener(delegate { PuzzlePiecePress(n); });
                pieceButtons.Add(nb);
                //sr = n.AddComponent<SpriteRenderer>();
                //sr.sprite = newSprite;
                n.transform.position = new Vector3(i* padding, j* padding , 0);
                n.transform.parent = rawImage.transform;
                n.transform.localScale = new Vector3(1, 1, 1);
                // rotate
                //RectTransform nr = n.GetComponent<RectTransform>();
                //Quaternion initialRot = nr.rotation;
                //nr.rotation = initialRot * Quaternion.Euler(0, 0, rotateTiles);
                //rotate end
                index++;
            }
        }
        puzzleLayoutGroup.cellSize = new Vector2(resizedTextureInt,resizedTextureInt);
        RandomizePieces();
    }

    private void PuzzlePiecePress(GameObject index)
    {
        if (selectedPieceList.Count > 0 && ReferenceEquals(selectedPieceList[0], index))
        {
            selectedPieceList.Clear();
            return;
        }
        selectedPieceList.Add(index);
        if (selectedPieceList.Count > 1)
            StartCoroutine(PiecesMove());
    }

    IEnumerator PiecesMove()
    {
        TogglePiecesInteractability(false);
        puzzleLayoutGroup.enabled = false;
        Transform child0Transform = rawImage.transform.GetChild(selectedPieceList[0].transform.GetSiblingIndex());
        Vector3 saveChild0Pos = child0Transform.position;
        Transform child1Transform = rawImage.transform.GetChild(selectedPieceList[1].transform.GetSiblingIndex());
        Vector3 saveChild1Pos = child1Transform.position;
        var waitTime = 1;
        float elapsedTime = 0;
        while (elapsedTime < waitTime)
        {
            child0Transform.position = Vector3.Lerp(child0Transform.position, saveChild1Pos, elapsedTime / waitTime);
            child1Transform.position = Vector3.Lerp(child1Transform.position, saveChild0Pos, elapsedTime / waitTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        int i = selectedPieceList[0].transform.GetSiblingIndex();
        child0Transform.SetSiblingIndex(selectedPieceList[1].transform.GetSiblingIndex());
        child1Transform.SetSiblingIndex(i);
        puzzleLayoutGroup.enabled = true;
        if (CheckWin())
        {
            completedText.SetText("Completed");
            foreach (var button in pieceButtons)
            {
                ColorBlock cb = button.colors;
                cb.disabledColor = new Color(1f, 1f, 1f, 1f);
                button.colors = cb;
            }
        }
        else
        {
            completedText.SetText("");
            TogglePiecesInteractability(true);
        }
        selectedPieceList.Clear();
        st = 0;
    }

    private void TogglePiecesInteractability(bool status)
    {
        foreach (var button in pieceButtons)
            button.interactable = status;
    }
    
    Texture2D Resize(Texture2D texture2D,int targetX,int targetY)
    {
        RenderTexture rt=new RenderTexture(targetX, targetY,24);
        RenderTexture.active = rt;
        Graphics.Blit(texture2D,rt);
        Texture2D result=new Texture2D(targetX,targetY);
        result.ReadPixels(new Rect(0,0,targetX,targetY),0,0);
        result.Apply();
        return result;
    }

    public void SetPuzzlePieces(float amount)
    {
        pieces = (int)amount;
    }

    public void DestroyChildren()
    {
        foreach (Transform child in rawImage.transform) {
            Destroy(child.gameObject);
        }
        selectedPieceList.Clear();
        createdPieceList.Clear();
        pieceButtons.Clear();
        completedText.SetText("");
    }

    private void RandomizePieces()
    {
        for (int i = 0; i < imageTiles.Count; i++)
        {
            int r = UnityEngine.Random.Range(0, imageTiles.Count);
            rawImage.transform.GetChild(i).SetSiblingIndex(r);
            //Debug.Log("Do ReRandomize bc " + r + " = To " + rawImage.transform.GetChild(i).name);
            // Randomize while everything is in incorrect order
        }
    }

    private bool CheckWin()
    {
        for (int i = 0; i < rawImage.transform.childCount; i++)
        {
            if (!rawImage.transform.GetChild(i).name.Contains(i.ToString()))
                return false;
        }
        return true;
    }

    void Start()
    {
        
    }
    
    void Update()
    {
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                touchStartPos = touch.position;
                break;
            case TouchPhase.Ended:
                touchEndPos = touch.position;
                if (Vector2.Distance(touchStartPos,touchEndPos) > resizedTextureInt / 2)
                    StartCoroutine(GuestureLogic());
                break;
        }
    }

    IEnumerator GuestureLogic()
    {
        yield return new WaitForSeconds(0.3f);
        if (st < 2)
        {
            foreach (var button in pieceButtons)
            {
                if (Vector2.Distance(button.transform.position, touchStartPos) < resizedTextureInt / 2 || Vector2.Distance(button.transform.position, touchEndPos) < resizedTextureInt / 2)
                {
                    button.onClick.Invoke();
                    st++;
                }
            }
        }
    }

}
