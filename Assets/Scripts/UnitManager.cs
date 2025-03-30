using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.UI.CanvasScaler;


public class UnitManager : MonoBehaviour
{
    #warning Check for extraneous variables

    public static event Action<GameObject, GameObject> DisplayUnitUIEvent;
    public static event Action<GameObject, GameObject> DisplayBattleUIEvent;
    public static event Action PauseGameEvent;
    public static event Action GameOverEvent;
    public static event Action<LevelData> DisplayLevelDialogueEvent;
    public static event Action<Vector2> SetCameraPosition;
    public static event Action<LevelData> PlayMusic;
    public static event Action<LevelData> SaveGame;
    public static event Action<string> LoadGame;

    private event Action UpdateActions;

    [SerializeField] private LevelData _startLevel;
    [SerializeField] private GameObject _gridObject;
    [SerializeField] private List<TileData> _tileData;

    //obviously there's got to be a better way to do this, but I'm too lazy to figure out IOstream right now
    [SerializeField] private GameObject _mopJanitorPrefab, _broomJanitorPrefab, _vacuumJanitorPrefab, _sprayBottleJanitorPrefab, _paperCranePrefab, _lunchSlimePrefab, _dirtBallPrefab, _greenSlushiePrefab, _redSlushiePrefab;

    [SerializeField] private GameObject _cursorPrefab;
    [SerializeField] private GameObject _whiteSquarePrefab;


    //Movement Arrow Prefabs
    [SerializeField] private GameObject _arrowBasePrefab;
    [SerializeField] private GameObject _arrowBodyPrefab;
    [SerializeField] private GameObject _arrowTurnPrefab;
    [SerializeField] private GameObject _arrowHeadPrefab;

    [SerializeField] private GameObject _miniHealthBarGreen;

    [SerializeField] private GameObject _fadeOutOverlayObject;

    private Tilemap _tilemap;
    private LevelData _currentLevel;
    private GameObject _currentLevelTilemapObject;

    private Dictionary<TileBase, TileData> _dataFromTiles;

    private int _janitorSpawnAmount, _enemySpawnAmount;
    private List<GameObject> _janitors, _enemies, _allUnits, _arrowPositions;

    private List<GameObject> _highlightedSquares, _highlightedEnemySquares;

    

    private Vector3Int _mouseGridPosition;
    private Vector3Int _oldMouseGridPosition;

    private Vector3Int _lastClickedPosition;
    private GameObject _lastClickedUnit;

    private GameObject _selectedUnit;
    private Unit _selectedUnitUnit;
    private GameObject _oldSelectedUnit;

    private bool _highlightedEnemySquaresDisplayed;

    private bool _enemySquareToggleLock;
    private List<GameObject> _toggleLockedEnemies;

    private bool _selectedUnitHasMoved;
    private bool _selectedUnitReadyToFight;
    private bool _battleOver;
    private bool _counterKill;

    /*
    private GameObject _selectedEnemy;
    private GameObject _movingEnemy;
    private Unit _movingEnemyUnit;
    */

    private GameObject _hoveredUnit;
    private GameObject _oldHoveredUnit;
    
    private bool _janitorsTurn;
    
    private GameObject _selectedUnitCopy;
    private GameObject _cursor;

    private UnitManagerUtils _utils;

    private int _timeSinceLastEnemyMove;
    private int _timeSinceEnemySelected;

    private Coroutine _runningMoveCoroutine;
    private Coroutine _runningOnUpdateEnemiesTurn = null;

    private SFXManager _SFXManager;

    private bool _gameOverScreenUp = false;
    private bool _moveOver = false;
    private bool _currentlyBattling = false;

    private bool _dialogueDone = false;
    [SerializeField] private GameObject _dialogueBox;

    [SerializeField] private List<LevelData> _levelsList;

    [SerializeField] private GameObject _cleaningIndicator;

    private void Update()
    {
        UpdateActions?.Invoke();
    }
    private void OnUpdateGeneral()
    {
       
    }
    private void OnUpdateJanitorsTurn()
    {
        _mouseGridPosition = _utils.MousePositionToGridPosition();
        _hoveredUnit = _utils.GetOccupantAtPosition(_allUnits, _mouseGridPosition);

        if (_mouseGridPosition != _oldMouseGridPosition) RenderCursor();

        if (_selectedUnitCopy != null) SetObjectPosition(_selectedUnitCopy, _mouseGridPosition);

        if (_selectedUnit == null)
        {
            HoverUnit(_hoveredUnit);
        }
        else
        {
            PathArrowLogic();
        }

        if (_oldHoveredUnit != _hoveredUnit) 
        {

            if (_selectedUnitReadyToFight) DisplayUnitUIEvent?.Invoke(_selectedUnit, _hoveredUnit);
            else DisplayUnitUIEvent?.Invoke(null, _hoveredUnit);

        }

        _oldHoveredUnit = _hoveredUnit;
        _oldMouseGridPosition = _mouseGridPosition;
    }
    IEnumerator BattleTheseUnits(GameObject janitor, GameObject enemy)
    {

        foreach (GameObject unit in _allUnits)
        {
            unit.GetComponent<Unit>().HideMiniHealthBar();
        }

        enabled = false;

        DisplayUnitUIEvent?.Invoke(null, null);

        yield return null;

        DisplayBattleUIEvent?.Invoke(janitor, enemy);
        
        yield return new WaitUntil(() => _battleOver);

        _battleOver = false;

        enabled = true;

        foreach (GameObject unit in _allUnits)
        {
            unit.GetComponent<Unit>().ShowMiniHealthBar();
        }

    }
    private void BattleComplete()
    {
        _battleOver = true;
    }

