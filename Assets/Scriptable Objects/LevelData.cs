
//using System.Collections;
//using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class LevelData : ScriptableObject
{
    public GameObject TilemapObject;
    public LevelData NextLevel;

    public AudioClip Music;
    public float MusicVolume;

    public List<Vector2> _janitorSpawnLocations;
    public List<Vector2> _enemySpawnLocations;

    public Vector2 _cameraStartPosition;

    //Matching with enemy spawn locations by index

        public List<UnitType> _enemyTypes;
        public List<int> _enemySpawnLevels;

    //Matching with each other by index

    [TextArea] public string[] levelTextSpeaker;
    [TextArea] public string[] levelText;
}
