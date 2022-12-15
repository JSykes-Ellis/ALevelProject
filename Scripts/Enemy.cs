using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    idle,
    walk,
    battle
}

public class Enemy : MonoBehaviour
{
    public string enemyName;
    public float moveSpeed;
    public EnemyState currentState;
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
