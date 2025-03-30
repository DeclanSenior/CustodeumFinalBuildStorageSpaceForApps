using System;
using System.Collections.Generic;
using System.Linq;
//using System.Threading;
//using Unity.Collections;
using UnityEngine;
//using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
//using UnityEngine.WSA;
//using static UnityEngine.GraphicsBuffer;

public class PathTile
{
    private Vector3Int position;
    private int g, h, f;
    private PathTile parentTile;

    public PathTile(Vector3Int tilePosition, Vector3Int endPosition, PathTile parent)
    {
        position = tilePosition;
        h = Math.Abs(tilePosition.x - endPosition.x) + Math.Abs(tilePosition.y - endPosition.y);

        if (parent != null) 
        { 
            g = parent.GetG() + Math.Abs(parent.GetPathTilePosition().x - tilePosition.x) + Math.Abs(parent.GetPathTilePosition().y - tilePosition.y);
            parentTile = parent;
        }
        else g = 0;

        f = g + h;
    }

    public int GetG() { return g; }
    public int GetH() { return h; }
    public int GetF() { return f; }
    public Vector3Int GetPathTilePosition() { return position; }
    public PathTile GetParentTile() { return parentTile; }
    public void SetParentTile(PathTile parent) { parentTile = parent; }

}
public class UnitManagerUtils
{
    private Tilemap _tilemap;
    private List<TileData> _tileData;
    private Dictionary<TileBase, TileData> _dataFromTiles;

    private List<GameObject> _allUnits, _janitors, _enemies, _selectedEnemies;

    private List<Vector3Int> _highlightedSquares;

    private Vector3Int _mouseGridPosition;

    private bool _moved;

    public UnitManagerUtils(Tilemap tilemap, List<TileData> tileData, List<GameObject> allUnits)
    {
        _tilemap = tilemap;
        _tileData = tileData;
        _allUnits = allUnits;

        _janitors = new List<GameObject>();
        _enemies = new List<GameObject>();

        _dataFromTiles = new Dictionary<TileBase, TileData>();

        foreach (var tileDatum in _tileData)
        {
            foreach (var tile in tileDatum.tiles)
            {
                _dataFromTiles.Add(tile, tileDatum);
            }
        }

        foreach (var unit in _allUnits)
        {
            Team team = unit.GetComponent<Unit>().GetTeam();

            if (team == Team.Janitor) _janitors.Add(unit);
            else if (team == Team.Enemy) _enemies.Add(unit);
        }
    }

    public void UpdateInfo(Tilemap tilemap, List<TileData> tileData, List<GameObject> allUnits)
    {
        ClearAll();

        _tilemap = null;
        _tileData = null;
        _dataFromTiles.Clear();

        _tilemap = tilemap;
        _tileData = tileData;
        _allUnits = allUnits;

        foreach (var tileDatum in _tileData)
        {
            foreach (var tile in tileDatum.tiles)
            {
                _dataFromTiles.Add(tile, tileDatum);
            }
        }

        foreach (var unit in _allUnits)
        {
            Team team = unit.GetComponent<Unit>().GetTeam();

            if (team == Team.Janitor) _janitors.Add(unit);
            else if (team == Team.Enemy) _enemies.Add(unit);
        }

        Debug.Log(_enemies.Count());

    }
    public Vector3Int MousePositionToGridPosition()
    {
        Vector3Int gridPosition;
        Vector2 mousePosition;

        mousePosition = Input.mousePosition;

        gridPosition = _tilemap.WorldToCell(Camera.main.ScreenToWorldPoint(mousePosition));

        return gridPosition;
    }
    public GameObject GetOccupantAtPosition(List<GameObject> units, Vector3Int selectedPos)
    {
        foreach (GameObject obj in units)
        {
            Vector3Int objGridPos = GetPosition(obj);

            if (objGridPos == selectedPos) return obj;
        }

        return null;
    }
    public AttackResults Attack(GameObject attacker, GameObject attackee)
    {
        Unit attackerUnit = attacker.GetComponent<Unit>();
        Unit attackeeUnit = attackee.GetComponent<Unit>();

        attackerUnit.SetAttackTarget(null);

        return attackeeUnit.AttackedBy(attackerUnit, (GetDistance(GetPosition(attacker), GetPosition(attackee))));
    }
    public bool IsUnitInAttackRange(GameObject attacker, GameObject attackee)
    {
        if (GetDistance(GetPosition(attacker), GetPosition(attackee)) <= attacker.GetComponent<Unit>().GetRng()) return true;
        else return false;
    }
    public bool GetTileMoveValidity(GameObject selectedUnit, Vector3Int selectedPos)
    {
        if (_tilemap.GetTile(selectedPos) != null)
        {
            if (!_dataFromTiles[_tilemap.GetTile(selectedPos)].walkable) return false;
            else if (GetOccupantAtPosition(_allUnits, selectedPos) != null) return false;
            else return true;

        }
        else return false;
    }
    public bool GetTileMoveValidityIgnoringUnits(GameObject selectedUnit, Vector3Int selectedPos, List<GameObject> ignoredUnits)
    {
        if (_tilemap.GetTile(selectedPos) != null)
        {
            if (!_dataFromTiles[_tilemap.GetTile(selectedPos)].walkable) return false;
            else if (GetOccupantAtPosition(_allUnits.Except(ignoredUnits).ToList(), selectedPos) != null )
            {
                return false;
            }
            else return true;
        }
        else return false;
    }

