using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using UnityEngine;



public class GameManager : MonoBehaviour
{
    public static Action LeftClickEvent;
    public static Action RightClickEvent;
    public static Action LeftShiftDownEvent;
    public static Action LeftShiftUpEvent;
    public static Action RightShiftDownEvent;
    public static Action PDownEvent;

    public static Action<string> LoadLevel;
    public static Action<string> DisplayJSON;

    [SerializeField] private GameObject _fadeOutRectangle;
    [SerializeField] private Camera _camera;

    private Vector3 _moveDirection = Vector3.zero;
    private float _moveDirectionMultiplier;

    public static GameManager Instance;

    private Dictionary<UnitType, Janitor> _janitorTypeJanitorUnitPairs = new();


    // this is a horrible way to code this but im lazy so that's what i'm gonna do

    private class SaveObject
    {
        public string CurrentLevelName;

        public int maxHp1;
        public int atk1;
        public int def1;
        public int spd1;
        public int exp1;
        public int lvl1;

        public int maxHp2;
        public int atk2;
        public int def2;
        public int spd2;
        public int exp2;
        public int lvl2;

        public int maxHp3;
        public int atk3;
        public int def3;
        public int spd3;
        public int exp3;
        public int lvl3;

        public int maxHp4;
        public int atk4;
        public int def4;
        public int spd4;
        public int exp4;
        public int lvl4;
    }

    private void Save(LevelData currentLevel)
    {

        SaveObject gameSave = new SaveObject
        {
            CurrentLevelName = currentLevel.name,

            maxHp1 = _janitorTypeJanitorUnitPairs[UnitType.MopJanitor].GetMaxHP(),
            atk1 = _janitorTypeJanitorUnitPairs[UnitType.MopJanitor].GetAtk(),
            def1 = _janitorTypeJanitorUnitPairs[UnitType.MopJanitor].GetDef(),
            spd1 = _janitorTypeJanitorUnitPairs[UnitType.MopJanitor].GetSpd(),
            exp1 = _janitorTypeJanitorUnitPairs[UnitType.MopJanitor].GetExp(),
            lvl1 = _janitorTypeJanitorUnitPairs[UnitType.MopJanitor].GetLvl(),

            maxHp2 = _janitorTypeJanitorUnitPairs[UnitType.BroomJanitor].GetMaxHP(),
            atk2 = _janitorTypeJanitorUnitPairs[UnitType.BroomJanitor].GetAtk(),
            def2 = _janitorTypeJanitorUnitPairs[UnitType.BroomJanitor].GetDef(),
            spd2 = _janitorTypeJanitorUnitPairs[UnitType.BroomJanitor].GetSpd(),
            exp2 = _janitorTypeJanitorUnitPairs[UnitType.BroomJanitor].GetExp(),
            lvl2 = _janitorTypeJanitorUnitPairs[UnitType.BroomJanitor].GetLvl(),

            maxHp3 = _janitorTypeJanitorUnitPairs[UnitType.VacuumJanitor].GetMaxHP(),
            atk3 = _janitorTypeJanitorUnitPairs[UnitType.VacuumJanitor].GetAtk(),
            def3 = _janitorTypeJanitorUnitPairs[UnitType.VacuumJanitor].GetDef(),
            spd3 = _janitorTypeJanitorUnitPairs[UnitType.VacuumJanitor].GetSpd(),
            exp3 = _janitorTypeJanitorUnitPairs[UnitType.VacuumJanitor].GetExp(),
            lvl3 = _janitorTypeJanitorUnitPairs[UnitType.VacuumJanitor].GetLvl(),

            maxHp4 = _janitorTypeJanitorUnitPairs[UnitType.SprayBottleJanitor].GetMaxHP(),
            atk4 = _janitorTypeJanitorUnitPairs[UnitType.SprayBottleJanitor].GetAtk(),
            def4 = _janitorTypeJanitorUnitPairs[UnitType.SprayBottleJanitor].GetDef(),
            spd4 = _janitorTypeJanitorUnitPairs[UnitType.SprayBottleJanitor].GetSpd(),
            exp4 = _janitorTypeJanitorUnitPairs[UnitType.SprayBottleJanitor].GetExp(),
            lvl4 = _janitorTypeJanitorUnitPairs[UnitType.SprayBottleJanitor].GetLvl(),
        };

        string saveJson = JsonUtility.ToJson(gameSave);
        DisplayJSON?.Invoke(saveJson);
    }

    private void Load(string saveJson)
    {
        SaveObject loadedSaveObject = JsonUtility.FromJson<SaveObject>(saveJson);

        _janitorTypeJanitorUnitPairs[UnitType.MopJanitor].SetStats(loadedSaveObject.maxHp1, loadedSaveObject.atk1, loadedSaveObject.def1, loadedSaveObject.spd1, loadedSaveObject.exp1, loadedSaveObject.lvl1, UnitType.MopJanitor);
        _janitorTypeJanitorUnitPairs[UnitType.BroomJanitor].SetStats(loadedSaveObject.maxHp2, loadedSaveObject.atk2, loadedSaveObject.def2, loadedSaveObject.spd2, loadedSaveObject.exp2, loadedSaveObject.lvl2, UnitType.BroomJanitor);
        _janitorTypeJanitorUnitPairs[UnitType.VacuumJanitor].SetStats(loadedSaveObject.maxHp3, loadedSaveObject.atk3, loadedSaveObject.def3, loadedSaveObject.spd3, loadedSaveObject.exp3, loadedSaveObject.lvl3, UnitType.VacuumJanitor);
        _janitorTypeJanitorUnitPairs[UnitType.SprayBottleJanitor].SetStats(loadedSaveObject.maxHp4, loadedSaveObject.atk4, loadedSaveObject.def4, loadedSaveObject.spd4, loadedSaveObject.exp4, loadedSaveObject.lvl4, UnitType.SprayBottleJanitor);

        Debug.Log(loadedSaveObject.CurrentLevelName + " first time");

        LoadLevel?.Invoke(loadedSaveObject.CurrentLevelName);
    }

