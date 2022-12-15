using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public GameObject dialogueBox;
    public Text dialogueText;
    public List<string> dialogueLines;
    public bool showDialogue;
    public GameController gameController;
    int currentLine = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("interact") && showDialogue)
        {
            if (dialogueBox.activeInHierarchy && currentLine == dialogueLines.Count)
            {
                HideDialogue();
                currentLine = 0;
            }
            else
            {
                StartCoroutine(ShowDialogue());
            }
        }
    }

    public IEnumerator ShowDialogue()
    {
        yield return new WaitForEndOfFrame();

        dialogueBox.SetActive(true);
        gameController.state = GameState.Dialogue;
        dialogueText.text = dialogueLines[currentLine];
        if (currentLine < dialogueLines.Count)
        {
            currentLine++;
        }
    }

    public void HideDialogue()
    {
        dialogueBox.SetActive(false);
        gameController.state = GameState.FreeRoam;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            showDialogue = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            showDialogue = false;
            dialogueBox.SetActive(false);
        }
    }

}
