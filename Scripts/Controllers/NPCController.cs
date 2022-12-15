using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NPCState
{
    idle,
    walk,
    battle,
    still
}

public class NPCController : MonoBehaviour
{
    public string Name;
    private Rigidbody2D myRigidbody;
    private Animator animator;
    public List<Transform> targets;
    public float moveSpeed;
    public NPCState currentState;
    int currentTarget = 0;

    // Start is called before the first frame update
    void Start()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        animator.SetFloat("moveX", 0);
        animator.SetFloat("moveY", 0);
    }

    void FixedUpdate()
    {   if (currentState == NPCState.walk || currentState == NPCState.idle)
        {
            if (targets[currentTarget].transform.position.x - myRigidbody.transform.position.x > 0)
            {
                animator.SetFloat("moveX", 1);
            }
            else if (targets[currentTarget].transform.position.x - myRigidbody.transform.position.x < 0)
            {
                animator.SetFloat("moveX", -1);
            }
            else if (targets[currentTarget].transform.position.x - myRigidbody.transform.position.x == 0)
            {
                animator.SetFloat("moveX", 0);
            }

            if (targets[currentTarget].transform.position.y - myRigidbody.transform.position.y > 0)
            {
                animator.SetFloat("moveY", 1);
            }
            else if (targets[currentTarget].transform.position.y - myRigidbody.transform.position.y < 0)
            {
                animator.SetFloat("moveY", -1);
            }
            else if (targets[currentTarget].transform.position.y - myRigidbody.transform.position.y == 0)
            {
                animator.SetFloat("moveY", 0);
            }

            CheckDistance();
            if (currentState == NPCState.walk)
            {
                animator.SetBool("moving", true);
            }
        }
        else
        {
            animator.SetBool("moving", false);
        }
    }

    void CheckDistance()
    {
        if (currentState != NPCState.battle)
        {
            Vector3 temp = Vector3.MoveTowards(transform.position, targets[currentTarget].position, moveSpeed * Time.deltaTime);
            myRigidbody.MovePosition(temp);
            ChangeState(NPCState.walk);
            if(myRigidbody.transform.position == targets[currentTarget].transform.position && targets.Count - 1 > currentTarget)
            {
                currentTarget++;
            }
            else if(myRigidbody.transform.position == targets[currentTarget].transform.position && targets.Count - 1 == currentTarget)
            {
                currentTarget--;
            }
        }
    }

    private void ChangeState(NPCState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ChangeState(NPCState.still);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ChangeState(NPCState.idle);
        }
    }
}