    private void OnRightClick()
    {
        PlayAudioWithVolumeAndPitch("MenuClick", 1f, 0.7f);
        if (_janitorsTurn && _selectedUnit != null) Deselect();
    }
    private void OnClick()
    {
        OnClickMainLogic();
        RenderCursor();

        if (_runningMoveCoroutine == null)
        {
            if (_janitors.All(u => u.GetComponent<Unit>().IsWaiting()) && _janitorsTurn) EnemiesTurnStart();
        }
    }
    private void OnClickMainLogic()
    {
        _lastClickedPosition = _mouseGridPosition;
        GameObject clickedUnit = _utils.GetOccupantAtPosition(_allUnits, _lastClickedPosition);

        //if a gameobject has been clicked
        if (clickedUnit != null)
        {
            Unit clickedUnitUnit = clickedUnit.GetComponent<Unit>();

            clickedUnitUnit.UpdateMiniHealthBar();

            if (clickedUnitUnit.IsWaiting()) return;
            
            if (_selectedUnit != null)
            {
                //If clicked unit is selected unit
                if (_selectedUnit == clickedUnit)
                {
                    //If selected unit has already moved and that unit is clicked again
                    if (_selectedUnitHasMoved)
                    {
                        WaitUnit(_selectedUnit);
                    }
                    //If selected unit has not already moved and that unit is clicked again
                    else
                    {
                        PlayAudioWithVolumeAndPitch("MenuClick", 1f, 0.7f);
                        Deselect();
                    }
                }

                //if clicked unit is not selected unit
                else
                {


                    //if clicked unit isn't within attack distance without needing to walk from selected unit
                    if (!(_utils.IsUnitInAttackRange(_selectedUnit, clickedUnit) && clickedUnitUnit.GetTeam() == Team.Enemy)) return;

                    //if this clicked unit has already been clicked once
                    if (_selectedUnitReadyToFight && _lastClickedUnit != null)
                    {
                        if (_lastClickedUnit == clickedUnit)
                        {

                            PlayAudioWithVolumeAndPitch("Shwing", 1f, 1f);
                            StartCoroutine(AttackUnit(_selectedUnit, clickedUnit));
                            WaitUnit(_selectedUnit);

                        }

                    }

                    //if this clicked unit has not already been clicked once
                    else
                    {
                        PlayAudioWithVolumeAndPitch("Sheathe", 1f, 1f);
                        _selectedUnitReadyToFight = true;
                        _oldHoveredUnit = null;
                    }

                }

            }
            //if selected unit is null and clicked unit is a janitor
            else if (clickedUnitUnit.GetTeam() == Team.Janitor)
            {
                SelectUnit(_utils.GetOccupantAtPosition(_janitors, _mouseGridPosition));

                _oldSelectedUnit = _selectedUnit;
            }
            else 
            {
                /*
                if (_toggleLockedEnemies.Contains(clickedUnit))
                {
                    _toggleLockedEnemies.Remove(clickedUnit);
                }
                else
                {
                    _toggleLockedEnemies.Add(clickedUnit);
                }

                RemoveExistantEnemySquareHighlights();
                HighlightDangerArea(_toggleLockedEnemies);
                */
            }

        }
        //if a non-gameobject has been clicked
        else
        {
            //if there isn't a selected unit when no clicked unit
            if (_selectedUnit == null) return;
            
            if (_mouseGridPosition == _selectedUnitUnit.GetTargetPosition())
            {
                WaitUnit(_selectedUnit);
                return;
            }
                  
            //if click is within walking distance of selected unit
            if (_selectedUnitUnit.GetMoveableTiles().Contains(_mouseGridPosition))
            {
                if (_selectedUnitUnit.IsInPosition() || _mouseGridPosition != _selectedUnitUnit.GetPosition())
                {
                    MoveSelectedUnitToClickedPosition();
                }
            }

            //if click is outside of both walking and attacking distance
            else if (!_selectedUnitUnit.GetAttackableOnlySquares().Contains(_mouseGridPosition))
            {
                PlayAudioWithVolumeAndPitch("MenuClick", 1f, 0.7f);
                Deselect();
            }
            
        }

        _lastClickedUnit = clickedUnit;
    }
    private void OnLeftShiftDown()
    {
        if (!_enemySquareToggleLock)
        {
            HighlightDangerArea( (_enemies.Except(_toggleLockedEnemies)).ToList<GameObject>() );
            _highlightedEnemySquaresDisplayed = true;

        }
    }
    private void OnLeftShiftUp()
    { 
        if (!_enemySquareToggleLock)
        {
            RemoveExistantEnemySquareHighlights();
            HighlightDangerArea(_toggleLockedEnemies);

            _highlightedEnemySquaresDisplayed = false;
        }
    }
    private void OnRightShift()
    {
        if (!_enemySquareToggleLock)
        {
            HighlightDangerArea(_enemies.Except(_toggleLockedEnemies).ToList());
            _enemySquareToggleLock = true;
            _highlightedEnemySquaresDisplayed = true;
        }
        else
        {
            RemoveExistantEnemySquareHighlights();
            HighlightDangerArea(_toggleLockedEnemies);
            _enemySquareToggleLock = false;
            _highlightedEnemySquaresDisplayed = false;
        }
    }
    private IEnumerator AttackUnit(GameObject attacker, GameObject defender)
    {
        if (_cleaningIndicator.activeSelf) _cleaningIndicator.SetActive(false);

        _currentlyBattling = true;

        Debug.Log("BattleUnits start");
        yield return StartCoroutine(BattleTheseUnits(attacker, defender));
        Debug.Log("BattleUnits end");

        AttackResults attackResults = _utils.Attack(attacker, defender);

        Unit attackingUnit = attacker.GetComponent<Unit>();
        Unit defendingUnit = defender.GetComponent<Unit>();

        int bonusExp;

        Unit janUnit;
        
        if (attackingUnit.GetTeam() == Team.Janitor) 
        {
            janUnit = attackingUnit;
            bonusExp = (int) (defendingUnit.GetBaseExp() * ((float) defendingUnit.GetLvl() / attackingUnit.GetLvl()));
        }
        else
        {
            janUnit = defendingUnit;
            bonusExp = (int)(attackingUnit.GetBaseExp() * ((float) attackingUnit.GetLvl() / defendingUnit.GetLvl()));
        }

        switch (attackResults)
        {    
            case AttackResults.Kill:

                StartCoroutine(DestroyUnit(defender));
                attacker.GetComponent<Unit>().UpdateMiniHealthBar();

                break;
            case AttackResults.CounterKill:

                _counterKill = true;
                defender.GetComponent<Unit>().UpdateMiniHealthBar();

                break;
            case AttackResults.NoKill:

                bonusExp = 0;

                attacker.GetComponent<Unit>().UpdateMiniHealthBar();
                defender.GetComponent<Unit>().UpdateMiniHealthBar();

                break;
        }

        janUnit.GetComponent<Unit>().AddEXPAndLevelUpIfNecessary(10 + bonusExp);

        if (_counterKill)
        {
            StartCoroutine(DestroyUnit(attacker));
            _counterKill = false;
        }

        _currentlyBattling = false;

        if (_selectedUnit != null) Deselect();
    }
    private void MoveSelectedUnitToClickedPosition()
    {
        if (_runningMoveCoroutine != null)
        {
            StopCoroutine(_runningMoveCoroutine);
            _runningMoveCoroutine = null;
        }

        SetObjectPosition(_selectedUnit, _selectedUnitUnit.GetOriginalPosition());

        _selectedUnitHasMoved = false;
        ClearPathArrow();
        RenderPathArrow(_selectedUnitUnit.GetOriginalPosition(), _mouseGridPosition, _selectedUnitUnit.GetMoveableTiles());

        SetUnitPath(_selectedUnit, _mouseGridPosition);

        _selectedUnitUnit.SetPosition(_selectedUnitUnit.GetOriginalPosition());

        if (_selectedUnitCopy != null) Destroy(_selectedUnitCopy); _selectedUnitCopy = null;

        _selectedUnitHasMoved = true;
        _selectedUnitReadyToFight = false;

        _runningMoveCoroutine = StartCoroutine(MoveUnitAndAttackIfNecessary(_selectedUnit));
    }
    private void HoverUnit(GameObject unit)
    {
        if (unit == null)
        {
            RemoveExistantSquareHighlights();
        }
        else if (unit != _oldHoveredUnit && !unit.GetComponent<Unit>().IsWaiting())
        {
            RemoveExistantSquareHighlights();
            HighlightMoveableSquares(unit, 0.3f);
        }

    }
    private void SpawnUnits(LevelData levelData)
    {
        foreach (GameObject janObj in _janitors)
        {
            GameManager.Instance.UpdateJanitorInDictionary(janObj.GetComponent<Janitor>());
            Destroy(janObj);
        } 
        _janitors.Clear();
        foreach (GameObject enemy in _enemies) Destroy(enemy);
        _enemies.Clear();
        _allUnits.Clear();

        Dictionary<UnitType, bool> janitorTypeHasSpawned = new();

#warning change accordingly
        janitorTypeHasSpawned.Add(UnitType.MopJanitor, false);
        janitorTypeHasSpawned.Add(UnitType.BroomJanitor, false);
        janitorTypeHasSpawned.Add(UnitType.VacuumJanitor, false);
        janitorTypeHasSpawned.Add(UnitType.SprayBottleJanitor, false);

        List<Vector2> adjustedJanitorSpawnLocations = new List<Vector2>();
        List<Vector2> adjustedEnemySpawnLocations = new List<Vector2>();

        foreach (Vector2 location in levelData._janitorSpawnLocations)
        {
            adjustedJanitorSpawnLocations.Add(new Vector2(location.x - 8.5f, location.y - 4.5f));
        }
        foreach (Vector2 location in levelData._enemySpawnLocations)
        {
            adjustedEnemySpawnLocations.Add(new Vector2(location.x - 8.5f, location.y - 4.5f));
        }

        for (int i = 0; i < adjustedJanitorSpawnLocations.Count; i++) 
        { 
            Vector2 coord = adjustedJanitorSpawnLocations[i];

#nullable enable
            UnitType? nullableSpawnedJanitorType = null;

            foreach (KeyValuePair<UnitType, bool> typeBoolPair in janitorTypeHasSpawned)
            {
                if (typeBoolPair.Value == false)
                {
                    nullableSpawnedJanitorType = typeBoolPair.Key;
                    break;
                }
            }
            
            if (nullableSpawnedJanitorType == null) continue;

            janitorTypeHasSpawned[(UnitType) nullableSpawnedJanitorType] = true;

#nullable disable

            UnitType spawnedJanitorType = (UnitType) nullableSpawnedJanitorType;

            GameObject janitorPrefab = null;

            switch (spawnedJanitorType)
            {
                case UnitType.MopJanitor: janitorPrefab = _mopJanitorPrefab;  break;
                case UnitType.BroomJanitor: janitorPrefab = _broomJanitorPrefab; break;
                case UnitType.VacuumJanitor: janitorPrefab = _vacuumJanitorPrefab; break;
                case UnitType.SprayBottleJanitor: janitorPrefab = _sprayBottleJanitorPrefab; break;
                default: break;
            }


            GameObject spawnedJanitor = Instantiate(janitorPrefab, coord, Quaternion.identity);

            spawnedJanitor.name = $"Janitor {spawnedJanitorType}";

            spawnedJanitor.GetComponent<Unit>().SetStatsBasedOnUnit(GameManager.Instance.GetJanitorTypeJanitorUnitPairs()[spawnedJanitorType]);

            spawnedJanitor.GetComponent<Unit>().SetMiniHealthBarGreen(Instantiate(_miniHealthBarGreen, new(coord.x, coord.y - 0.3f, 0f), Quaternion.identity), spawnedJanitor);
            spawnedJanitor.GetComponent<Unit>().UpdateMiniHealthBar();

            _janitors.Add(spawnedJanitor);
            _allUnits.Add(spawnedJanitor);
            _janitorSpawnAmount++;
        }

        for (int i = 0; i < adjustedEnemySpawnLocations.Count; i++)
        {
            Vector2 coord = adjustedEnemySpawnLocations[i];

            UnitType spawnedEnemyType = levelData._enemyTypes[i];
            GameObject enemyPrefab = null;

            switch (spawnedEnemyType)
            {
                case UnitType.LunchSlime: enemyPrefab = _lunchSlimePrefab; break;
                case UnitType.PaperCrane: enemyPrefab = _paperCranePrefab; break;
                case UnitType.GreenSlushie: enemyPrefab = _greenSlushiePrefab; break;
                case UnitType.RedSlushie: enemyPrefab = _redSlushiePrefab; break;
                case UnitType.DirtBall: enemyPrefab = _dirtBallPrefab; break;
                default: break;
            }

            GameObject spawnedEnemy = Instantiate(enemyPrefab, coord, Quaternion.identity);
            spawnedEnemy.name = $"{levelData._enemyTypes[i]}";

            spawnedEnemy.GetComponent<Unit>().SimLevelUp(_currentLevel._enemySpawnLevels[i]);

            spawnedEnemy.GetComponent<Unit>().SetMiniHealthBarGreen(Instantiate(_miniHealthBarGreen, new(coord.x, coord.y - 0.3f), Quaternion.identity), spawnedEnemy);
            spawnedEnemy.GetComponent<Unit>().UpdateMiniHealthBar();

            _enemies.Add(spawnedEnemy);
            _allUnits.Add(spawnedEnemy);
            _enemySpawnAmount++;
        }

    }
    private void PathArrowLogic()
    {
        
        if (_oldMouseGridPosition != _mouseGridPosition && !_selectedUnitHasMoved)
        {
            ClearPathArrow();

            if (_selectedUnitUnit.GetMoveableTiles() != null && _selectedUnitUnit.GetOriginalPosition() != null)
            {
                if (_selectedUnitUnit.GetMoveableTiles().Contains(_mouseGridPosition))
                {
                    RenderPathArrow(_selectedUnitUnit.GetOriginalPosition(), _mouseGridPosition, _selectedUnitUnit.GetMoveableTiles());
                }
            }
        }
    }
    private void RenderCursor()
    {
        if (_tilemap.GetTile(_mouseGridPosition) == null)
        {
            if (!_selectedUnitHasMoved) _cursor.SetActive(false);
            return;
        }
        else
        {
            _cursor.SetActive(true);
        }

        if (!_dataFromTiles[_tilemap.GetTile(_mouseGridPosition)].walkable || (_hoveredUnit != null && _hoveredUnit != _selectedUnit && _selectedUnit != null)) _cursor.GetComponent<SpriteRenderer>().color = Color.red;
        else _cursor.GetComponent<SpriteRenderer>().color = Color.blue;

        if (_selectedUnit != null) {
            if (_selectedUnitReadyToFight)
            {
                _cursor.GetComponent<SpriteRenderer>().color = Color.red;
                if (!_cleaningIndicator.activeSelf)
                {
                    _cleaningIndicator.SetActive(true);
                    SetObjectPosition(_cursor, _mouseGridPosition);
                }

            }
            else
            {
                _cleaningIndicator.SetActive(false);
                SetObjectPosition(_cursor, _utils.GetPosition(_selectedUnit));

                if (!_cleaningIndicator.activeSelf) SetObjectPosition(_cursor, _lastClickedPosition);
            }
        }


        if (!_cleaningIndicator.activeSelf)
        {
            if (_selectedUnit != null) SetObjectPosition(_cursor, _selectedUnit.GetComponent<Unit>().GetOriginalPosition());
            else SetObjectPosition(_cursor, _mouseGridPosition);
        }

    }
    private void RemoveExistantSquareHighlights()
    {
        foreach (GameObject square in _highlightedSquares) Destroy(square);
        _highlightedSquares.Clear();

    }
    private void RemoveExistantEnemySquareHighlights()
    {
        foreach (GameObject square in _highlightedEnemySquares) Destroy(square);
        _highlightedEnemySquares.Clear();
    }
    private void HighlightMoveableSquares(GameObject unit, float alpha)
    {
        (List<Vector3Int>, List<Vector3Int>) tiles;

        List<GameObject> opposingTeam;

        if (unit.GetComponent<Unit>().GetTeam() == Team.Janitor)
        {
            tiles = _utils.GetMoveableTiles(unit, _utils.GetPosition(unit), _janitors, _enemies);
            opposingTeam = _enemies;
        }
        else
        {
            tiles = _utils.GetMoveableTiles(unit, _utils.GetPosition(unit), _enemies, _janitors);
            opposingTeam = _janitors;
        }

        foreach (Vector3Int tile in tiles.Item1)
        {

            Vector3Int gridPos = _tilemap.WorldToCell(tile);
            GameObject focusedTile = Instantiate(_whiteSquarePrefab, new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0.0f), Quaternion.identity);

            focusedTile.GetComponent<SpriteRenderer>().color = new Color(0, 0, 1, alpha);


            _highlightedSquares.Add(focusedTile);

        }

