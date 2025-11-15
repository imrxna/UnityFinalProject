using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[Header("Horizontal Movement Settings:")]
	[SerializeField] private float walkSpeed = 1; 
	[Header("Vertical Movement Settings")]
	[SerializeField] private float jumpForce = 45f; 
	private float jumpBufferCounter = 0; 
	[SerializeField] private float jumpBufferFrames; 
	private float coyoteTimeCounter = 0; 
	[SerializeField] private float coyoteTime; 

	private int airJumpCounter = 0; 
	[SerializeField] private int maxAirJumps;

	private float gravity; 
	
	[Header("Ground Check Settings:")]
	[SerializeField] private Transform groundCheckPoint; 
	[SerializeField] private float groundCheckY = 0.2f; 
	[SerializeField] private float groundCheckX = 0.5f; 
	[SerializeField] private LayerMask whatIsGround; 
	

	[Header("Dash Settings")]
	[SerializeField] private float dashSpeed; 
	[SerializeField] private float dashTime; 
	[SerializeField] private float dashCooldown; 
	[SerializeField] GameObject dashEffect;
	private bool canDash = true, dashed;

	[Header("Attack Settings:")]
	[SerializeField] private Transform SideAttackTransform; 
	[SerializeField] private Vector2 SideAttackArea; 

	[SerializeField] private Transform UpAttackTransform; 
	[SerializeField] private Vector2 UpAttackArea; 

	[SerializeField] private Transform DownAttackTransform; 
	[SerializeField] private Vector2 DownAttackArea; 

	[SerializeField] private LayerMask attackableLayer; 

	[SerializeField] private float timeBetweenAttack;
	private float timeSinceAttack;

	[SerializeField] private float damage; 

	[SerializeField] private GameObject slashEffect; 

	bool restoreTime;
	float restoreTimeSpeed;

	[Header("Recoil Settings:")]
	[SerializeField] private int recoilXSteps = 5; 
	[SerializeField] private int recoilYSteps = 5; 

	[SerializeField] private float recoilXSpeed = 100; 
	[SerializeField] private float recoilYSpeed = 100;

	private int stepsXRecoiled, stepsYRecoiled; 
	
	[Header("Health Settings")]
	public int health;
	public int maxHealth;
	[SerializeField] GameObject bloodSpurt;
	[SerializeField] float hitFlashSpeed;
	public delegate void OnHealthChangedDelegate();
	[HideInInspector] public OnHealthChangedDelegate onHealthChangedCallback;

	float healTimer;
	[SerializeField] float timeToHeal;

	[Header("Mana Settings")]
	[SerializeField] UnityEngine.UI.Image manaStorage;

	[SerializeField] float mana;
	[SerializeField] float manaDrainSpeed;
	[SerializeField] float manaGain;

	[Header("Spell Settings")]
	[SerializeField] float manaSpellCost = 0.3f;
	[SerializeField] float timeBetweenCast = 0.5f;
	float timeSinceCast;
	[SerializeField] float spellDamage; 
	[SerializeField] float downSpellForce; 
										   
	[SerializeField] GameObject sideSpellFireball;
	[SerializeField] GameObject upSpellExplosion;
	[SerializeField] GameObject downSpellFireball;
	
	[HideInInspector] public PlayerStateList pState;
	private Animator anim;
	private Rigidbody2D rb;
	private SpriteRenderer sr;

	private float xAxis, yAxis;
	private bool attack = false;


	public static PlayerController Instance;

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
		Health = maxHealth;
	}


	// Start is called before the first frame update
	void Start()
	{
		pState = GetComponent<PlayerStateList>();

		rb = GetComponent<Rigidbody2D>();
		sr = GetComponent<SpriteRenderer>();

		anim = GetComponent<Animator>();

		gravity = rb.gravityScale;

		Mana = mana;
		//manaStorage.fillAmount = Mana;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
		Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
		Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);
	}

	// Update is called once per frame
	void Update()
	{
		GetInputs();
		UpdateJumpVariables();

		if (pState.dashing) return;
		RestoreTimeScale();
		FlashWhileInvincible();
		Move();
		Heal();
		CastSpell();
		if (pState.healing) return;
		Flip();
		Jump();
		StartDash();
		Attack();
	}
	private void OnTriggerEnter2D(Collider2D _other) 
	{
		if (_other.GetComponent<Enemy>() != null && pState.casting)
		{
			_other.GetComponent<Enemy>().EnemyHit(spellDamage, (_other.transform.position - transform.position).normalized, -recoilYSpeed);
		}
	}

	private void FixedUpdate()
	{
		if (pState.dashing || pState.healing) return;
		Recoil();
	}

	void GetInputs()
	{
		xAxis = Input.GetAxisRaw("Horizontal");
		yAxis = Input.GetAxisRaw("Vertical");
		attack = Input.GetButtonDown("Attack");
	}

	void Flip()
	{
		if (xAxis < 0)
		{
			transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
			pState.lookingRight = false;
		}
		else if (xAxis > 0)
		{
			transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
			pState.lookingRight = true;
		}
	}

	private void Move()
	{
		if (pState.healing) rb.linearVelocity = new Vector2(0, 0);
		rb.linearVelocity = new Vector2(walkSpeed * xAxis, rb.linearVelocity.y);
		anim.SetBool("Walking", rb.linearVelocity.x != 0 && Grounded());
	}

	void StartDash()
	{
		if (Input.GetButtonDown("Dash") && canDash && !dashed)
		{
			StartCoroutine(Dash());
			dashed = true;
		}

		if (Grounded())
		{
			dashed = false;
		}
	}

	IEnumerator Dash()
	{
		canDash = false;
		pState.dashing = true;
		anim.SetTrigger("Dashing");
		rb.gravityScale = 0;
		rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0);
		if (Grounded()) Instantiate(dashEffect, transform);
		yield return new WaitForSeconds(dashTime);
		rb.gravityScale = gravity;
		pState.dashing = false;
		yield return new WaitForSeconds(dashCooldown);
		canDash = true;

	}

	void Attack()
	{
		timeSinceAttack += Time.deltaTime;

		if (attack && timeSinceAttack >= timeBetweenAttack)
		{
			timeSinceAttack = 0;
			anim.SetTrigger("Attacking");
			if ((yAxis == 0 || yAxis < 0) && Grounded())
			{
				Hit(SideAttackTransform, SideAttackArea, ref pState.recoilingX, recoilXSpeed);
				Instantiate(slashEffect, SideAttackTransform);
			}

			else if (yAxis > 0)
			{
				Hit(UpAttackTransform, UpAttackArea, ref pState.recoilingY, recoilYSpeed);
				SlashEffectAtAngle(slashEffect, 80, UpAttackTransform);
			}

			else if (yAxis < 0 && !Grounded())
			{
				Hit(DownAttackTransform, DownAttackArea, ref pState.recoilingY, recoilYSpeed);
				SlashEffectAtAngle(slashEffect, -90, DownAttackTransform);
			}
		}
	}

	void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float _recoilStrength)
	{
		Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);
		List<Enemy> hitEnemies = new List<Enemy>();

		if (objectsToHit.Length > 0)
		{
			_recoilDir = true;
		}

		for (int i = 0; i < objectsToHit.Length; i++)
		{
			Enemy e = objectsToHit[i].GetComponent<Enemy>();
			if (e && !hitEnemies.Contains(e))
			{
				e.EnemyHit(damage, (transform.position - objectsToHit[i].transform.position).normalized, _recoilStrength);
				hitEnemies.Add(e);

				if (objectsToHit[i].CompareTag("Enemy"))
				{
					Mana += manaGain;
				}
			}
		}
	}
	void SlashEffectAtAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
	{
		_slashEffect = Instantiate(_slashEffect, _attackTransform);
		_slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
		_slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
	}
	void Recoil()
	{
		if (pState.recoilingX)
		{
			if (pState.lookingRight)
			{
				rb.linearVelocity = new Vector2(-recoilXSpeed, 0);
			}
			else
			{
				rb.linearVelocity = new Vector2(recoilXSpeed, 0);
			}
		}

		if (pState.recoilingY)
		{
			rb.gravityScale = 0;

			if (yAxis < 0)
			{

				rb.linearVelocity = new Vector2(rb.linearVelocity.x, recoilYSpeed);
			}
			else
			{
				rb.linearVelocity = new Vector2(rb.linearVelocity.x, -recoilYSpeed);
			}
			airJumpCounter = 0;
		}
		else
		{
			rb.gravityScale = gravity;
		}

		//stop recoil
		if (pState.recoilingX && stepsXRecoiled < recoilXSteps)
		{
			stepsXRecoiled++;
		}
		else
		{
			StopRecoilX();
		}
		if (pState.recoilingY && stepsYRecoiled < recoilYSteps)
		{
			stepsYRecoiled++;
		}
		else
		{
			StopRecoilY();
		}

		if (Grounded())
		{
			StopRecoilY();
		}
	}
	void StopRecoilX()
	{
		stepsXRecoiled = 0;
		pState.recoilingX = false;
	}
	void StopRecoilY()
	{
		stepsYRecoiled = 0;
		pState.recoilingY = false;
	}
	public void TakeDamage(float _damage)
	{
		Health -= Mathf.RoundToInt(_damage);
		StartCoroutine(StopTakingDamage());
	}
	IEnumerator StopTakingDamage()
	{
		pState.invincible = true;
		GameObject _bloodSpurtParticles = Instantiate(bloodSpurt, transform.position, Quaternion.identity);
		Destroy(_bloodSpurtParticles, 1.5f);
		anim.SetTrigger("TakeDamage");
		yield return new WaitForSeconds(1f);
		pState.invincible = false;
	}
	void FlashWhileInvincible()
	{
		sr.material.color = pState.invincible ? Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * hitFlashSpeed, 1.0f)) : Color.white;
	}
	void RestoreTimeScale()
	{
		if (restoreTime)
		{
			if (Time.timeScale < 1)
			{
				Time.timeScale += Time.unscaledDeltaTime * restoreTimeSpeed;
			}
			else
			{
				Time.timeScale = 1;
				restoreTime = false;
			}
		}
	}

	public void HitStopTime(float _newTimeScale, int _restoreSpeed, float _delay)
	{
		restoreTimeSpeed = _restoreSpeed;
		if (_delay > 0)
		{
			StopCoroutine(StartTimeAgain(_delay));
			StartCoroutine(StartTimeAgain(_delay));
		}
		else
		{
			restoreTime = true;
		}
		Time.timeScale = _newTimeScale;
	}
	IEnumerator StartTimeAgain(float _delay)
	{
		yield return new WaitForSecondsRealtime(_delay);
		restoreTime = true;
	}

	public int Health
	{
		get { return health; }
		set
		{
			if (health != value)
			{
				health = Mathf.Clamp(value, 0, maxHealth);

				if (onHealthChangedCallback != null)
				{
					onHealthChangedCallback.Invoke();
				}
			}
		}
	}
	void Heal()
	{
		if (Input.GetButton("Healing") && Health < maxHealth && Mana > 0 && Grounded() && !pState.dashing)
		{
			pState.healing = true;
			anim.SetBool("Healing", true);

			//healing
			healTimer += Time.deltaTime;
			if (healTimer >= timeToHeal)
			{
				Health++;
				healTimer = 0;
			}

			//drain mana
			Mana -= Time.deltaTime * manaDrainSpeed;
		}
		else
		{
			pState.healing = false;
			anim.SetBool("Healing", false);
			healTimer = 0;
		}
	}
	float Mana
	{
		get { return mana; }
		set
		{
			//if mana stats change
			if (mana != value)
			{
				mana = Mathf.Clamp(value, 0, 1);
				manaStorage.fillAmount = Mana;
			}
		}
	}

	void CastSpell()
	{
		if (Input.GetButtonDown("CastSpell") && timeSinceCast >= timeBetweenCast && Mana >= manaSpellCost)
		{
			pState.casting = true;
			timeSinceCast = 0;
			StartCoroutine(CastCoroutine());
		}
		else
		{
			timeSinceCast += Time.deltaTime;
		}

		if (Grounded())
		{
			downSpellFireball.SetActive(false);
		}
		if (downSpellFireball.activeInHierarchy)
		{
			rb.linearVelocity += downSpellForce * Vector2.down;
		}
	}
	IEnumerator CastCoroutine()
	{
		anim.SetBool("Casting", true);
		yield return new WaitForSeconds(0.15f);

		//side cast
		if (yAxis == 0 || (yAxis < 0 && Grounded()))
		{
			GameObject _fireBall = Instantiate(sideSpellFireball, SideAttackTransform.position, Quaternion.identity);

			if (pState.lookingRight)
			{
				_fireBall.transform.eulerAngles = Vector3.zero;
			}
			else
			{
				_fireBall.transform.eulerAngles = new Vector2(_fireBall.transform.eulerAngles.x, 180);

			}
			pState.recoilingX = true;
		}

		//up cast
		else if (yAxis > 0)
		{
			Instantiate(upSpellExplosion, transform);
			rb.linearVelocity = Vector2.zero;
		}

		//down cast
		else if (yAxis < 0 && !Grounded())
		{
			downSpellFireball.SetActive(true);
		}

		Mana -= manaSpellCost;
		yield return new WaitForSeconds(0.35f);
		anim.SetBool("Casting", false);
		pState.casting = false;
	}

	public bool Grounded()
	{
		if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
			|| Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
			|| Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
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
		if (!pState.jumping)
		{
			if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
			{
				rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);

				pState.jumping = true;
			}
			else if (!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump"))
			{
				pState.jumping = true;

				airJumpCounter++;

				rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);
			}
		}

		if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
		{
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

			pState.jumping = false;
		}

		anim.SetBool("Jumping", !Grounded());
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
			jumpBufferCounter = jumpBufferCounter - Time.deltaTime * 10;
		}
	}
}