    private void Awake()
    {
        UnitManager.SetCameraPosition += SetCameraPosition;

        UnitManager.SaveGame += Save;
        UnitManager.LoadGame += Load;

        Instance = this;
        Application.targetFrameRate = 60;
        Application.runInBackground = false;
        QualitySettings.vSyncCount = 1;

        _camera.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1);

#warning wip
        GameObject mopJanitorContainer = new();
        mopJanitorContainer.AddComponent<MopJanitor>();

        GameObject broomJanitorContainer = new();
        broomJanitorContainer.AddComponent<BroomJanitor>();

        GameObject vacuumJanitorContainer = new();
        vacuumJanitorContainer.AddComponent<VacuumJanitor>();

        GameObject sprayBottleJanitorContainer = new();
        sprayBottleJanitorContainer.AddComponent<SprayBottleJanitor>();


#warning wip
        _janitorTypeJanitorUnitPairs.Add(UnitType.MopJanitor, mopJanitorContainer.GetComponent<MopJanitor>());
        _janitorTypeJanitorUnitPairs.Add(UnitType.BroomJanitor, broomJanitorContainer.GetComponent<BroomJanitor>());
        _janitorTypeJanitorUnitPairs.Add(UnitType.VacuumJanitor, vacuumJanitorContainer.GetComponent<VacuumJanitor>());
        _janitorTypeJanitorUnitPairs.Add(UnitType.SprayBottleJanitor, sprayBottleJanitorContainer.GetComponent<SprayBottleJanitor>());

    }

    private void OnDestroy()
    {
        UnitManager.SetCameraPosition -= SetCameraPosition;

        UnitManager.SaveGame -= Save;
        UnitManager.LoadGame -= Load;
    }

    public Dictionary<UnitType, Janitor> GetJanitorTypeJanitorUnitPairs()
    {
        return _janitorTypeJanitorUnitPairs;
    }

    public void UpdateJanitorInDictionary(Janitor janitorUnit)
    {
        _janitorTypeJanitorUnitPairs[janitorUnit.GetUnitType()] = janitorUnit;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) LeftClickEvent?.Invoke();
        else if (Input.GetMouseButtonDown(1)) RightClickEvent?.Invoke();

        if (Input.GetKeyDown("right shift")) RightShiftDownEvent?.Invoke();
        else if (Input.GetKeyDown("left shift")) LeftShiftDownEvent?.Invoke();
        else if (Input.GetKeyUp("left shift")) LeftShiftUpEvent?.Invoke();

        if (Input.GetKeyDown("p")) PDownEvent?.Invoke();


        _moveDirection = Vector3.SmoothDamp((_moveDirection.normalized * _moveDirectionMultiplier / 20f * _camera.orthographicSize), Vector3.zero, ref _moveDirection, 0.3f);
        _moveDirectionMultiplier = Mathf.Clamp(_moveDirectionMultiplier - 0.1f, 0, 1);

        if (Input.GetKey("up") || Input.GetKey("w"))
        {
            _moveDirection += Vector3.up;
            _moveDirectionMultiplier = 1;
        }
        else if (Input.GetKey("down") || Input.GetKey("s"))
        {
            _moveDirection += Vector3.down;
            _moveDirectionMultiplier = 1;
        }

        if (Input.GetKey("left") || Input.GetKey("a"))
        {
            _moveDirection += Vector3.left;
            _moveDirectionMultiplier = 1;
        }
        else if (Input.GetKey("right") || Input.GetKey("d"))
        {
            _moveDirection += Vector3.right;
            _moveDirectionMultiplier = 1;
        }

        if (Input.GetKey("-"))
        {
            _camera.orthographicSize = Math.Clamp(_camera.orthographicSize + 0.1f, 4f, 16f);
        }
        else if (Input.GetKey("="))
        {
            _camera.orthographicSize = Math.Clamp(_camera.orthographicSize - 0.1f, 4f, 16f);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            _camera.orthographicSize = Math.Clamp(_camera.orthographicSize - Input.GetAxis("Mouse ScrollWheel") * 5f, 4f, 16f);
        }

        _camera.transform.position += _moveDirection.normalized * _moveDirectionMultiplier / 20f * _camera.orthographicSize;


    }

    private void Start()
    {
        _fadeOutRectangle.SetActive(true);
        StartCoroutine(FadeOut(_fadeOutRectangle));
    }

    private void SetCameraPosition(Vector2 position)
    {
        _camera.orthographicSize = 8;
        _camera.transform.position = new(position.x - 9, position.y - 5, -100);
    }

    private IEnumerator FadeOut(GameObject targetObject)
    {
        SpriteRenderer renderer = targetObject.GetComponent<SpriteRenderer>();

        for (float i = 1; i > 0; i -= 0.05f)
        {
            renderer.color = new Color(0, 0, 0, i);

            yield return null;
        }

        renderer.color = new Color(0, 0, 0, 0);
    }

}