    public void ClearAll()
    {
        _allUnits.Clear();
        _janitors.Clear();
        _enemies.Clear();
        _highlightedSquares?.Clear();
        _selectedEnemies?.Clear();

    }

    public void RemoveUnit(GameObject unit)
    {
        _allUnits.Remove(unit);

        Team team = unit.GetComponent<Unit>().GetTeam();

        if (team == Team.Janitor) _janitors.Remove(unit);
        else if (team == Team.Enemy) _enemies.Remove(unit);


    }
    
    public (List<Vector3Int>, List<Vector3Int>) GetMoveableTiles(GameObject unit, Vector3Int startPos, List<GameObject> teammates, List<GameObject> opposingUnits)
    {
        /* Steps:
         * 0. Mov = unit's move range
         * 1. For each adjacent square, check if moveable. If yes, add to returnedMoveableSquares and focusedSquares
         * 2. Lower Mov by one
         * 3. Repeat. If Mov is negative, add to returnedAttackableSquares
         * 4. Stop once Mov = attack range * -1
         */

        Unit thisUnit = unit.GetComponent<Unit>();
        int mov = thisUnit.GetMov();
        int rng = thisUnit.GetRng();


        List<Vector3Int>  moveableTiles = new List<Vector3Int>();
        List<Vector3Int>  attackableTiles = new List<Vector3Int>();
        List<Vector3Int>  focusedTiles = new List<Vector3Int>();


        focusedTiles.Add(startPos);
        moveableTiles.Add(startPos);

        for (int remainingMove = mov; remainingMove > rng * -1; remainingMove--)
        {
            List<Vector3Int> prospectiveTiles = new List<Vector3Int>();
            List<Vector3Int> prospectiveAttackableTiles = new List<Vector3Int>();

            foreach (Vector3Int tilePos in focusedTiles)
            {
                for (int mod = -1; mod < 2; mod += 2)
                {
                    Vector3Int testedTile = new(tilePos.x + mod, tilePos.y, 0);

                    if (!(moveableTiles.Contains(testedTile) || prospectiveTiles.Contains(testedTile)))
                    {
                        if (GetOccupantAtPosition(opposingUnits, testedTile) != null)
                        {
                            prospectiveAttackableTiles.Add(testedTile);
                        }

                        else if (GetTileMoveValidityIgnoringUnits(unit, testedTile, teammates))
                        {
                            if (attackableTiles.Contains(testedTile)) attackableTiles.Remove(testedTile);

                            if (!GetTileMoveValidity(unit, testedTile))
                            {
                                if (remainingMove > 0)
                                {
                                    for (int dist = 1; dist <= rng; dist++)
                                    {
                                        Vector3Int rangedTestedTile = new Vector3Int(tilePos.x + (dist * mod), tilePos.y, 0);

                                        if (_tilemap.GetTile(rangedTestedTile) != null && !(moveableTiles.Contains(rangedTestedTile) || prospectiveTiles.Contains(rangedTestedTile)))
                                        { prospectiveAttackableTiles.Add(rangedTestedTile); }
                                    }
                                }

                                if (attackableTiles.Contains(testedTile)) attackableTiles.Remove(testedTile);

                                if (remainingMove != 1) prospectiveTiles.Add(testedTile);
                            }

                            else prospectiveTiles.Add(testedTile);

                        }

                        else if (remainingMove > 0 && !GetTileMoveValidity(unit, testedTile))
                        {
                            for (int dist = 1; dist <= rng; dist++)
                            {
                                Vector3Int rangedTestedTile = new Vector3Int(tilePos.x + (dist * mod), tilePos.y, 0);

                                if (_tilemap.GetTile(rangedTestedTile) != null && !(moveableTiles.Contains(rangedTestedTile) || prospectiveTiles.Contains(rangedTestedTile)))
                                { prospectiveAttackableTiles.Add(rangedTestedTile); }
                            }
                        }

                    }

                    testedTile = new(tilePos.x, tilePos.y + mod, 0);

                    if (!(moveableTiles.Contains(testedTile) || prospectiveTiles.Contains(testedTile)))
                    {
                        if (GetOccupantAtPosition(opposingUnits, testedTile) != null)
                        {
                            prospectiveAttackableTiles.Add(testedTile);
                        }

                        else if (GetTileMoveValidityIgnoringUnits(unit, testedTile, teammates))
                        {
                            if (attackableTiles.Contains(testedTile)) attackableTiles.Remove(testedTile);

                            if (!GetTileMoveValidity(unit, testedTile))
                            {
                                if (remainingMove > 0) { 
                                    for (int dist = 1; dist <= rng; dist++)
                                    {
                                        Vector3Int rangedTestedTile = new Vector3Int(tilePos.x, tilePos.y + (dist * mod), 0);

                                        if (_tilemap.GetTile(rangedTestedTile) != null && !(moveableTiles.Contains(rangedTestedTile) || prospectiveTiles.Contains(rangedTestedTile)))
                                        { prospectiveAttackableTiles.Add(rangedTestedTile); }
                                    }
                                }

                                if (attackableTiles.Contains(testedTile)) attackableTiles.Remove(testedTile);

                                if (remainingMove != 1) prospectiveTiles.Add(testedTile);
                            }

                            else prospectiveTiles.Add(testedTile);

                        }

                        else if (remainingMove > 0 && !GetTileMoveValidity(unit, testedTile))
                        {
                            for (int dist = 1; dist <= rng; dist++)
                            {
                                Vector3Int rangedTestedTile = new Vector3Int(tilePos.x, tilePos.y + (dist * mod), 0);

                                if (_tilemap.GetTile(rangedTestedTile) != null && !(moveableTiles.Contains(rangedTestedTile) || prospectiveTiles.Contains(rangedTestedTile)))
                                { prospectiveAttackableTiles.Add(rangedTestedTile); }
                            }
                        }

                    }

                   
                }
            }


            focusedTiles = prospectiveTiles;

            if (remainingMove > 0) moveableTiles.AddRange(focusedTiles);
            else attackableTiles.AddRange(focusedTiles);

            attackableTiles.AddRange(prospectiveAttackableTiles);


        }

        attackableTiles = attackableTiles.Except(moveableTiles).ToList();

        return (moveableTiles.Distinct().ToList(), attackableTiles.Distinct().ToList());

    }
    public List<Vector3Int> GetTilesInRangeExcludingCenter(Vector3Int center, int range)
    {
        List<Vector3Int> tilesInRange = new List<Vector3Int>();
        List<Vector3Int> focusedTiles = new List<Vector3Int>();

        focusedTiles.Add(center);
        tilesInRange.Add(center);

        for (int remainingRng = range; remainingRng > 0; remainingRng--)
        {
            List<Vector3Int> prospectiveTiles = new List<Vector3Int>();

            foreach (Vector3Int tilePos in focusedTiles)
            {
                for (int mod = -1; mod < 2; mod += 2)
                {
                    Vector3Int testedTile = new();

                        
                    testedTile = new(tilePos.x + mod, tilePos.y, 0);

                    if (!(tilesInRange.Contains(testedTile) || prospectiveTiles.Contains(testedTile))) prospectiveTiles.Add(testedTile);

                    testedTile = new(tilePos.x, tilePos.y + mod, 0);

                    if (!(tilesInRange.Contains(testedTile) || prospectiveTiles.Contains(testedTile))) prospectiveTiles.Add(testedTile);
                }
            }

            focusedTiles = prospectiveTiles;

            tilesInRange.AddRange(focusedTiles);

        }

        if (tilesInRange.Contains(center)) tilesInRange.Remove(center);

        return tilesInRange.Distinct().ToList();

    }

