using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Character : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private Vector2 knockbackVelocity;
    private float jumpHoldTime;
    private CapsuleCollider2D capsuleCollider;
    private BoxCollider2D boxCollider;
    private List<int> activeZoneNumbers = new List<int>();

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool isKnockedBack = false;
    private float groundCheckDistance = 0.2f;
    [SerializeField] private bool isGrounded;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private int life;
    [SerializeField] private float knockbackForce; private bool jumpingStarted;
    [SerializeField] private float maxJumpHoldTime = 0.2f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    //[SerializeField] private float fallMultiplier = 2.5f;
    //[SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] public MainCamera mainCamera;
    [SerializeField] public bool inSpecialZone = false;
    [SerializeField] public bool inCenteredZone = false;
    private int jumpCount;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] public GameObject officeCameraYLevel;
    [SerializeField] public GameObject hellCameraYLevel;
    [SerializeField] public GameObject specialZone1CameraYLevel;
    private Animator animator;
    public float cameraYLevel;
    


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        CalculateCameraYLevel();
        CheckIfGrounded();

        if (!isKnockedBack)
        {
            Move();
        }

        if (isGrounded || jumpCount < maxJumpCount)
        {
            Jump();
        }

        if (!isGrounded)
        {
            animator.SetBool("walking", false);
            if(rb.velocity.y > 0)
            {
                animator.SetBool("jumping", true);
                animator.SetBool("falling", false);
            }          
            else
            {
                animator.SetBool("jumping", false);
                animator.SetBool("falling", true);
            }
        }
    }


    private void Move()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        Vector2 playerVelocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        rb.velocity = playerVelocity;

        if (moveInput > 0 || moveInput < 0)
        {
            animator.SetBool("walking", true);

            if(moveInput > 0)
            {
                spriteRenderer.flipX = false;        
            }
            else
            {
                spriteRenderer.flipX = true;
            }         
        }
        else
        {       
            animator.SetBool("walking", false);
        }
    }

    private void Jump()
    {
        if (jumpCount == 0 && !isGrounded)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            StartJump();
        }

        if (Input.GetKey(KeyCode.Space) && jumpingStarted)
        {
            HoldJump();
        }

        if (Input.GetKeyUp(KeyCode.Space) && jumpingStarted || jumpHoldTime >= maxJumpHoldTime && jumpingStarted)
        {
            jumpingStarted = false;
        }
    }

    private void StartJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        jumpingStarted = true;
        jumpHoldTime = 0f;
        jumpCount++;
    }

    private void HoldJump()
    {
        jumpHoldTime += Time.deltaTime;
      
        /*
        if (jumpHoldTime >= maxJumpHoldTime)
        {
            jumpingStarted = false;
            animator.SetBool("jumping", false);
        }
        */
    }

    private IEnumerator ResetKnockback(float duration)
    {
        yield return new WaitForSeconds(duration);
        isKnockedBack = false;
        knockbackVelocity = Vector2.zero;
    }

    private void CheckIfGrounded()
    {     
        Vector2 bottomLeft = capsuleCollider.bounds.min;
        Vector2 bottomRight = new Vector2(capsuleCollider.bounds.max.x, capsuleCollider.bounds.min.y);



        RaycastHit2D hitLeft = Physics2D.Raycast(bottomLeft, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(bottomRight, Vector2.down, groundCheckDistance, groundLayer);

        Debug.DrawRay(bottomLeft, Vector2.down * groundCheckDistance, Color.red);
        Debug.DrawRay(bottomRight, Vector2.down * groundCheckDistance, Color.red);

        if (hitLeft.collider != null || hitRight.collider != null)
        {
            isGrounded = true;   
        }
        else
        {
            isGrounded = false;
        }
    }

    private void CalculateCameraYLevel()
    {            
        if(Mathf.Abs(transform.position.y - officeCameraYLevel.transform.position.y) <= 10)
        {
            cameraYLevel = officeCameraYLevel.transform.position.y;
        }
        if(Mathf.Abs(transform.position.y - hellCameraYLevel.transform.position.y) <= 10)
        {
            cameraYLevel = hellCameraYLevel.transform.position.y;
        }
    }

    private void OnTriggerEnter2D(Collider2D trigger)
    {  
        SpecialZone specialZone = trigger.GetComponent<SpecialZone>();

        if(specialZone != null)
        {
            if (trigger.CompareTag("Special Zone Enter") && activeZoneNumbers.Count == 0)
            {
                inSpecialZone = true;
                activeZoneNumbers.Add(specialZone.zoneNumber);
                mainCamera.translate_Y(specialZone1CameraYLevel.transform.position.y);

                Debug.Log("Entering Zone: " + specialZone.zoneNumber);
            }

            if (trigger.CompareTag("Special Zone Enter") && activeZoneNumbers.Count > 0 && !activeZoneNumbers.Contains(specialZone.zoneNumber))
            {
                mainCamera.translate_Y(specialZone1CameraYLevel.transform.position.y);
                activeZoneNumbers.Add(specialZone.zoneNumber);
                Debug.Log("Entering Zone: " + specialZone.zoneNumber);
            }

            if (trigger.CompareTag("Special Zone Exit") && activeZoneNumbers.Contains(specialZone.zoneNumber))
            {
                inSpecialZone = false;
                mainCamera.translate_Y(officeCameraYLevel.transform.position.y);
                activeZoneNumbers.Remove(specialZone.zoneNumber);
                Debug.Log("Exiting Zone: " + specialZone.zoneNumber);
                
                mainCamera.last_Y_translation = 0f;
            }
        }

        if (trigger.CompareTag("Special Zone Centered Enter"))
        {         
            inSpecialZone = false;
            inCenteredZone = true;
        }

        if (trigger.CompareTag("Special Zone Centered Exit"))
        {              
            inCenteredZone = false;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Enemy>())
        {
            Collider2D enemyCollider = collision.gameObject.GetComponent<Collider2D>();
            float enemyTop = collision.transform.position.y + (enemyCollider.bounds.size.y / 2);
            float playerBottom = transform.position.y - (GetComponent<Collider2D>().bounds.size.y / 2);

            bool isTopHit = false;

            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.point.y > enemyTop - 0.3f && contact.point.y < playerBottom + 0.1f)
                {
                    isTopHit = true;
                    break;
                }
            }

            if (isTopHit)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpCount = 1;

                collision.gameObject.GetComponent<Enemy>().KillEnemy();
            }
            else
            {
                life--;
                Vector2 direction = (transform.position - collision.transform.position).normalized;
                rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);

                isKnockedBack = true;
                knockbackVelocity = direction * knockbackForce;

                StartCoroutine(ResetKnockback(0.5f));
            }
        }

        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            animator.SetBool("falling", false);
            jumpCount = 0;
        }
}

}