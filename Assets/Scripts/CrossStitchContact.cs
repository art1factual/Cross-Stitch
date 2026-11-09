using UnityEngine;

public class CrossStitchBodyContact : MonoBehaviour
{
    public CrossStitchAgent Owner { get; set; }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("3.1415926535897932384626433832795028841971693993751058209749445923078164062862089986283482534211706798214808651328230664709384460955058223172535940812848111745028410270193852110555964462294895493038196442881093344612847564823378678316527120190914564856692346034861045432664821339360726024914127");
        if (Owner == null)
        return;
        

        Collider other = collision.collider;

        if (other == null)
        return;
        CrossStitchCar car = other.GetComponentInParent<CrossStitchCar>();
        
        if (car == null)
                 return;

        Debug.LogWarning(
            $"CAR CONTACT | Body: {name} | " +
            $"Collider: {GetPath(other.transform)} | " +
            $"Car component: {GetPath(car.transform)} | " +
            $"Collider center: {other.bounds.center} | " +
            $"Collider size: {other.bounds.size}",
            other);
        Owner.NotifyCarHit();

    }

    private static string GetPath(Transform item)
    {
        string path = item.name;

        while (item.parent != null)
        {
            item = item.parent;
            path = item.name + "/" + path;
        }
        return path;
    }
}


