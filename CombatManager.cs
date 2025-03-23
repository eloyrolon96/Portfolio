using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public class CombatManager : MonoBehaviour
{
    private static CombatManager instance;
    public static CombatManager Instance
    {
        get { return instance; }
    }

    public List<Character> charactersInGame = new List<Character>();
    public List<Character> characters = new List<Character>();
    public TurnsManager turns;

    public static int minDamage = 0;
    public static int maxDamage = 9999;

    public static int minAttack = 0;
    public static int maxAttack = 150;

    public static float damageRange = 0.1f;

    [SerializeField] public BattleInterface battleUI;

    public event BattleStartEventHandler startBattle;
    public delegate void BattleStartEventHandler();

    public event EndTurnEventHandler EndTurn;
    public delegate void EndTurnEventHandler();

    public event EndSelectingModeEventHandler EndSelectingMode;
    public delegate void EndSelectingModeEventHandler();

    public event HPToUpdateEventHandler HPToUpdate;
    public delegate void HPToUpdateEventHandler(Character character, int previousHP, int currentHP);

    public event AttackEventHandler Attacking;
    public delegate void AttackEventHandler(Character character, Enemy enemy = null);

    public event CharacterDyingEventHandler CharacterDying;
    public delegate void CharacterDyingEventHandler(Character character);

    public event GameOverEventHandler GameOver;
    public delegate void GameOverEventHandler();

    [SerializeField] private bool committingBattleAction = false;

    private int IDCounter = 0;

    public Pointer pointer;

    private bool gameIsOver;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        gameIsOver = false;

        foreach (Character character in characters)
        {
            charactersInGame.Add(character);
        }

        CombatManager.Instance.startBattle += OnBattleStart;

        CombatManager.Instance.Attacking += OnCharacterAttacking;

        CombatManager.Instance.EndTurn += OnTurnEnd;

        CombatManager.Instance.EndSelectingMode += OnSelectingModeEnd;

        CombatManager.Instance.HPToUpdate += OnHPToUpdate;

        CombatManager.Instance.GameOver += OnGameOver;

        CombatManager.Instance.CharacterDying += OnCharacterDying;

        startBattle?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameIsOver)
        {
            Debug.Log("GAME OVER");
            return;
        }

        if (turns.turnNumberIsOverList[turns.currentTurnNumber] && !gameIsOver)
        {
            callNextTurn();
        }

        if (!turns.turnsList[turns.currentTurnNumber].isAlly && !committingBattleAction)
        {
           Attack();
        }     
    }

    private void OnBattleStart()
    {
        battleUI.InitializeBattleScene();

        turns = FindObjectOfType<TurnsManager>();
        if (turns == null)
        {
            turns.Initialize();
        }
        turns.initializeTurns(charactersInGame);

        battleUI.callBattleUI(turns.turnsList[turns.currentTurnNumber]);

        if (turns.turnsList[turns.currentTurnNumber].isAlly)
        {
            pointer.movePointerTo(turns.turnsList[turns.currentTurnNumber]);
            pointer.gameObject.SetActive(true);
        }
    }

    private void callNextTurn()
    {                
        removeDeadCharacters();
        Debug.Log("removeDeadCharacters");

        if (gameIsOver) return;
        if (committingBattleAction) return;

        turns.nextTurn(turns.currentTurnNumber);
        Debug.Log("nextTurn");

        battleUI.callBattleUI(turns.turnsList[turns.currentTurnNumber]);
        Debug.Log("callBattleUI");

        pointer.movePointerTo(turns.turnsList[turns.currentTurnNumber]);
        Debug.Log("movePointerTo");

        //pointer.gameObject.SetActive(true);
        //Debug.Log("SetActive");
       
        //turns.calculateFollowingTurns(characters);
        //Debug.Log("calculateFollowingTurns");        
    }

    public void Attack(Enemy enemy = null)
    {
        committingBattleAction = true;
        battleUI.removeBattleMenus();
        //pointer.gameObject.SetActive(false);

        Attacking?.Invoke(turns.turnsList[turns.currentTurnNumber], enemy);

        EndSelectingMode?.Invoke();
    }

    private void OnCharacterAttacking(Character character, Enemy enemy = null)
    {
        if(character.isAlly)
        {
            allyAttack(character, enemy); 
        }

        if(!character.isAlly)
        {
            enemyAttack(character);
        }
    }

    private void allyAttack(Character character, Enemy enemy)
    {
        MoveAllyOnAttack(character);
        //Enemy.Instance.health = Enemy.Instance.health - calculateDamage(character.physicAttack, Enemy.Instance.physicDefense);
        enemy.health = enemy.health - calculateDamage(character.physicAttack, enemy.physicDefense);

        if(enemy.health <= 0)
        {
            //enemy.isAlive = false;
            CharacterDying?.Invoke(enemy);
        }
    }

    private void MoveAllyOnAttack(Character character)
    {
        character.transform.position += new Vector3(0.5f, 0, 0);
        StartCoroutine(BackToThePlace(character, character.initialPosition));
    } 

    private void enemyAttack(Character enemy)
    {
        Character character;
        character = getRandomCharacter();

        int previousHP = 0;
        int currentHP = 0;

        Debug.Log(character);
        MoveEnemyOnAttack(enemy);

        previousHP = character.health;
        character.health = character.health - calculateDamage(enemy.physicAttack, Warrior.Instance.physicDefense);

        currentHP = character.health;

        HPToUpdate?.Invoke(character, previousHP, currentHP);

        if(character.health <= 0)
        { 
            //character.isAlive = false;
            CharacterDying?.Invoke(character);
        }
    }

    private void OnTurnEnd()
    {       
        turns.turnNumberIsOverList[turns.currentTurnNumber] = true; 
    }

    private void OnSelectingModeEnd()
    {
        battleUI.removeBattleMenus();
        pointer.gameObject.SetActive(false);
    }

    private void MoveEnemyOnAttack(Character character)
    {
        character.transform.position += new Vector3(-0.5f, 0, 0);
        StartCoroutine(BackToThePlace(character, character.initialPosition));
    }

    private IEnumerator BackToThePlace(Character character, Vector3 initialPosition)
    {
        yield return new WaitForSeconds(1f);
        character.transform.position = initialPosition;

        committingBattleAction = false;

        EndTurn?.Invoke();
    }

    private int calculateDamage(int attack, int defense)
    {
        float fixedDamage = (maxDamage * (Mathf.Pow(attack, 2) / Mathf.Pow(maxAttack, 2)));
        //Debug.Log("fixedDamage: " + fixedDamage);

        int minDamageRange = (int)(fixedDamage - (fixedDamage * damageRange));
        int maxDamageRange = (int)(fixedDamage + (fixedDamage * damageRange));

        int flatDamage = Random.Range(minDamageRange, maxDamageRange);
        //Debug.Log("flatDamage: " + flatDamage);


        float reductionPercentage = (float)defense / (attack + defense);
        int finalDamage = (int)(flatDamage * (1 - reductionPercentage));
        //Debug.Log("finalDamage: " + finalDamage);

        return finalDamage;
    }

    private Character getRandomCharacter()
    {
        if (charactersInGame == null || charactersInGame.Count == 0)
        {
            return null;
        }

        Character randomCharacter;
        do
        {
            int randomIndex = Random.Range(0, charactersInGame.Count);
            randomCharacter = charactersInGame[randomIndex];
        } while (!(randomCharacter.isAlly && randomCharacter.isAlive));

        return randomCharacter;
    }
    private void OnHPToUpdate(Character character, int previousHP, int currentHP)
    {
        battleUI.updateHP(character, previousHP, currentHP);
    }

    private void removeDeadCharacters()
    {
        List<Character> deadCharacters = new List<Character>();
        //deadCharacters = charactersInGame.FindAll(character => character.health <= 0 && character.isAlive);

        deadCharacters = charactersInGame.FindAll(character => character.isAlive == false);
        /*
        foreach (Character character in deadCharacters)
        {
            //character.isAlive = false;           
        }
        */

        if (deadCharacters.Count > 0)
        {
            turns.removeDeadCharactersTurns(deadCharacters);
        }

        if (!characters.Any(character => character.isAlive && character.isAlly))
        {
            GameOver?.Invoke();
        }
    }

    private void OnCharacterDying(Character character)
    {
        character.kill();
    }

    private void OnGameOver()
    {
        //battleUI.remove();

        pointer.gameObject.SetActive(false);

        gameIsOver = true;
        Debug.Log("Game Over");

        CombatManager.Instance.Attacking -= OnCharacterAttacking;

        CombatManager.Instance.EndTurn -= OnTurnEnd;

        CombatManager.Instance.EndSelectingMode -= OnSelectingModeEnd;

        CombatManager.Instance.HPToUpdate -= OnHPToUpdate;           
    }

    public int GenerateCharacterID()
    {
        IDCounter++;

        return IDCounter;
    }
}