        foreach (Vector3Int tile in tiles.Item2)
        {
            bool doRender = true;
            if (!_utils.GetTileMoveValidity(_selectedUnit, tile) && _utils.GetOccupantAtPosition(opposingTeam, tile) == null) { doRender = false; }

            Vector3Int gridPos = _tilemap.WorldToCell(tile);

            if (doRender)
            {
                GameObject focusedTile = Instantiate(_whiteSquarePrefab, new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0.0f), Quaternion.identity);

                focusedTile.GetComponent<SpriteRenderer>().color = new Color(1, 0, 0, Math.Clamp(alpha + 0.1f, 0f, 1f));

                _highlightedSquares.Add(focusedTile);
            }

        }
    }
    private void HighlightDangerArea(List<GameObject> targets)
    {
        if (targets.Count <= 0) return;

        (List<Vector3Int>, List<Vector3Int>) tiles = new();

        foreach (GameObject enemy in targets)
        {
            (List<Vector3Int>, List<Vector3Int>) tempTiles = _utils.GetMoveableTiles(enemy, _utils.GetPosition(enemy), _enemies, _janitors);

            if (tiles.Item1 != null) 
            {
            tiles.Item1.AddRange(tempTiles.Item1.Except(tiles.Item1));
            }
            else tiles.Item1 = tempTiles.Item1;

            if (tiles.Item2 != null)
            {
                tiles.Item2.AddRange(tempTiles.Item2.Except(tiles.Item2));
            }
            else tiles.Item2 = tempTiles.Item2;    
        }

        List<Vector3Int> finalTiles = tiles.Item1;
        finalTiles.AddRange(tiles.Item2);
        finalTiles = finalTiles.Distinct().ToList();

        foreach (Vector3Int tile in finalTiles)
        {
            bool doRender = true;
            if (!_utils.GetTileMoveValidityIgnoringUnits(_selectedUnit, tile, _allUnits)) { doRender = false; }

            Vector3Int gridPos = _tilemap.WorldToCell(tile);

            if (doRender)
            {
                GameObject focusedTile = Instantiate(_whiteSquarePrefab, new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0.0f), Quaternion.identity);
                focusedTile.GetComponent<SpriteRenderer>().sortingOrder += 1;
                focusedTile.GetComponent<SpriteRenderer>().color = new Color(1, 0, 0, 0.60f);

                _highlightedEnemySquares.Add(focusedTile);
            }

        }
    }
    private void SetUnitPath(GameObject unit, Vector3Int endPos)
    {
        PlayAudioWithVolumeAndPitch("MenuClick", 1f, 1f);

        Unit unitUnit = unit.GetComponent<Unit>();

        List<Vector3> path = _utils.GetPath(unitUnit.GetOriginalPosition(), endPos, unitUnit.GetMoveableTiles());

        unitUnit.SetInPosition(false);
        unitUnit.SetIsWaiting(false);
        unitUnit.SetPath(path);
        unitUnit.SetTargetPosition(endPos);
        unitUnit.SetCurrentPathIndex(0);
    }
    private void RenderPathArrow(Vector3Int startPos, Vector3Int endPos, List<Vector3Int> accessibleTiles)
    {
        List<Vector3> path = _utils.GetPath(startPos, endPos, accessibleTiles);

        if (path != null)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 previousPos = new Vector2(0, 500);
                Vector2 nextPos = new Vector2(0, 500);
                Vector2 currentPos = path[i];

                GameObject arrowPiece;

                if (i < path.Count - 1)
                {
                    nextPos = path[i + 1];
                }
                if (i > 0)
                {
                    previousPos = path[i - 1];
                }

                //i should probably change these to a switch statement somehow

                if (previousPos.x.Equals(currentPos.x) && currentPos.x.Equals(nextPos.x)) arrowPiece = Instantiate(_arrowBodyPrefab, currentPos, Quaternion.identity);//Vertical Body
                else if (previousPos.y.Equals(currentPos.y) && currentPos.y.Equals(nextPos.y)) arrowPiece = Instantiate(_arrowBodyPrefab, currentPos, Quaternion.Euler(0, 0, 90));//Horizontal Body
                else if (Vector2.Distance(nextPos, currentPos) > 2)
                {
                    if (currentPos.x < previousPos.x) arrowPiece = Instantiate(_arrowHeadPrefab, currentPos, Quaternion.Euler(0, 0, 90));//left head
                    else if (currentPos.x > previousPos.x) arrowPiece = Instantiate(_arrowHeadPrefab, currentPos, Quaternion.Euler(0, 0, -90));
                    else if (currentPos.y < previousPos.y) arrowPiece = Instantiate(_arrowHeadPrefab, currentPos, Quaternion.Euler(0, 0, 180));//down head
                    else arrowPiece = Instantiate(_arrowHeadPrefab, currentPos, Quaternion.Euler(0, 0, 0));//up head
                }
                else if (Vector2.Distance(previousPos, currentPos) > 2)
                {
                    if (currentPos.x < nextPos.x) arrowPiece = Instantiate(_arrowBasePrefab, currentPos, Quaternion.Euler(0, 0, -90)); //left base
                    else if (currentPos.x > nextPos.x) arrowPiece = Instantiate(_arrowBasePrefab, currentPos, Quaternion.Euler(0, 0, 90)); //right base
                    else if (currentPos.y < nextPos.y) arrowPiece = Instantiate(_arrowBasePrefab, currentPos, Quaternion.Euler(0, 0, 0)); //down base
                    else arrowPiece = Instantiate(_arrowBasePrefab, currentPos, Quaternion.Euler(0, 0, 180)); //up base
                }
                else if (previousPos.y < currentPos.y)
                {
                    if (currentPos.x < nextPos.x) arrowPiece = Instantiate(_arrowTurnPrefab, currentPos, Quaternion.Euler(0, 0, 0)); // up then left
                    else arrowPiece = Instantiate(_arrowTurnPrefab, currentPos, Quaternion.Euler(0, 0, -90));// up then right
                }
                else if (previousPos.y > currentPos.y)
                {
                    if (currentPos.x < nextPos.x) arrowPiece = Instantiate(_arrowTurnPrefab, currentPos, Quaternion.Euler(0, 0, 90));// down then left
                    else arrowPiece = Instantiate(_arrowTurnPrefab, currentPos, Quaternion.Euler(0, 0, 180)); // down then right
                }
                else if (previousPos.x < currentPos.x)
                {
                    if (currentPos.y < nextPos.y) arrowPiece = Instantiate(_arrowTurnPrefab, currentPos, Quaternion.Euler(0, 0, 180));// right then up
                    else arrowPiece = Instantiate(_arrowTurnPrefab, currentPos, Quaternion.Euler(0, 0, -90)); // right then down
                }
                else
                {
                    if (currentPos.y < nextPos.y) arrowPiece = Instantiate(_arrowTurnPrefab, currentPos, Quaternion.Euler(0, 0, 90));// left then up
                    else arrowPiece = Instantiate(_arrowTurnPrefab, currentPos, Quaternion.Euler(0, 0, 0));// left then down
                }

                _arrowPositions.Add(arrowPiece);

            }
        }
    }
    private void ClearPathArrow()
    {
        foreach (GameObject piece in _arrowPositions) Destroy(piece);
        _arrowPositions.Clear();

    }
    private void SelectUnit(GameObject unit)
    {
        PlayAudioWithVolumeAndPitch("MenuClick", 1f, 1f);

        _selectedUnit = unit;
        _selectedUnitUnit = unit.GetComponent<Unit>();
        _selectedUnitUnit.SetOriginalPosition(_utils.GetPosition(_selectedUnit));

        _selectedUnitUnit.SetMoveableAndAttackableTiles(_utils.GetMoveableTiles(_selectedUnit, _selectedUnitUnit.GetOriginalPosition(), _janitors, _enemies));

        _selectedUnitCopy = Instantiate(_selectedUnit);
        _selectedUnitCopy.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);

        RemoveExistantSquareHighlights();
        HighlightMoveableSquares(_selectedUnit, 0.7f);

        _selectedUnitReadyToFight = false;
        _selectedUnitHasMoved = false;

        _selectedUnit.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
    }
    private void GameOverDone()
    {
        _gameOverScreenUp = false;
    }
    private void DialogueDone()
    {
        _dialogueDone = true;
    }
    private IEnumerator NextLevel(bool restart)
    {
        DisplayUnitUIEvent?.Invoke(null, null);

        RemoveExistantEnemySquareHighlights();
        RemoveExistantSquareHighlights();
        DisplayUnitUIEvent?.Invoke(null, null);

        
        if (!restart)
        {
            if (_currentLevel.NextLevel == null)
            {
                StartCoroutine(FadeInThenLoadScene(_fadeOutOverlayObject, "EndScene"));
            }
            else
            {
                yield return FadeInObject(_fadeOutOverlayObject);
                enabled = false;

                LoadNextLevel();
                LoadCurrentLevelUnits();

                SetCameraPosition?.Invoke(_currentLevel._cameraStartPosition);

                yield return TextBoxStuff();

                yield return FadeOutObject(_fadeOutOverlayObject);
            }

        }
        else
        {
            GameOverEvent?.Invoke();
            _gameOverScreenUp = true;

            yield return new WaitForSecondsRealtime(1f);

            enabled = false;

            LoadCurrentLevelUnits();
            SetCameraPosition?.Invoke(_currentLevel._cameraStartPosition);

            enabled = true;

            yield return new WaitUntil(() => !_gameOverScreenUp);

            JanitorsTurnStart();
        }
    }

    private void LoadNextLevel()
    {
        Destroy(_currentLevelTilemapObject);
        _currentLevel = _currentLevel.NextLevel;
        _currentLevelTilemapObject = Instantiate(_currentLevel.TilemapObject);
        _currentLevelTilemapObject.transform.parent = _gridObject.transform;
        _tilemap = _currentLevelTilemapObject.GetComponent<Tilemap>();
        
        
    }

    private void LoadCurrentLevelUnits()
    {
        SpawnUnits(_currentLevel);
        _utils = new UnitManagerUtils(_tilemap, _tileData, _allUnits);

        foreach (GameObject enemy in _enemies) enemy.GetComponent<Unit>().SetPosition(_utils.GetPosition(enemy));
        foreach (GameObject janitor in _janitors) janitor.GetComponent<Unit>().SetPosition(_utils.GetPosition(janitor));
    }

    private void InitVariables()
    {
        _cleaningIndicator = Instantiate(_cleaningIndicator);
        _cleaningIndicator.SetActive(false);
        _currentLevel = _startLevel;

        _currentLevelTilemapObject = Instantiate(_currentLevel.TilemapObject);
        _currentLevelTilemapObject.transform.parent = _gridObject.transform;
        _tilemap = _currentLevelTilemapObject.GetComponent<Tilemap>();

        _lastClickedPosition = new();

        _dataFromTiles = new Dictionary<TileBase, TileData>();

        _janitorSpawnAmount = 0;
        _enemySpawnAmount = 0;

        _timeSinceLastEnemyMove = 0;

        _selectedUnit = null;
        _selectedUnitUnit = null;

        _highlightedEnemySquaresDisplayed = false;

        _janitorsTurn = true;
        UpdateActions += OnUpdateJanitorsTurn;
        UpdateActions += OnUpdateGeneral;


        UpdateActions += () => _timeSinceLastEnemyMove++;
        UpdateActions += () => _timeSinceEnemySelected++;



        if (_selectedUnitCopy != null) Destroy(_selectedUnitCopy); _selectedUnitCopy = null;

        _janitors = new List<GameObject>();
        _enemies = new List<GameObject>();
        _allUnits = new List<GameObject>();

        _highlightedSquares = new List<GameObject>();
        _highlightedEnemySquares = new List<GameObject>();
        _arrowPositions = new List<GameObject>();

        _cursor = Instantiate(_cursorPrefab);
        _cursor.GetComponent<SpriteRenderer>().color = Color.blue;

        _cleaningIndicator.transform.parent = _cursor.transform;

        _selectedUnitHasMoved = false;

        ClearPathArrow();

        foreach (var tileData in _tileData)
        {
            foreach (var tile in tileData.tiles)
            {
                _dataFromTiles.Add(tile, tileData);
            }
        }

        _toggleLockedEnemies = new();

    }
    private void WaitUnit(GameObject unit)
    {
        PlayAudioWithVolumeAndPitch("Click", 1f, 1f);

        if (_highlightedEnemySquaresDisplayed)
        {
            RemoveExistantEnemySquareHighlights();
            HighlightDangerArea(_enemies);
        }
            
        

        Unit unitUnit = unit.GetComponent<Unit>();

        unitUnit.SetIsWaiting(true);
        unitUnit.SetOriginalPosition(new());

        unit.GetComponent<SpriteRenderer>().color = Color.gray;

        if (unitUnit.GetTeam() == Team.Janitor)
        {
            _selectedUnitHasMoved = false;
            if (_selectedUnitCopy != null) Destroy(_selectedUnitCopy); _selectedUnitCopy = null;
            _selectedUnitReadyToFight = false;

            _selectedUnit = null;

            RemoveExistantSquareHighlights();
            ClearPathArrow();
        }

    }
    private void Deselect()
    {
        if (_cleaningIndicator.activeSelf) _cleaningIndicator.SetActive(false);
        _cursor.GetComponent<SpriteRenderer>().color = Color.blue;

        if (_selectedUnit != null)
        {
            if (_runningMoveCoroutine != null)
            {
                StopCoroutine(_runningMoveCoroutine);
                _runningMoveCoroutine = null;
            }

            SetObjectPosition(_selectedUnit, _selectedUnitUnit.GetOriginalPosition());
            _selectedUnitUnit.SetPosition(_selectedUnitUnit.GetOriginalPosition());
            _selectedUnitUnit.SetOriginalPosition(new());
            _selectedUnitUnit.SetTargetPosition(null);

            _selectedUnit.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
        }
       
        _selectedUnitHasMoved = false;
        _selectedUnitReadyToFight = false;

        if (_selectedUnitCopy != null) Destroy(_selectedUnitCopy); _selectedUnitCopy = null;

        _selectedUnit = null;
        _selectedUnitUnit = null;

        RemoveExistantSquareHighlights();
        ClearPathArrow();

        if (_highlightedEnemySquaresDisplayed)
        {
            RemoveExistantEnemySquareHighlights();
            HighlightDangerArea(_enemies);
        }
    }
    private void JanitorsTurnStart()
    {
        PlayAudioWithVolumeAndPitch("Shwing", 1f, 1f);

        if (_highlightedEnemySquaresDisplayed)
        {
            RemoveExistantEnemySquareHighlights();
            HighlightDangerArea(_enemies);
        }

        _janitorsTurn = true;

        UpdateActions -= OnUpdateJanitorsTurn;
        UpdateActions += OnUpdateJanitorsTurn;

        Deselect();

        foreach (GameObject janitor in _janitors)
        {
            janitor.GetComponent<Unit>().SetIsWaiting(false);
            janitor.GetComponent<Unit>().SetInPosition(true);

            janitor.GetComponent<SpriteRenderer>().color = Color.white;
        }

        foreach (GameObject enemy in _enemies)
        {
            enemy.GetComponent<SpriteRenderer>().color = Color.white;

            Unit enemyUnit = enemy.GetComponent<Unit>();

            enemyUnit.SetIsWaiting(false);
            enemyUnit.SetPath(null);

        }
    }
    private void EnemiesTurnStart()
    {
        _cursor.transform.position = new Vector2(1000f, 1000f);

        PlayAudioWithVolumeAndPitch("Shwing", 1f, 1f);

        DisplayUnitUIEvent?.Invoke(null, null);

        _janitorsTurn = false;

        UpdateActions -= OnUpdateJanitorsTurn;

        foreach (GameObject janitor in _janitors)
        {
            janitor.GetComponent<SpriteRenderer>().color = Color.white;
        }

        foreach (GameObject enemy in _enemies)
        {
            enemy.GetComponent<SpriteRenderer>().color = Color.white;

            Unit enemyUnit = enemy.GetComponent<Unit>();

            enemyUnit.SetIsWaiting(false);
            enemyUnit.SetOriginalPosition(_utils.GetPosition(enemy));
            enemyUnit.SetPath(null);
            enemyUnit.SetTargetPosition(null);
            enemyUnit.SetInPosition(false);
            enemyUnit.SetCurrentPathIndex(0);

        }

        StartCoroutine(EnemyTurn());

    }

    private IEnumerator EnemyTurn()
    {
        yield return new WaitUntil(() => enabled);


        for (int i = 0; i < _enemies.Count; i++)
        {
            yield return new WaitUntil(() => !_currentlyBattling);

            GameObject enemy = _enemies[i];

            Unit enemyUnit = enemy.GetComponent<Unit>();

            enemyUnit.SetMoveableAndAttackableTiles(_utils.GetMoveableTiles(enemy, enemyUnit.GetPosition(), _enemies, _janitors));

            Vector3Int targetSquare = _utils.FindBestMoveLocation(enemy, _utils.FindBestTarget(enemy));

            enemyUnit.SetTargetPosition(targetSquare);
            SetUnitPath(enemy, targetSquare);

            yield return new WaitForSecondsRealtime(1 - Mathf.Log10(enemyUnit.GetPath().Count + 1));

            yield return new WaitUntil(() => _moveOver);
            _moveOver = false;

            Debug.Log("MoveUnitAndAttackIfNecessary start");
            
            yield return MoveUnitAndAttackIfNecessary(enemy);

            Debug.Log("MoveUnitAndAttackIfNecessary end");

            if (_counterKill)
            {
                StartCoroutine(DestroyUnit(enemy));
                i--;
                _counterKill = false;
            }


        }

        JanitorsTurnStart();
    }

    private void Awake()
    {
        UIManager.BattleOverEvent += BattleComplete;

        GameManager.LoadLevel += LoadLevelData;
        SaveButton.SaveButtonPressed += SendSaveData;

        GameManager.PDownEvent += PDown;
        UnpauseButton.UnpauseClicked += PDown;

        InitVariables();

        if(NewGameButton.saveDataString != string.Empty)
        {
            Debug.Log("passed string empty test");
            LoadGame?.Invoke(NewGameButton.saveDataString);
        }

        SpawnUnits(_currentLevel);
        _utils = new UnitManagerUtils(_tilemap, _tileData, _allUnits);

        foreach (GameObject enemy in _enemies) enemy.GetComponent<Unit>().SetPosition(_utils.GetPosition(enemy));
        foreach (GameObject janitor in _janitors) janitor.GetComponent<Unit>().SetPosition(_utils.GetPosition(janitor));

        _SFXManager = this.GetComponent<SFXManager>();

        UIManager.GameOverDoneEvent += GameOverDone;
        DialogueManager.DialogueDone += DialogueDone;

        StartCoroutine(TextBoxStuff());
        
    }

    private IEnumerator TextBoxStuff()
    {
        enabled = false;

        PlayMusic?.Invoke(_currentLevel);

        _dialogueBox.SetActive(true);
        yield return FadeInUI(_dialogueBox);

        DisplayLevelDialogueEvent?.Invoke(_currentLevel);
        yield return new WaitUntil(() => _dialogueDone);
        _dialogueDone = false;

        yield return FadeOutUI(_dialogueBox);
        _dialogueBox.SetActive(false);

        enabled = true;

        JanitorsTurnStart();
    }

    private void OnEnable()
    {

        GameManager.LeftClickEvent += OnClick;
        GameManager.RightClickEvent += OnRightClick;

        GameManager.RightShiftDownEvent += OnRightShift;
        GameManager.LeftShiftDownEvent += OnLeftShiftDown;
        GameManager.LeftShiftUpEvent += OnLeftShiftUp;

        
    }

    private void SendSaveData()
    {
        foreach (GameObject janitor in _janitors)
        {
            Janitor janitorJanitor = janitor.GetComponent<Janitor>();
            GameManager.Instance.UpdateJanitorInDictionary(janitorJanitor);
        }

        Debug.Log("sending save data");
        
        SaveGame?.Invoke(_currentLevel);
    }

    private void LoadLevelData(string LevelName)
    {
        StartCoroutine(LoadingLevelData(LevelName));
    }
    private IEnumerator LoadingLevelData(string LevelName)
    {
        switch (LevelName) 
        {
            case ("0 - Tutorial Level"):
                _currentLevel = _levelsList[0];
                break;
            case ("1 - Cafeteria"):
                _currentLevel = _levelsList[1];
                break;
            case ("2 - MiddleHalls"):
                _currentLevel = _levelsList[2];
                break;
            case ("3 - Gym and Halls"):
                _currentLevel = _levelsList[3];
                break;
            default:
                _currentLevel = _levelsList[0];
                break;
        }

        Destroy(_currentLevelTilemapObject);
        
        yield return FadeInObject(_fadeOutOverlayObject);
        enabled = false;

        _currentLevelTilemapObject = Instantiate(_currentLevel.TilemapObject);
        _currentLevelTilemapObject.transform.parent = _gridObject.transform;
        _tilemap = _currentLevelTilemapObject.GetComponent<Tilemap>();

        LoadCurrentLevelUnits();

        SetCameraPosition?.Invoke(_currentLevel._cameraStartPosition);

        yield return TextBoxStuff();

        yield return FadeOutObject(_fadeOutOverlayObject);
    }

    private void OnDisable()
    {
        GameManager.LeftClickEvent -= OnClick;
        GameManager.RightClickEvent -= OnRightClick;

        GameManager.RightShiftDownEvent -= OnRightShift;
        GameManager.LeftShiftDownEvent -= OnLeftShiftDown;
        GameManager.LeftShiftUpEvent -= OnLeftShiftUp;

    }

    private void OnDestroy()
    {
        SaveButton.SaveButtonPressed -= SendSaveData;
        GameManager.LoadLevel -= LoadLevelData;

        UIManager.BattleOverEvent -= BattleComplete;
        GameManager.PDownEvent -= PDown;
        UnpauseButton.UnpauseClicked -= PDown;

        UIManager.GameOverDoneEvent -= GameOverDone;
        DialogueManager.DialogueDone -= DialogueDone;
    }

    private void PDown() 
    {
        if (enabled) enabled = false;
        else enabled = true;
    }

    private void SetObjectPosition(GameObject obj, Vector3Int gridPosition)
    {
        obj.transform.position = new Vector2(gridPosition.x + 0.5f, gridPosition.y + 0.5f);
    }
    IEnumerator DestroyUnit(GameObject unit)
    {

        if (unit.GetComponent<Unit>().GetTeam() == Team.Janitor)
        {
            GameManager.Instance.UpdateJanitorInDictionary(unit.GetComponent<Janitor>());
            _janitors.Remove(unit);
            
        }
        else _enemies.Remove(unit); 

        _allUnits.Remove(unit);
        _utils.RemoveUnit(unit);

        yield return StartCoroutine(FadeOutObject(unit));

        Destroy(unit);

        if (_enemies.Count == 0) StartCoroutine(NextLevel(false));
        else if (_janitors.Count == 0) StartCoroutine(NextLevel(true));
    }
    IEnumerator FadeOutObject(GameObject obj)
    {
        Color ogColor = obj.GetComponent<SpriteRenderer>().color;
        float r = ogColor.r;
        float g = ogColor.g;
        float b = ogColor.b;

        obj.GetComponent<SpriteRenderer>().color = new Color(r, g, b, 1f);

        for (float alpha = ogColor.a; alpha > 0f; alpha -= 0.05f)
        {
            obj.GetComponent<SpriteRenderer>().color = new Color(r, g, b, alpha);
            yield return null;
        }

        obj.GetComponent<SpriteRenderer>().color = new Color(r, g, b, 0f);

    }

    IEnumerator FadeInObject(GameObject obj)
    {
        Color ogColor = obj.GetComponent<SpriteRenderer>().color;
        float r = ogColor.r;
        float g = ogColor.g;
        float b = ogColor.b;

        obj.GetComponent<SpriteRenderer>().color = new Color(r, g, b, 0f);

        for (float alpha = ogColor.a; alpha < 1f; alpha += 0.05f)
        {
            obj.GetComponent<SpriteRenderer>().color = new Color(r, g, b, alpha);
            yield return null;
        }

        obj.GetComponent<SpriteRenderer>().color = new Color(r, g, b, 1f);

    }

    IEnumerator FadeOutUI(GameObject obj)
    {
        Color ogColor = obj.GetComponent<Image>().color;
        float r = ogColor.r;
        float g = ogColor.g;
        float b = ogColor.b;

        obj.GetComponent<Image>().color = new Color(r, g, b, 1f);

        for (float alpha = ogColor.a; alpha > 0; alpha -= 0.05f)
        {
            obj.GetComponent<Image>().color = new Color(r, g, b, alpha);
            yield return null;
        }

        obj.GetComponent<Image>().color = new Color(r, g, b, 0);

    }

    IEnumerator FadeInUI(GameObject obj)
    {
        Color ogColor = obj.GetComponent<Image>().color;
        float r = ogColor.r;
        float g = ogColor.g;
        float b = ogColor.b;

        obj.GetComponent<Image>().color = new Color(r, g, b, 0);

        for (float alpha = ogColor.a; alpha < 1f; alpha += 0.05f)
        {
            obj.GetComponent<Image>().color = new Color(r, g, b, alpha);
            yield return null;
        }

        obj.GetComponent<Image>().color = new Color(r, g, b, 1f);

    }

    private IEnumerator MoveUnitAndAttackIfNecessary(GameObject unit)
    {
        yield return new WaitUntil(() => !_currentlyBattling);

        Unit unitUnit = unit.GetComponent<Unit>();
        List<Vector3> path = unitUnit.GetPath();

        unit.GetComponent<SpriteRenderer>().sortingOrder = 50 - (int)unit.transform.position.y;

        while (!unitUnit.IsInPosition())
        {
            if (unitUnit.GetCurrentPathIndex() < path.Count - 1)
            {
                if (Vector2.Distance(unit.transform.position, path[unitUnit.GetCurrentPathIndex() + 1]) > 0.05)
                {
                    unit.transform.position = Vector2.MoveTowards(unit.transform.position, path[unitUnit.GetCurrentPathIndex() + 1], 0.6f);
                }

                else unitUnit.SetCurrentPathIndex(unitUnit.GetCurrentPathIndex() + 1);
            }
            else
            {
                unitUnit.SetInPosition(true);
                unitUnit.SetPath(null);
                unitUnit.SetPosition(_utils.GetPosition(unit));
                unit.GetComponent<SpriteRenderer>().sortingOrder = 500 - (int)unit.transform.position.y;
                unitUnit.SetCurrentPathIndex(0);

                SetObjectPosition(unit, (Vector3Int) unitUnit.GetTargetPosition());
            }

            yield return new WaitForSecondsRealtime(1 / 60f);
        }

        if (_highlightedEnemySquaresDisplayed || _toggleLockedEnemies.Count > 0)
        {
            RemoveExistantEnemySquareHighlights();
            HighlightDangerArea(_enemies);
        }

        if (unitUnit.GetTeam() == Team.Janitor)
        {
            if (_janitors.All(u => u.GetComponent<Unit>().IsWaiting())) EnemiesTurnStart();
        }
        else
        {
            WaitUnit(unit);

            if (unitUnit.GetAttackTarget() != null)
            {
                if (_utils.IsUnitInAttackRange(unit, unitUnit.GetAttackTarget()))
                {
                    GameManager.PDownEvent -= PDown;
                    Debug.Log("AttackUnit start");
                    yield return AttackUnit(unit, unitUnit.GetAttackTarget());
                    Debug.Log("AttackUnit end");
                    yield return null;
                    GameManager.PDownEvent += PDown;

                    unitUnit.SetAttackTarget(null);

                }
            }
        }

        _moveOver = true;
        _runningMoveCoroutine = null;
    }

    public void PlayAudioWithVolumeAndPitch(string soundName, float volume, float pitch)
    {
        _SFXManager.PlayAudioSetPitch(soundName, volume, pitch);
    }

    private IEnumerator FadeInThenLoadScene(GameObject targetObject, string sceneName)
    {
        SpriteRenderer renderer = targetObject.GetComponent<SpriteRenderer>();

        for (float i = 0; i < 1f; i += 0.05f)
        {
            renderer.color = new Color(0, 0, 0, i);

            yield return null;
        }

        renderer.color = new Color(0, 0, 0, 1f);

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

    }
}