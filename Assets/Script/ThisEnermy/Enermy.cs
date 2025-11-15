using UnityEngine;

public class Enermy : MonoBehaviour
{
    [SerializeField] float health;
    [SerializeField] float recoillength;
    [SerializeField] float recoillFactor;
    [SerializeField] bool isRecoilling = false;

    float recoilTimer;
    Rigidbody2D rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    // Update is called once per frame
    void Update()
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
    
    public void EnermyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        health -= _damageDone;
        if(!isRecoilling)
        {
            rb.AddForce(-_hitForce * recoillFactor * _hitDirection);
        }
    }
}
