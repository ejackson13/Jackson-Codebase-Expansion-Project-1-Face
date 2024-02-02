using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public float runSpeed = 20f;
    public float sprintSpeed = 5;

    public GameObject flashLight;

    public Vector3[] flashlightRotations; // 0 = down , 1 = left, 2 = top, 3 = right
    public Vector3[] flashlightPositions;

    public bool pickedUpExitKey = false;

    public Sprite defaultSprite;

    private float horizontal;
    private float vertical;
    private float moveLimiter = 0.7f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = gameObject.transform.GetChild(3).transform.gameObject.GetComponent<SpriteRenderer>();
        anim = gameObject.transform.GetChild(3).transform.gameObject.GetComponent<Animator>();
        anim.enabled = false;
        sr.sprite = defaultSprite;
        flashLight.transform.localPosition = flashlightPositions[3];
    }

    // Update is called once per frame
    void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        
        if (vertical < -.5f || vertical > .5f || horizontal < -.5f || horizontal > .5f)
        {
            anim.SetBool("Vertical", true);
            anim.enabled = true;
        }
        else
        {
            anim.enabled = false;
        }
        

        // get vector from center of screen to mouse position
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 centerScreen = Camera.main.ViewportToWorldPoint(new Vector3(.5f, .5f, 0));
        Vector2 diff = centerScreen - mousePos;

        // calculate angle of vector
        float angleRad = Mathf.Atan2(diff.y, diff.x);
        float angleDeg = angleRad * Mathf.Rad2Deg;
        //Debug.Log(angleDeg);

        // rotate character and flashlight to match the angle (have the character point at the mouse)
        flashLight.transform.eulerAngles = new Vector3(0, 0, angleDeg + 90);
        sr.gameObject.transform.eulerAngles = new Vector3(0, 0, angleDeg - 90);

        // adjust position of light source (so it looks like it is coming out of the flashlight)
        // The formula comes from https://math.stackexchange.com/questions/260096/find-the-coordinates-of-a-point-on-a-circle because I never would have figured it out on my own
        float ellipseX = -.225f * Mathf.Cos(angleRad + (30*Mathf.Deg2Rad));
        float ellipseY = -.225f * Mathf.Sin(angleRad + (30 * Mathf.Deg2Rad));
        flashLight.transform.localPosition = new Vector2(ellipseX, ellipseY);
        

    }

    private void FixedUpdate()
    {
        if (horizontal != 0 && vertical != 0)
        {
            horizontal *= moveLimiter;
            vertical *= moveLimiter;
        }

        if (Input.GetButton("Sprint"))
        {
            rb.velocity = new Vector2(horizontal * sprintSpeed, vertical * sprintSpeed);
        }
        else
        {
            rb.velocity = new Vector2(horizontal * runSpeed, vertical * runSpeed);
        }        
    }
}