    public List<Vector3Int> FindBestPathInRange(Vector3Int startTilePos, Vector3Int endTilePos, List<Vector3Int> moveableTilePositions)
    {

        //only look for end tile position if it is in range (makes sense)
        if (moveableTilePositions.Contains(endTilePos) || startTilePos.Equals(endTilePos))
        {
            //tiles that have been looked at already
            List<PathTile> closedTiles = new List<PathTile>();
            //Tiles to look at
            List<PathTile> openTiles = new List<PathTile>();

            //stores route to be returned
            List<Vector3Int> successfulRoute = new List<Vector3Int>();

            //current focused tile
            PathTile currentTile = null;

            //add start tile to open tiles
            openTiles.Add(new PathTile(startTilePos, endTilePos, null));

            int count = 0;

            while (true)
            {
                //makes sure there is no infinite while loop
                count++;
                if (count > 10000)
                {
                    Debug.Log($"Too many loops trying to go from {startTilePos} to {endTilePos}" );
                    return new();
                }


                int bestFValue = 10000;

                //Find lowest F value
                foreach (PathTile tile in openTiles)
                {
                    int f = tile.GetF();

                    if (f < bestFValue)
                    {
                        bestFValue = f;
                    }
                }

                //best tiles = tiles that have the lowest f value
                List<PathTile> bestTiles = openTiles.Where(u => u.GetF() == bestFValue).ToList();

                int bestHValue = 10000;

                //current tile = tile in best tiles where h value is lowest
                foreach (PathTile tile in bestTiles)
                {
                    int h = tile.GetH();

                    if (h < bestHValue) 
                    { 
                        bestHValue = h; 
                        currentTile = tile;
                    }
                }

                //remove current tile from open tiles and add it to closed tiles
                openTiles.Remove(currentTile);
                closedTiles.Add(currentTile);

                //if current tile is target tile, set the path taken to reach it
                if (currentTile.GetPathTilePosition().Equals(endTilePos))
                {
                    List<PathTile> currentPath = new List<PathTile>();
                    currentPath.Add(currentTile);

                    while (true)
                    {
                        PathTile parentTile = currentPath[currentPath.Count - 1].GetParentTile();

                        if (parentTile != null)
                        {
                            currentPath.Add(parentTile);
                        }
                        else break;
                    }

                    foreach (PathTile tile in currentPath)
                    {
                        successfulRoute.Add(tile.GetPathTilePosition());
                    }

                    successfulRoute.Reverse();

                    if (startTilePos.Equals(endTilePos)) successfulRoute.Clear();

                    return successfulRoute;
                }

                List<Vector3Int> neighborTilePositions = GetTilesInRangeExcludingCenter(currentTile.GetPathTilePosition(), 1);

                //for each neighbor of the current tile
                foreach (Vector3Int neighbor in neighborTilePositions)
                {
                    PathTile tileRef = new PathTile(neighbor, endTilePos, currentTile);
                     
                    //if neighbor is in range (traversable) and neighbor is not in closed tiles
                    if (moveableTilePositions.Contains(neighbor) && !closedTiles.Any(u => u.GetPathTilePosition().Equals(neighbor)))
                    {
                        if (!openTiles.Any(u => u.GetPathTilePosition().Equals(neighbor))) openTiles.Add(tileRef);

                        else if (tileRef.GetG() < openTiles.Where(u => u.GetPathTilePosition().Equals(neighbor)).Single().GetG()) 
                        {
                            openTiles.Remove(openTiles.Where(u => u.GetPathTilePosition().Equals(neighbor)).Single());
                            openTiles.Add(tileRef);
                        }

                    }
                }

            }

        }

        else
        {
            Debug.Log("EndPos outside of accessible Tiles.");
            return null;
        }

    }
    public List<Vector3Int> FindBestPath(Vector3Int startTilePos, Vector3Int endTilePos)
    {

        //tiles that have been looked at already
        List<PathTile> closedTiles = new List<PathTile>();
        //Tiles to look at
        List<PathTile> openTiles = new List<PathTile>();

        //stores route to be returned
        List<Vector3Int> successfulRoute = new List<Vector3Int>();

        //current focused tile
        PathTile currentTile = null;

        //add start tile to open tiles
        openTiles.Add(new PathTile(startTilePos, endTilePos, null));

        int count = 0;

        while (true)
        {
            //makes sure there is no infinite while loop
            count++;
            if (count > 10000)
            {
                Debug.Log("Too many loops");
                return new();
            }


            int bestFValue = 10000;

            //Find lowest F value
            foreach (PathTile tile in openTiles)
            {
                int f = tile.GetF();

                if (f < bestFValue)
                {
                    bestFValue = f;
                }
            }

            //best tiles = tiles that have the lowest f value
            List<PathTile> bestTiles = openTiles.Where(u => u.GetF() == bestFValue).ToList();

            int bestHValue = 10000;

            //current tile = tile in best tiles where h value is lowest
            foreach (PathTile tile in bestTiles)
            {
                int h = tile.GetH();

                if (h < bestHValue)
                {
                    bestHValue = h;
                    currentTile = tile;
                }
            }

            //remove current tile from open tiles and add it to closed tiles
            openTiles.Remove(currentTile);
            closedTiles.Add(currentTile);

            //if current tile is target tile, set the path taken to reach it
            if (currentTile.GetPathTilePosition().Equals(endTilePos))
            {
                List<PathTile> currentPath = new List<PathTile>();
                currentPath.Add(currentTile);

                while (true)
                {
                    PathTile parentTile = currentPath[currentPath.Count - 1].GetParentTile();

                    if (parentTile != null)
                    {
                        currentPath.Add(parentTile);
                    }
                    else break;
                }

                foreach (PathTile tile in currentPath)
                {
                    successfulRoute.Add(tile.GetPathTilePosition());
                }

                successfulRoute.Reverse();

                if (startTilePos.Equals(endTilePos)) successfulRoute.Clear();

                return successfulRoute;
            }

            List<Vector3Int> neighborTilePositions = GetTilesInRangeExcludingCenter(currentTile.GetPathTilePosition(), 1);

            //for each neighbor of the current tile
            foreach (Vector3Int neighbor in neighborTilePositions)
            {
                PathTile tileRef = new PathTile(neighbor, endTilePos, currentTile);

                //if neighbor is traversable and neighbor is not in closed tiles

                if (_tilemap.GetTile(neighbor) != null && !closedTiles.Any(u => u.GetPathTilePosition().Equals(neighbor)))
                {
                    if (_dataFromTiles[_tilemap.GetTile(neighbor)].walkable)
                    {
                        if (!openTiles.Any(u => u.GetPathTilePosition().Equals(neighbor))) openTiles.Add(tileRef);

                        else if (tileRef.GetG() < openTiles.Where(u => u.GetPathTilePosition().Equals(neighbor)).Single().GetG())
                        {
                            openTiles.Remove(openTiles.Where(u => u.GetPathTilePosition().Equals(neighbor)).Single());
                            openTiles.Add(tileRef);
                        }
                    }
                }
            }

        }


    }

