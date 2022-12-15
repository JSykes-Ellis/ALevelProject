using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomChange : MonoBehaviour
{
    public Vector2 cameraDelta;
    public Vector3 playerDelta;
    private CameraController cam;
    public bool wantText;
    public string placeName;
    public GameObject text;
    public Text placeText;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main.GetComponent<CameraController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            cam.minPosition += cameraDelta;
            cam.maxPosition += cameraDelta;
            other.transform.position += playerDelta;
            if (wantText)
            {
                StartCoroutine(SetPlaceName());
            }
        }
    }

    private IEnumerator SetPlaceName()
    {
        text.SetActive(true);
        placeText.text = placeName;
        yield return new WaitForSeconds(4f);
        text.SetActive(false);
    }

}
