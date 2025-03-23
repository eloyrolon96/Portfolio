using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class TurnsManager : MonoBehaviour
{
    private static TurnsManager instance;
    public static TurnsManager Instance
    {
        get { return instance; }
    }

    public CombatManager combatManager;

    [SerializeField] public List<Character> turnsList;
    [SerializeField] public List<bool> turnNumberIsOverList;
    [SerializeField] public int currentTurnNumber;
    [SerializeField] public int turnsToshow;
    [SerializeField] public float minTurns = 1f;
    [SerializeField] public float maxTurns = 3f;

    [SerializeField] public List<Character> internalTurns = new List<Character>();
    [SerializeField] public int internalTurnCounter;

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
        combatManager = CombatManager.Instance;     
    }

    public void Initialize()
    {
        Awake();
    }

    public void initializeTurns(List<Character> characters)
    {
        currentTurnNumber = 0;

        internalTurns.Clear();
        turnsList.Clear();

        createInternalTurns(characters);                 

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < internalTurns.Count; j++)
            {
                TurnsManager.Instance.turnsList.Add(internalTurns[j]);
                internalTurnCounter = j;
                turnNumberIsOverList.Add(false);
                if (TurnsManager.Instance.turnsList.Count >= turnsToshow) break;
            }

            if (TurnsManager.Instance.turnsList.Count >= turnsToshow) break;
        } 
    }

    public void nextTurn(int currentTurn)
    {
        currentTurnNumber = currentTurn + 1;
        calculateFollowingTurns(CombatManager.Instance.characters);
    }

    public void calculateFollowingTurns(List<Character> characters)
    {
        if (characters.Any(character => character.isAlive && character.isAlly))
        {
            do
            {
                if (internalTurnCounter < internalTurns.Count - 1)
                {
                    internalTurnCounter++;
                }
                else
                {
                    internalTurnCounter = 0;
                }
            }
            while (!internalTurns[internalTurnCounter].isAlive);
        }
        else return;

        TurnsManager.Instance.turnsList.Add(internalTurns[internalTurnCounter]);      

        turnNumberIsOverList.Add(false);
    }

    private void createInternalTurns(List<Character> characters) 
    {
        List<float> rapidities = new List<float>();
        float rapiditiesTotal = 0;
        List<int> turnsNumber = new List<int>();

        internalTurns.Clear();

        characters = characters.OrderByDescending(c => c.rapidity).ToList();
        foreach (Character character in characters)
        {
            rapidities.Add(character.rapidity);
        }

        rapiditiesTotal = rapidities.Sum();

        foreach (int rapidity in rapidities)
        {
            //turnsNumber.Add((int) (rapidity / rapiditiesTotal * 10));
            turnsNumber.Add(Mathf.Max((int) (rapidity / rapiditiesTotal * 10), 1));
        }

        if (characters.Any(character => character.isAlive && character.isAlly))
        {
            while (turnsNumber.Sum() > 0)
            {
                for (int i = 0; i < turnsNumber.Count; i++)
                {
                    if (turnsNumber[i] > 0)
                    {
                        internalTurns.Add(characters[i]);
                        turnsNumber[i]--;
                    }
                }
            }
        }
        else return;          
           
    }

    public void removeDeadCharactersTurns(List<Character> deadCharacters)
    {
        int index = -1;

        turnsList.RemoveAll(character =>
        {
            index++;
            return index >= currentTurnNumber && deadCharacters.Contains(character);
        });

        foreach(Character character in deadCharacters)
        {
            internalTurns.Remove(character);
        }
    }
}