    public List<Vector3> GetPath(Vector3Int startPos, Vector3Int endPos, List<Vector3Int> accessibleTiles)
    {
        if (startPos != null && endPos != null && accessibleTiles != null)
        {

            if (accessibleTiles.Contains(endPos))
            {
                List<Vector3Int> path = FindBestPathInRange(startPos, endPos, accessibleTiles);

                if (path != null)
                {
                    List<Vector3> adjustedPath = new List<Vector3>();

                    for (int i = 0; i < path.Count; i++)
                    {
                        adjustedPath.Add(new Vector3(path[i].x + 0.5f, path[i].y + 0.5f, 0));
                    }

                    return adjustedPath;
                }

            }


        }

        return new();


    }
    public Vector3Int GetPosition(GameObject unit) { return _tilemap.WorldToCell(unit.transform.position); }

#warning Fix Enemy AI (Sometimes, it just decides not to attack (I think it happens when enemy is surrounded by janitors from three sides) )
    public GameObject FindBestTarget(GameObject enemy)
    {
        Unit enemyUnit = enemy.GetComponent<Unit>();

        (List<Vector3Int>, List<Vector3Int>) moveAndAttackRanges = GetMoveableTiles(enemy, GetPosition(enemy), _enemies, _janitors);

        List<Vector3Int> moveAndAttackRange = moveAndAttackRanges.Item1; moveAndAttackRange.AddRange(moveAndAttackRanges.Item2);

        List<GameObject> janitorsInRange = new List<GameObject>();
        List<GameObject> janitorsOutOfRange = new List<GameObject>();

        foreach (GameObject janitor in _janitors)
        {
            if (moveAndAttackRange.Contains(GetPosition(janitor))) janitorsInRange.Add(janitor);
            else janitorsOutOfRange.Add(janitor);
        }

        //dictionary: potential target, target priority
        Dictionary<GameObject, int> janitorPrioritization = new Dictionary<GameObject, int>();
        //dictionary: potential target, damage dealt
        Dictionary<GameObject, int> dmgDealtToEachJanitor = new Dictionary<GameObject, int>();

        //Find attack priority of janitors in attack range
        foreach (GameObject janitor in janitorsInRange)
        {
            Unit janitorUnit = janitor.GetComponent<Unit>();

            bool enemyDouble = false;

            int dmgAmount = enemyUnit.GetAtk() - janitorUnit.GetDef();
            if (dmgAmount < 0) dmgAmount = 0;
            if (enemyUnit.GetSpd() - janitorUnit.GetSpd() >= 5)
            {
                dmgAmount *= 2;
            }

            dmgDealtToEachJanitor.Add(janitor, dmgAmount);


            int counterDmgAmount = 0;

            //if enemy is able to counter
            if (enemyUnit.GetRng() <= janitorUnit.GetRng())
            {
                counterDmgAmount = janitorUnit.GetAtk() - enemyUnit.GetDef();
                if (counterDmgAmount < 0) dmgAmount = 0;
                if (janitorUnit.GetSpd() - enemyUnit.GetSpd() >= 5)
                {
                    counterDmgAmount *= 2;
                    enemyDouble = true;
                }
            }

            //If killable by single hit, highest priority
            if ((enemyDouble && janitorUnit.GetHp() <= dmgAmount / 2) || (!enemyDouble && janitorUnit.GetHp() <= dmgAmount))
            {
                janitorPrioritization.Add(janitor, 1);
            }

            //If counter is survivable, second highest priority
            else if (counterDmgAmount < enemyUnit.GetHp())
            {
                janitorPrioritization.Add(janitor, 2);
            }

            //If counter is not survivable, third highest priority
            else
            {
                janitorPrioritization.Add(janitor, 3);
            }


        }

        //Make list of janitors that are at highest attacking priority
        int highestPriority = 3;
        foreach (int priority in janitorPrioritization.Values)
        {
            if (priority < highestPriority) highestPriority = priority;
        }
        List<GameObject> targetJanitors = new List<GameObject>();
        foreach (GameObject janitor in janitorPrioritization.Keys)
        {
            if (janitorPrioritization[janitor] == highestPriority) targetJanitors.Add(janitor);
        }

        if (targetJanitors.Count > 1)
        {
            //tiebreaker
            //target janitors refined to janitors in target janitors that can be dealt the most damage
            List<GameObject> mostDamagedJanitors = new List<GameObject>();

            int highestDmg = 0;

            //find highest damage number
            foreach (int dmgDealt in dmgDealtToEachJanitor.Values)
            {
                if (dmgDealt > highestDmg) highestDmg = dmgDealt;
            }
            //set janitors that match that number
            foreach (GameObject janitor in targetJanitors)
            {
                if (dmgDealtToEachJanitor[janitor] == highestDmg) mostDamagedJanitors.Add(janitor);
            }

            targetJanitors = mostDamagedJanitors;

        }


        if (targetJanitors.Count > 0) 
        {
            GameObject targetJanitor = targetJanitors[0];

            enemyUnit.SetAttackTarget(targetJanitor);
            return targetJanitor;
        }

        else return null;
    }

