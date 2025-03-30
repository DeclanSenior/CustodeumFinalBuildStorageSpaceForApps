using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class UIManager : MonoBehaviour
{
    public static event Action BattleOverEvent;

    [SerializeField] private SFXManager _SFXManager;

    [SerializeField] private Dictionary<UnitType, Animation> _unitTypeAnimationPairs;

    [SerializeField] private GameObject _unitStatsPanel;

        [SerializeField] private GameObject _headerName;
        [SerializeField] private GameObject _unitLevel;
        [SerializeField] private GameObject _will;
        [SerializeField] private GameObject _exp;
        [SerializeField] private GameObject _stats1;
        [SerializeField] private GameObject _stats2;
        [SerializeField] private GameObject _movement;
        [SerializeField] private GameObject _range;

        [SerializeField] private GameObject _hoverHpBar;
        [SerializeField] private GameObject _predictiveHpBar;
        [SerializeField] private GameObject _hoverExpBar;

    Coroutine pulseHP = null;

    [SerializeField] private GameObject _unitNamePanel;

        [SerializeField] private GameObject _unitName;



    [SerializeField] private Camera _camera;

    [SerializeField] private int _statsPanelWidth;
    [SerializeField] private int _statsPanelHeight;
    [SerializeField] private int _namePanelWidth;
    [SerializeField] private int _namePanelHeight;

    [SerializeField] private int margin;


    [SerializeField] private GameObject _battlePanel;
        
        [SerializeField] private GameObject _battlingJanitor;
        [SerializeField] private GameObject _battlingEnemy;

        [SerializeField] private GameObject _battlingJanitorNameTag;
        [SerializeField] private GameObject _battlingEnemyNameTag;

        [SerializeField] private GameObject _janitorBattleHealthBar;
        [SerializeField] private GameObject _enemyBattleHealthBar;

        [SerializeField] private GameObject _battleExpBar;

    [SerializeField] private GameObject _pausePanel;

    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _fadeOutObject;

    [SerializeField] private GameObject saveText;

    private Action<GameObject, GameObject> attackUIDelegate;
    public static event Action GameOverDoneEvent;

    private bool _attackCompleted;
    private bool _attackConnected;
    private bool _healthBarDone;

    private Coroutine _battleTimer;
    private Coroutine _runningReduceHealthCoroutine;

    private IEnumerator TimeBattle()
    {
        yield return new WaitForSecondsRealtime(8f);
       
        BattleOverEvent?.Invoke();

        _battlingJanitor.GetComponent<SpriteRenderer>().color = Color.white;
        _battlingEnemy.GetComponent<SpriteRenderer>().color = Color.white;

        _battlePanel.SetActive(false);

    }

    private void PlaySound(string unitTypeName)
    {
        _SFXManager.PlayAudio(unitTypeName);
    }

    private void DisplayJSON(string json)
    {
        saveText.GetComponent<TMP_InputField>().text = json;
    }

    private void Awake()
    {
        GameManager.PDownEvent += PDown;
        GameManager.DisplayJSON += DisplayJSON;
        UnpauseButton.UnpauseClicked += PDown;
        UnitManager.GameOverEvent += DisplayGameOverMessage;
    }
    void OnEnable()
    {
        attackUIDelegate = (attacker, defender) => StartCoroutine(BattleUnits(attacker, defender));

        _unitStatsPanel.SetActive(false);
        _unitNamePanel.SetActive(false);

        UnitManager.DisplayUnitUIEvent += DisplaySelectedUnitUI;
        UnitManager.DisplayBattleUIEvent += attackUIDelegate;

        AnimationUtils.EndAnimationEvent += AttackCompleted;
        AnimationUtils.AttackConnectedEvent += AttackConnected;
        AnimationUtils.SoundEffectUnitTypeNameEvent += PlaySound;

    }

    private void OnDestroy()
    {
        UnitManager.DisplayUnitUIEvent -= DisplaySelectedUnitUI;
        UnitManager.DisplayBattleUIEvent -= attackUIDelegate;

        GameManager.PDownEvent -= PDown;
        UnpauseButton.UnpauseClicked -= PDown;
        GameManager.DisplayJSON -= DisplayJSON;

        AnimationUtils.EndAnimationEvent -= AttackCompleted;
        AnimationUtils.AttackConnectedEvent -= AttackConnected;
        AnimationUtils.SoundEffectUnitTypeNameEvent -= PlaySound;

        UnitManager.GameOverEvent -= DisplayGameOverMessage;
    }

    private void DisplayGameOverMessage()
    {
        StartCoroutine(GameOverMessageCoroutine());
    }

    private IEnumerator GameOverMessageCoroutine()
    {
        _fadeOutObject.SetActive(true);
        yield return UnfadeObject(_fadeOutObject);
        _gameOverPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(3f);
        _gameOverPanel.SetActive(false);
        yield return FadeObject(_fadeOutObject, false);
        _fadeOutObject.SetActive(false);

        GameOverDoneEvent?.Invoke();
    }

    private void PDown()
    {
        if (enabled)
        {
            enabled = false;
            _unitStatsPanel.SetActive(false);
            _unitNamePanel.SetActive(false);
            _battlePanel.SetActive(false);
            _pausePanel.SetActive(true);
        }
        else
        {
            enabled = true;
            _pausePanel.SetActive(false);
        }
    }
    private IEnumerator BattleUnits(GameObject attacker, GameObject defender)
    {
        GameManager.PDownEvent -= PDown;

        //_battleTimer = StartCoroutine(TimeBattle());

        Unit janitorUnit;
        Unit enemyUnit;

        if (attacker.GetComponent<Unit>().GetTeam() == Team.Janitor)
        {
            janitorUnit = attacker.GetComponent<Unit>();
            enemyUnit = defender.GetComponent<Unit>();
        }
        else
        {
            janitorUnit = defender.GetComponent<Unit>();
            enemyUnit = attacker.GetComponent<Unit>();
        }

        int bonusExpUponKill = (int)(enemyUnit.GetBaseExp() * ((float)enemyUnit.GetLvl() / janitorUnit.GetLvl()));

        _battlePanel.SetActive(true);

        _battlingJanitorNameTag.GetComponent<TextMeshProUGUI>().text = janitorUnit.GetUnitName();
        _battlingEnemyNameTag.GetComponent<TextMeshProUGUI>().text = enemyUnit.GetUnitName();
        
        
        RuntimeAnimatorController janitorController = AnimationManager.Instance.GetAnimationController(janitorUnit.GetUnitType());
        RuntimeAnimatorController enemyController = AnimationManager.Instance.GetAnimationController(enemyUnit.GetUnitType());

        _battlingJanitor.GetComponent<Animator>().runtimeAnimatorController = janitorController;
        _battlingEnemy.GetComponent<Animator>().runtimeAnimatorController = enemyController;

        Animator janitorAnimator = _battlingJanitor.GetComponent<Animator>();
        Animator enemyAnimator = _battlingEnemy.GetComponent<Animator>();

        janitorAnimator.speed = 0;
        enemyAnimator.speed = 0;


        Animator attackingAnimator;
        Animator defendingAnimator;

        Unit attackingUnit;
        Unit defendingUnit;

        GameObject battlingAttacker;
        GameObject battlingDefender;

        GameObject attackingHealthBar;
        GameObject defendingHealthBar;


        /*Janitor Attacking*/
        if (attacker.GetComponent<Unit>().GetTeam() == Team.Janitor)
        {
            attackingAnimator = janitorAnimator;
            defendingAnimator = enemyAnimator;

            attackingUnit = janitorUnit;
            defendingUnit = enemyUnit;

            battlingAttacker = _battlingJanitor;
            battlingDefender = _battlingEnemy;

            attackingHealthBar = _janitorBattleHealthBar;
            defendingHealthBar = _enemyBattleHealthBar;

            _battleExpBar.GetComponent<ProgressBar>().DisplayProgress(attackingUnit.GetExp() / 100f);

        }
        /*Enemy Attacking*/
        else
        {
            attackingAnimator = enemyAnimator;
            defendingAnimator = janitorAnimator;

            attackingUnit = enemyUnit;
            defendingUnit = janitorUnit;

            battlingAttacker = _battlingEnemy;
            battlingDefender = _battlingJanitor;

            defendingHealthBar = _janitorBattleHealthBar;
            attackingHealthBar = _enemyBattleHealthBar;

            _battleExpBar.GetComponent<ProgressBar>().DisplayProgress(defendingUnit.GetExp() / 100f);

        }


        attackingHealthBar.GetComponent<ProgressBar>().DisplayProgress(attackingUnit.GetHPPercent());
        defendingHealthBar.GetComponent<ProgressBar>().DisplayProgress(defendingUnit.GetHPPercent());

        attackingAnimator.speed = 0;
        defendingAnimator.speed = 0;

        yield return new WaitForSecondsRealtime(0.5f);


        //attack


        System.Random randomizer = new System.Random();


        attackingAnimator.speed = 1 + (((float)randomizer.NextDouble()) / 4) - 0.125f;
        battlingAttacker.GetComponent<SpriteRenderer>().sortingOrder += 1;

        yield return new WaitUntil(() => _attackConnected);
        _attackConnected = false;

        if (_runningReduceHealthCoroutine != null) StopCoroutine(_runningReduceHealthCoroutine);
        _runningReduceHealthCoroutine = StartCoroutine(ReduceHealth(defender, defendingHealthBar, defendingUnit.GetHp(), defendingUnit.GetHp() - (attackingUnit.GetAtk() - defendingUnit.GetDef())));


        yield return new WaitUntil(() => _attackCompleted);
        _attackCompleted = false;

        attackingAnimator.speed = 0;
        battlingAttacker.GetComponent<SpriteRenderer>().sortingOrder -= 1;

        yield return new WaitUntil(() => _healthBarDone);

        bool survivingEnemy = true;

        

        //if attacked unit is still alive
        if (defendingUnit.GetHp() - (attackingUnit.GetAtk() - defendingUnit.GetDef()) > 0)
        {
            bool counterKill = false;

            // if attacker is in range, counter
            if (Math.Abs((defendingUnit.GetPosition().x - attackingUnit.GetPosition().x)) + Math.Abs((defendingUnit.GetPosition().y - attackingUnit.GetPosition().y)) <= defendingUnit.GetRng())
            {
                yield return new WaitForSecondsRealtime(0.5f);

                defendingAnimator.speed = 1 + (((float)randomizer.NextDouble()) / 4) - 0.125f;
                battlingDefender.GetComponent<SpriteRenderer>().sortingOrder += 1;

                yield return new WaitUntil(() => _attackConnected);
                _attackConnected = false;

                if (_runningReduceHealthCoroutine != null) StopCoroutine(_runningReduceHealthCoroutine);
                _runningReduceHealthCoroutine = StartCoroutine(ReduceHealth(attacker, attackingHealthBar, attackingUnit.GetHp(), attackingUnit.GetHp() - (defendingUnit.GetAtk() - attackingUnit.GetDef())));


                yield return new WaitUntil(() => _attackCompleted);
                _attackCompleted = false;

                defendingAnimator.speed = 0;
                battlingDefender.GetComponent<SpriteRenderer>().sortingOrder -= 1;

                yield return new WaitUntil(() => _healthBarDone);


                //if countered unit is still alive and countering unit has speed advantage, counter again
                if (attackingUnit.GetHp() - (defendingUnit.GetAtk() - attackingUnit.GetDef()) > 0)
                {
                    if (defendingUnit.GetSpd() - attackingUnit.GetSpd() >= 5)
                    {
                        yield return new WaitForSecondsRealtime(0.5f);

                        defendingAnimator.speed = 1 + (((float)randomizer.NextDouble()) / 4) - 0.125f;
                        battlingDefender.GetComponent<SpriteRenderer>().sortingOrder += 1;


                        yield return new WaitUntil(() => _attackConnected);
                        _attackConnected = false;

                        if (_runningReduceHealthCoroutine != null) StopCoroutine(_runningReduceHealthCoroutine);
                        _runningReduceHealthCoroutine = StartCoroutine(ReduceHealth(attacker, attackingHealthBar, attackingUnit.GetHp() - (defendingUnit.GetAtk() - attackingUnit.GetDef()), attackingUnit.GetHp() - 2 * (defendingUnit.GetAtk() - attackingUnit.GetDef())));


                        yield return new WaitUntil(() => _attackCompleted);
                        _attackCompleted = false;

                        defendingAnimator.speed = 0;
                        battlingDefender.GetComponent<SpriteRenderer>().sortingOrder -= 1;

                        yield return new WaitUntil(() => _healthBarDone);


                        if (attackingUnit.GetHp() - 2 * (defendingUnit.GetAtk() - attackingUnit.GetDef()) <= 0)
                        {

                            if (attacker.GetComponent<Unit>().GetTeam() == Team.Enemy) survivingEnemy = false;

                            //if double counter killed, fade unit
                            Debug.Log("Criminal 1");
                            yield return StartCoroutine(FadeObject(battlingAttacker, true));

                            yield return new WaitForSecondsRealtime(0.5f);

                        }
                    }
                }
                //if countered unit was killed by counter
                else
                {
                    if (attacker.GetComponent<Unit>().GetTeam() == Team.Enemy) survivingEnemy = false;

                    //fade dead unit if initial counter would kill
                    Debug.Log("Criminal 2");
                    yield return StartCoroutine(FadeObject(battlingAttacker, true));
                    
                    counterKill = true;


                    yield return new WaitForSecondsRealtime(0.5f);
                    

                }
            }


            //if attacking unit is still alive
            if (!counterKill)
            {
                if (attackingUnit.GetHp() - (defendingUnit.GetAtk() - attackingUnit.GetDef()) > 0)
                {
                    //and attacking unit has speed advantage, attack again
                    if (attackingUnit.GetSpd() - defendingUnit.GetSpd() >= 5)
                    {
                        yield return new WaitForSecondsRealtime(0.5f);

                        attackingAnimator.speed = 1 + (((float)randomizer.NextDouble()) / 4) - 0.125f;
                        battlingAttacker.GetComponent<SpriteRenderer>().sortingOrder += 1;

                        yield return new WaitUntil(() => _attackConnected);
                        _attackConnected = false;

                        if (_runningReduceHealthCoroutine != null) StopCoroutine(_runningReduceHealthCoroutine);
                        _runningReduceHealthCoroutine = StartCoroutine(ReduceHealth(defender, defendingHealthBar, defendingUnit.GetHp() - (attackingUnit.GetAtk() - defendingUnit.GetDef()), defendingUnit.GetHp() - 2 * (attackingUnit.GetAtk() - defendingUnit.GetDef())));

                        yield return new WaitUntil(() => _attackCompleted);
                        _attackCompleted = false;

                        attackingAnimator.speed = 0;
                        battlingAttacker.GetComponent<SpriteRenderer>().sortingOrder -= 1;

                        yield return new WaitUntil(() => _healthBarDone);



                        //if double attack killed
                        if (defendingUnit.GetHp() - 2 * (attackingUnit.GetAtk() - defendingUnit.GetDef()) <= 0)
                        {
                            if (defender.GetComponent<Unit>().GetTeam() == Team.Enemy) survivingEnemy = false;

                            Debug.Log("Criminal 3");
                            yield return StartCoroutine(FadeObject(battlingDefender, true));
                            yield return new WaitForSecondsRealtime(0.5f);
                        }
                    }

                }

            }
            

        }
        else 
        {
            /*fade dead unit if initial attack kills*/

            if (defender.GetComponent<Unit>().GetTeam() == Team.Enemy) survivingEnemy = false;

            Debug.Log("Criminal 4");
            yield return StartCoroutine(FadeObject(battlingDefender, true));

            yield return new WaitForSecondsRealtime(0.5f);

         
        }
        

        if (survivingEnemy) bonusExpUponKill = 0;
        if (defendingUnit.GetTeam() == Team.Janitor) yield return StartCoroutine(IncreaseExp(defender, _battleExpBar, 10 + bonusExpUponKill));
        else yield return IncreaseExp(attacker, _battleExpBar, 10 + bonusExpUponKill);

        yield return new WaitForSecondsRealtime(1);

        //StopCoroutine(_battleTimer);
        EndBattle(battlingAttacker, battlingDefender);

        GameManager.PDownEvent += PDown;
    }

    private IEnumerator ReduceHealth(GameObject parentUnit, GameObject healthBar, int startHP, int endHP)
    {
        _healthBarDone = false;

        if (endHP > startHP)
        {
            _SFXManager.PlayAudioOneShot("Clang");
            endHP = startHP;
        }
        else
        {
            float step = (startHP - endHP) / 600f;

            if (endHP < 0) endHP = 0;

            int maxHP = parentUnit.GetComponent<Unit>().GetMaxHP();

            float startPercent = (float)startHP / maxHP;
            float endPercent = (float)endHP / maxHP;

            for (float currentPercent = startPercent; currentPercent >= endPercent; currentPercent -= step)
            {
                healthBar.GetComponent<ProgressBar>().DisplayProgress(currentPercent);

                yield return new WaitForSecondsRealtime(1 / 60f);
            }

            healthBar.GetComponent<ProgressBar>().DisplayProgress(endPercent);

        }
        
        _healthBarDone = true;

    }

    private IEnumerator IncreaseExp(GameObject parentUnit, GameObject expBar, int addedExp)
    {
        Unit parentUnitUnit = parentUnit.GetComponent<Unit>();

        int startExp = parentUnitUnit.GetExp();
        int endExp = startExp + addedExp;

        float step = /*(endExp - startExp)*/  1 / 60f;

        float startPercent = startExp / 100f;
        float endPercent = endExp / 100f;

        for (float currentPercent = startPercent; currentPercent < endPercent; currentPercent += step)
        {
            _SFXManager.PlayAudioSetPitch("ExpClick", 0.7f, 0.8f + UnityEngine.Random.value * 0.2f);

            expBar.GetComponent<ProgressBar>().DisplayProgress(currentPercent % 1);
            yield return new WaitForSecondsRealtime(1 / 60f);
        }

        expBar.GetComponent<ProgressBar>().DisplayProgress(endPercent % 1);

        if (endPercent >= 1f) _SFXManager.PlayAudioSetPitch("LvlUp1", 1f, 1f);
    }

    private void EndBattle(GameObject battlingAttacker, GameObject battlingDefender)
    {
        BattleOverEvent?.Invoke();

        battlingAttacker.GetComponent<SpriteRenderer>().color = Color.white;
        battlingDefender.GetComponent<SpriteRenderer>().color = Color.white;

        _battlePanel.SetActive(false);
    }

    private void AttackCompleted()
    {
        _attackCompleted = true;
    }

    private void AttackConnected()
    {
        _attackConnected = true;
    }

    private IEnumerator FadeObject(GameObject obj, bool isUnit)
    {
        if (isUnit) _SFXManager.PlayAudio("FEFadeOut");

        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();

        for (float i = 1f; i > 0; i -= 0.05f)
        {
            renderer.color  = new(renderer.color.r, renderer.color.g, renderer.color.b, i);
            yield return null;
        }

        renderer.color = new(renderer.color.r, renderer.color.g, renderer.color.b, 0f);
    }

    private IEnumerator UnfadeObject(GameObject obj)
    {
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();

        for (float i = 0f; i < 1; i += 0.05f)
        {
            renderer.color = new(renderer.color.r, renderer.color.g, renderer.color.b, i);
            yield return null;
        }

        renderer.color = new(renderer.color.r, renderer.color.g, renderer.color.b, 1f);
    }

    private void DisplaySelectedUnitUI(GameObject selectedUnit, GameObject hoveredUnit)
    {
        if (hoveredUnit == null)
        {
            if (pulseHP != null)
            {
                StopCoroutine(pulseHP);
                pulseHP = null;
                _predictiveHpBar.GetComponent<SpriteRenderer>().color = new Color(1, 0, 0, 0.3f);
            }

            _unitStatsPanel.GetComponent<Image>().enabled = false;
            _unitStatsPanel.SetActive(false);
            _unitNamePanel.SetActive(false);

            _predictiveHpBar.SetActive(false);

            return;
        }

        
        _unitStatsPanel.GetComponent<Image>().enabled = true;
        _unitStatsPanel.SetActive(true);
        _unitNamePanel.SetActive(true);

        Unit hoveredUnitUnit = hoveredUnit.GetComponent<Unit>();

        string temp = hoveredUnitUnit.GetUnitName();
            _unitName.GetComponent<TextMeshProUGUI>().text = temp;
            _headerName.GetComponent<TextMeshProUGUI>().text = temp;


        _will.GetComponent<TextMeshProUGUI>().text = $"Will: {hoveredUnitUnit.GetHp()} / {hoveredUnitUnit.GetMaxHP()}";
        _unitLevel.GetComponent<TextMeshProUGUI>().text = $"Level {hoveredUnitUnit.GetLvl()}";
        _hoverHpBar.GetComponent<ProgressBar>().DisplayProgress(hoveredUnitUnit.GetHPPercent());
        _hoverExpBar.GetComponent<ProgressBar>().DisplayProgress(hoveredUnitUnit.GetExp() / 100f);
        _stats1.GetComponent<TextMeshProUGUI>().text = $"Efficiency: {hoveredUnitUnit.GetAtk()}\nResistance: {hoveredUnitUnit.GetDef()}";
        _stats2.GetComponent<TextMeshProUGUI>().text = $"Speed: {hoveredUnitUnit.GetSpd()}";
        _movement.GetComponent<TextMeshProUGUI>().text = $"Movement: {hoveredUnitUnit.GetMov()}";
        _range.GetComponent<TextMeshProUGUI>().text = $"Range: {hoveredUnitUnit.GetRng()}";

        RectTransform statsTransform = _unitStatsPanel.GetComponent<RectTransform>();
        RectTransform nameTransform = _unitNamePanel.GetComponent<RectTransform>();

        
        if (selectedUnit != null && hoveredUnitUnit.GetComponent<Unit>().GetTeam() != Team.Janitor)
        {
            Unit selectedUnitUnit = selectedUnit.GetComponent<Unit>();
            int dmgDealt = selectedUnitUnit.GetAtk() - hoveredUnitUnit.GetDef();

            if (dmgDealt > 0  && Math.Abs(selectedUnitUnit.GetPosition().x - hoveredUnitUnit.GetPosition().x) + Math.Abs(selectedUnitUnit.GetPosition().y - hoveredUnitUnit.GetPosition().y) <= selectedUnitUnit.GetRng())
            {
                _predictiveHpBar.SetActive(true);

                if (pulseHP != null)
                {
                    StopCoroutine(pulseHP);
                    pulseHP = null;
                    _predictiveHpBar.GetComponent<SpriteRenderer>().color = new Color(1, 0, 0, 0.3f);
                }

                pulseHP = StartCoroutine(PulseObject(_predictiveHpBar));

                if (selectedUnitUnit.GetSpd() - hoveredUnitUnit.GetSpd() > 4) dmgDealt *= 2;

                float remainingHp = (float)(hoveredUnitUnit.GetHp() - dmgDealt) / hoveredUnitUnit.GetMaxHP();
                if (remainingHp < 0) remainingHp = 0;

                _predictiveHpBar.transform.localScale = new Vector3(hoveredUnitUnit.GetHPPercent() - remainingHp, 1, 1);
                _predictiveHpBar.transform.localPosition = new Vector2(hoveredUnitUnit.GetHPPercent() - 0.5f - (_predictiveHpBar.transform.localScale.x / 2f), 0);

            }
        }
        

        if (hoveredUnit.transform.position.x <= _camera.ScreenToWorldPoint(new Vector3((Screen.width / 2), 0, 0)).x)
        {
            statsTransform.offsetMax = new(-margin, Screen.height * -0.65f);
            statsTransform.offsetMin = new(Screen.width * 0.675f, margin);

            nameTransform.offsetMax = new(-margin, -margin);
            nameTransform.offsetMin = new(Screen.width * 0.8f, Screen.height * 0.85f);
        }
        else
        {
            statsTransform.offsetMax = new(Screen.width * -0.675f, Screen.height * -0.65f);
            statsTransform.offsetMin = new(margin, margin);

            nameTransform.offsetMax = new(Screen.width * -0.8f, -margin);
            nameTransform.offsetMin = new(margin, Screen.height * 0.85f);
        }

    }

    private IEnumerator PulseObject(GameObject obj)
    {
        Color baseColor = obj.GetComponent<SpriteRenderer>().color;

        while (true)
        {
            obj.GetComponent<SpriteRenderer>().color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            for (float i = 0; i < 1; i += 0.1f)
            {
                obj.GetComponent<SpriteRenderer>().color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * i);

                yield return new WaitForSecondsRealtime(1 / 30f);
            }
            for (float i = 1; i > 0; i -= 0.1f)
            {
                obj.GetComponent<SpriteRenderer>().color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * i);

                yield return new WaitForSecondsRealtime(1 / 30f);
            }
        }
    }


}
