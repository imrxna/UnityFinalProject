using UnityEngine;

public class Enermy : MonoBehaviour
{
    [SerializeField] float health;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(health <=0)
        {
            Destroy(gameObject);
        }
    }
    
    public void EnermyHit(float _damageDone)
    {
        health -= _damageDone;
    }
}
