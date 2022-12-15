using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrainerController : MonoBehaviour
{
    public Sprite sprite;
    public string Name;

    public GameObject exclamation;
    private Rigidbody2D myRigidbody;
    public int moveSpeed;

    public GameObject dialogueBox;
    public Text dialogueText;
    public string dialogueLines;
    public GameObject gameController;
    public Transform target;
    private Animator animator;
    bool walk;
    public GameObject fov;

    public bool requireX;
    public bool requireY;

    private void Start()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (walk)
        {
            if (requireX)
            {
                if (target.transform.position.x - myRigidbody.transform.position.x > 0)
                {
                    animator.SetFloat("moveX", 1);
                }
                else if (target.transform.position.x - myRigidbody.transform.position.x < 0)
                {
                    animator.SetFloat("moveX", -1);
                }
                else if (target.transform.position.x - myRigidbody.transform.position.x == 0)
                {
                    animator.SetFloat("moveX", 0);
                }
            }

            if (requireY)
            {
                if (target.transform.position.y - myRigidbody.transform.position.y > 0)
                {
                    animator.SetFloat("moveY", 1);
                }
                else if (target.transform.position.y - myRigidbody.transform.position.y < 0)
                {
                    animator.SetFloat("moveY", -1);
                }
                else if (target.transform.position.y - myRigidbody.transform.position.y == 0)
                {
                    animator.SetFloat("moveY", 0);
                }
            }

            if (Vector3.Distance(target.position, transform.position) > 1)
            {
                CheckDistance(target);
                animator.SetBool("moving", true);
            }
        }
    }

    public void BattleLost()
    {
        fov.SetActive(false);
    }

    public IEnumerator TiggerTrainerBattle(PlayerController player)
    {
        gameController.GetComponent<GameController>().state = GameState.Dialogue;
        exclamation.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        exclamation.SetActive(false);

        walk = true;
        yield return WaitForArrival();
        animator.SetBool("moving", false);
        walk = false;
        StartCoroutine(ShowDialogue());
        yield return waitForKeyPress(KeyCode.Z);
        HideDialogue();
        GameController.Instance.StartTrainerBattle(this);
    }

    void CheckDistance(Transform target)
    {     
        Vector3 temp = Vector3.MoveTowards(transform.position, target.transform.position, moveSpeed * Time.deltaTime);
        myRigidbody.MovePosition(temp);
    }

    public IEnumerator ShowDialogue()
    {
        yield return new WaitForEndOfFrame();

        dialogueBox.SetActive(true);
        dialogueText.text = dialogueLines;
    }

    public void HideDialogue()
    {
        dialogueBox.SetActive(false);
        gameController.GetComponent<GameController>().state = GameState.FreeRoam;
    }

    private IEnumerator waitForKeyPress(KeyCode key)
    {
        bool done = false;
        while (!done) // essentially a "while true", but with a bool to break out naturally
        {
            if (Input.GetKeyDown(key))
            {
                done = true; // breaks the loop
            }
            yield return null; // wait until next frame, then continue execution from here (loop continues)
        }
    }

    private IEnumerator WaitForArrival()
    {
        bool arrived = false;
        while (!arrived)
        {
            if(Vector3.Distance(target.position, transform.position) <= 1)
            {
                arrived = true;
            }
            yield return null;
        }
    }
}
