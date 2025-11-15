using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Setting")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float walkSpeed = 1;
    private float xAxis, yAxis;

	[Header("Vertical Movement Setting")]
	[SerializeField] private float jumpForce = 45;
    private int jumpBufferCounter;
    [SerializeField] private int jumpBufferFrames;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0 ;
    [SerializeField] private int maxAirJumps;

    [Header("Ground Check Setting")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;

    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;

    public static PlayerController Instance;
    PlayerStateList pState;

	[Header("Dash Setting")]
	private bool canDash = true;
    private bool dashed;
    private float gravity;

    [Header("Attacking")]
	bool attack = false;
    float timeBetweenAttack, timeSinceAttack;
    [SerializeField] Transform SideAttackTransform, UpAttackTransform, DownAttackTransform;
    [SerializeField] Vector2 SideAttackArea, UpAttackArea, DownAttackArea;
    [SerializeField] LayerMask attackableLayer;
    [SerializeField] float damage;



	private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pState = GetComponent<PlayerStateList>();
        rb = GetComponent<Rigidbody2D>();
        gravity = rb.gravityScale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawCube(UpAttackTransform.position, UpAttackArea);
        Gizmos.DrawCube(DownAttackTransform.position, DownAttackArea);
    }

    // Update is called once per frame
    void Update()
    {
        GetInput();
        Move();
        Jump();
        Flip();
        UpdateJumpVariables();
        if(pState.dashing) return;
        StartDash();
        Attack();
    }

    void GetInput()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetMouseButtonDown(0);
    }

    void Flip()
    {
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-1, transform.localScale.y);
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
        }
    }
    private void Move()
    {
        rb.linearVelocity = new Vector2(walkSpeed * xAxis, rb.linearVelocity.y);
    }

    void StartDash()
    {
        if(Input.GetButtonDown("Dash") && canDash && !dashed)
        {
            StartCoroutine(Dash());
            dashed = true;
        }

        if(Grounded())
        {
            dashed = false;
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        pState.dashing = true;
        //anim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;

    }

    void Attack()
    {
        timeSinceAttack += Time.deltaTime;
        if(attack && timeSinceAttack >= timeBetweenAttack)
        {
            timeSinceAttack = 0;

			if ((yAxis == 0 || yAxis < 0) && Grounded())
			{
                Hit(SideAttackTransform, SideAttackArea);
			}
            else if(yAxis > 0)
            {
				Hit(UpAttackTransform, UpAttackArea);
			}
            else if(yAxis < 0 && !Grounded())
            {
				Hit(DownAttackTransform, DownAttackArea);
			}


		}
	}

	private void Hit(Transform _attackTransform, Vector2 _attackArea)
	{
		Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(
			_attackTransform.position,
			_attackArea,
			0,
			attackableLayer
		);

		if (objectsToHit.Length > 0)
		{
			Debug.Log("Hit");
		}

		for (int i = 0; i < objectsToHit.Length; i++)
		{
			Enermy enemy = objectsToHit[i].GetComponent<Enermy>();
			if (enemy != null)
			{
				enemy.EnermyHit(
					damage,
					(transform.position - objectsToHit[i].transform.position).normalized,
					100
				);
			}
		}
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
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

            pState.jumping = false;
        }

        if (!pState.jumping)
        {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);
                
                pState.jumping = true;
            }
            else if(!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump"))
            {
                pState.jumping = true;
                airJumpCounter++;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);
            }
        }
    }

    void UpdateJumpVariables()
    {
        if (Grounded())
        { 
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;

        }
        else 
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter--;
        }
    }
}