    public Vector3Int FindBestMoveLocation(GameObject enemy, GameObject targetJanitor)
    {

        Vector3Int targetLocation = new();

        Unit enemyUnit = enemy.GetComponent<Unit>();

        (List<Vector3Int>, List<Vector3Int>) moveAndAttackRanges = GetMoveableTiles(enemy, GetPosition(enemy), _enemies, _janitors);
        List<Vector3Int> moveRange = moveAndAttackRanges.Item1;




        //targetJanitor is not null when there is a janitor in attack range
        if (targetJanitor != null)
        {
            List<Vector3Int> moveablePotentialAttackSquares = GetTilesInRangeExcludingCenter(GetPosition(targetJanitor), enemyUnit.GetRng());
            moveablePotentialAttackSquares.RemoveAll(u => !moveRange.Contains(u));

            List<Vector3Int> unitSquares = new();
            foreach (GameObject unit in _allUnits)
            {
                foreach (Vector3Int tile in moveablePotentialAttackSquares)
                {
                    if (tile.Equals(unit.GetComponent<Unit>().GetPosition())) unitSquares.Add(tile);
                }
            }
            moveablePotentialAttackSquares = moveablePotentialAttackSquares.Except(unitSquares).ToList();

            if (moveablePotentialAttackSquares.Count == 0) targetLocation = FindBestMoveLocation(enemy, null);
            else targetLocation = FindSquareWithLeastAttackers(moveablePotentialAttackSquares);

        }

        else
        {
            //1st, find janitor with highest [(dmg) - (moves away * 5)] value
            //2nd, find closest tile you can move to
            //3rd (if multiple) find square with least attackers

            int highestTargetQuality = -10000;

            Dictionary<GameObject, int> janitorQualityPairs = new();

            foreach (GameObject janitor in _janitors)
            {
                Unit janitorUnit = janitor.GetComponent<Unit>();

                int dmgDealt = enemyUnit.GetAtk() - janitorUnit.GetDef();
                if (dmgDealt < 0) dmgDealt = 0;


                int squaresAway = FindBestPath(GetPosition(enemy), GetPosition(janitor)).Count - 1;


                int movesAway = (int)(squaresAway / enemyUnit.GetMov());

                int targetQuality = dmgDealt - movesAway * 5;

                if (targetQuality > highestTargetQuality) highestTargetQuality = targetQuality;

                janitorQualityPairs.Add(janitor, targetQuality);
            }

            GameObject bestTarget = janitorQualityPairs.Keys.Where(u => janitorQualityPairs[u] == highestTargetQuality).FirstOrDefault();

            int lowestDist = 10000;


            
            List<Vector3Int> unitSquares = new();

            foreach (GameObject unit in _allUnits)
            {
                foreach (Vector3Int tile in moveRange)
                {
                    if (tile.Equals(unit.GetComponent<Unit>().GetPosition())) unitSquares.Add(tile);
                }
            }

            moveRange = moveRange.Except(unitSquares).ToList();



            foreach (Vector3Int tile in moveRange)
            {
                int dist = FindBestPath(tile, GetPosition(bestTarget)).Count - 1;

                if (dist < lowestDist)
                {
                    lowestDist = dist;
                }
            }

            List<Vector3Int> lowestDistLocations = moveRange.Where(u => FindBestPath(u, GetPosition(bestTarget)).Count - 1 == lowestDist).ToList();

            if (lowestDistLocations.Count > 1) targetLocation = FindSquareWithLeastAttackers(lowestDistLocations);
            else targetLocation = lowestDistLocations[0];

        }

        return targetLocation;
    }

