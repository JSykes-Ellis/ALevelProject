using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    walk,
    attack,
    interact
}

public class PlayerController : MonoBehaviour
{
    public Sprite sprite;
    public string name;

    public PlayerState currentState;
    public float speed;
    private Rigidbody2D myRigidbody;
    private Vector3 change;
    private Animator animator;

    public GameObject gameController;

    public event Action OnEncountered;
    public event Action<Collider2D> OnEnteredTrainersView;

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("CheckForRandomEncounters", 1f, 0.3f);

        currentState = PlayerState.walk;
        myRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        animator.SetFloat("moveX", 0);
        animator.SetFloat("moveY", -1);
    }

    // Update is called once per frame
    private void Update()
    {
        if(gameController.GetComponent<GameController>().state != GameState.FreeRoam)
        {
            animator.SetBool("moving", false);
        }
    }

    public void HandleFixedUpdate()
    {
        change = Vector2.zero;
        change.x = Input.GetAxisRaw("Horizontal");
        change.y = Input.GetAxisRaw("Vertical");
        if (currentState == PlayerState.walk)
        {
            UpdateAnimationAndMove();
        }
    }

    public void HandleUpdate()
    {
        if (Input.GetButtonDown("attack") && currentState != PlayerState.attack)
        {
            StartCoroutine(Attack());
        }
    }

    private IEnumerator Attack()
    {
        animator.SetBool("attacking", true);
        currentState = PlayerState.attack;
        yield return null;
        animator.SetBool("attacking", false);
        yield return new WaitForSeconds(.33f);
        currentState = PlayerState.walk;
    }

    void UpdateAnimationAndMove()
    {
        if (change != Vector3.zero)
        {
            MoveCharacter();
            animator.SetFloat("moveX", change.x);
            animator.SetFloat("moveY", change.y);
            animator.SetBool("moving", true);
        }
        else
        {
            animator.SetBool("moving", false);
        }
    }

    void MoveCharacter()
    {
        change.Normalize();
        myRigidbody.MovePosition(transform.position + change * speed * Time.deltaTime);
        CheckIfInTrainersView();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            OnEncountered();
            other.gameObject.SetActive(false);
        }
    }

    private void CheckIfInTrainersView()
    {
        var collider = Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.fovLayer);
        if (collider != null)
        {
            Debug.Log("In view");
            OnEnteredTrainersView?.Invoke(collider);
        }
    }
    private void CheckForRandomEncounters()
    {
        if(Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.WildLayer) != null && gameController.GetComponent<GameController>().state != GameState.Battle && animator.GetBool("moving") == true)
        {
            if (UnityEngine.Random.Range(1,101) <= 10)
            {
                OnEncountered();
            }
        }
    }
}
