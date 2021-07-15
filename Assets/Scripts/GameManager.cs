using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] private GameObject TakePhotoCanvas;
    [SerializeField] private GameObject PuzzleUICanvas;

    private void Awake()
    {
        instance = this;
    }

    public void ToggleGameObjects(GameObject obj, bool status)
    {
        obj.SetActive(status);
    }

    public void GoToCamera()
    {
        PuzzleManager.instance.DestroyChildren();
        ToggleGameObjects(TakePhotoCanvas, true);
        ToggleGameObjects(PuzzleUICanvas, false);
    }

    void Start()
    {
        
    }


    void Update()
    {
        
    }
}
