using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Setting")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float walkSpeed = 1;
    private float xAxis;

    [Header("Ground Check Setting")]
    [SerializeField] private float jumpForce = 45;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        GetInput();
        Move();
        Jump();
    }

    void GetInput()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
    }
    private void Move()
    {
        rb.linearVelocity = new Vector2(walkSpeed * xAxis, rb.linearVelocity.y);
    }

    public bool Grounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround) || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround) || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckY, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
            return true;
        }
        else
        {
            return false;   
        }
    }

    void Jump()
    {
        if(Input.GetButtonDown("Jump") && Grounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);
        }
    }
}

