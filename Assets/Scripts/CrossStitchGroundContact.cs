using System.Collections.Generic;
using UnityEngine;

public class CrossStitchGroundContact : MonoBehaviour
{
    [SerializeField]

    private bool grounded;

    public bool Grounded => grounded;

    private readonly HashSet<Collider> supports = new HashSet<Collider>();

    private void OnCollisionEnter(Collision collision)
    {
        UpdateContact(collision);
        

    }
    private void OnCollisionStay(Collision collision)
    {
        UpdateContact(collision);

    }
    private void OnCollisionExit(Collision collision)
    {
        supports.Remove(collision.collider);
        grounded = supports.Count > 0;
    }

    private void UpdateContact(Collision collision)
    {
        //Platforms static
        //Vehicles do not appear as ground.
        if (collision.rigidbody != null)
        return;
        
        bool hasSupport = false;

        foreach(ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f)
            {
                hasSupport = true;
                break;
            }
        }
    

        if (hasSupport)
        supports.Add(collision.collider);
            else 
            supports.Remove(collision.collider);

        grounded = supports.Count > 0;
    }

        public void ClearContacts()
        {
            supports.Clear();
            grounded = false;
        }
        private void OnDisable()
        {
            ClearContacts();
        }
    
}