    public Vector3Int FindSquareWithLeastAttackers(List<Vector3Int> consideredSquares)
    {
            Dictionary<GameObject, List<Vector3Int>> attackerRangePairs = new();

        foreach (GameObject janitor in _janitors)
        {
            (List<Vector3Int>, List<Vector3Int>) janitorMoveAndAttackRanges = GetMoveableTiles(janitor, GetPosition(janitor), _enemies, _janitors);

            List<Vector3Int> janitorMoveAndAttackRange = janitorMoveAndAttackRanges.Item1;
            janitorMoveAndAttackRange.AddRange(janitorMoveAndAttackRanges.Item2);

            attackerRangePairs.Add(janitor, janitorMoveAndAttackRange);
        }


        Dictionary<Vector3Int, int> tileAttackerPairs = new();
        int leastAttackers = 10000;

        foreach (Vector3Int tile in consideredSquares)
        {
            int attackers = 0;

            foreach (List<Vector3Int> attackerRange in attackerRangePairs.Values)
            {
                if (attackerRange.Contains(tile)) attackers++;
            }

            tileAttackerPairs.Add(tile, attackers);

            if (attackers < leastAttackers) leastAttackers = attackers;
        }

        return tileAttackerPairs.Keys.Where(u => tileAttackerPairs[u] == leastAttackers).FirstOrDefault();
    }

    public int GetDistance(Vector3Int pos1,  Vector3Int pos2)
    {
        return Math.Abs(pos2.x - pos1.x) + Math.Abs(pos2.y - pos1.y);
    }

}