using UnityEngine;

public class Enermy : MonoBehaviour
{
    [SerializeField] protected float health;
    [SerializeField] protected float recoillength;
    [SerializeField] protected float recoillFactor;
    [SerializeField] protected bool isRecoilling = false;

    [SerializeField]protected PlayerController player;
    [SerializeField]protected float speed;

    protected float recoilTimer;
    protected Rigidbody2D rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public virtual void Start()
    {
        
    }
    public virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = PlayerController.Instance;
    }
    // Update is called once per frame
    public virtual void Update()
    {
        if(health <=0)
        {
            Destroy(gameObject);
        }
        if(isRecoilling)
        {
            if(recoilTimer < recoillength)
            {
                recoilTimer += Time.deltaTime;
            }
            else
            {
                isRecoilling = false;
                recoilTimer = 0;    
            }
        }
    }
    
    public virtual void EnermyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        health -= _damageDone;
        if(!isRecoilling)
        {
            rb.AddForce(-_hitForce * recoillFactor * _hitDirection);
        }
    }
}
