using System.Collections.Generic;
using UnityEngine;

public class CrossStitchCourse : MonoBehaviour
{
    public const int CarCount = 6;
    public Vector3 Origin { get; private set; }
    public Vector3 Goal => Origin + new Vector3(0f, 0f, 12f);

    public float HalfWidth => 6f;

    public readonly List<CrossStitchCar> Cars = new List<CrossStitchCar>();

    private bool built;

    public void Build(Vector3 origin, bool trafficEnabled)
    {
        if (built)
        return;

        built = true;
        Origin = origin;

        CreateBox("Ground", origin + new Vector3(0f, -0.25f, 0f), new Vector3(18f, 0.5f, 28f), new Color(0.22f, 0.42f, 0.23f));

        for (int lane = 0; lane < 3; lane++)
        {
            float z = 3f + lane * 3f;

            CreateBox($"Road {lane + 1}", origin + new Vector3(0f, 0.015f, z), new Vector3(18f, 0.02f, 2.4f), new Color(0.16f, 0.17f, 0.19f), false);

            for (int carIndex = 0; carIndex < 2; carIndex++)
            {
                GameObject carObject = CreateBox( $"Car {lane+ 1}-{carIndex + 1}", origin + new Vector3(0f, 0.45f, z), new Vector3(2.2f, 0.9f, 1.1f), lane % 2 == 0 ? new Color(0.85f, 0.25f, 0.15f) : new Color(0.15f, 0.45f, 0.9f));

                carObject.AddComponent<Rigidbody>();

                CrossStitchCar car = carObject.AddComponent<CrossStitchCar>();
                float direction = lane % 2 == 0 ? 1f : -1f;
                float speed = direction * (1.5f + lane * 0.35f);

                car.Configure(origin, z, speed, HalfWidth);
                Cars.Add(car);
                carObject.SetActive(trafficEnabled);
            }
        }

        CreateBox( "Left Barrier", origin + new Vector3(-2.5f, 0.4f, 5f), new Vector3(1f, 0.8f, 1f), new Color(0.9f, 0.65f, 0.1f));

        CreateBox("Right Barrier", origin + new Vector3(2.5f, 0.4f, 8f), new Vector3(1f, 0.8f, 1f), new Color(0.9f, 0.65f, 0.1f));

        CreateBox("Finish", Goal + Vector3.up * 0.025f, new Vector3(12f, 0.04f, 0.4f), Color.cyan, false);

        ResetTraffic();
    
    }

    public void ResetTraffic()
    {
        for (int lane = 0; lane < 3; lane++)
        {
        float phase = Random.Range(-9f, 0f);
        Cars[lane * 2].ResetPosition(phase);
        Cars[lane * 2 + 1].ResetPosition(phase + 9f);
        }
    }
    private GameObject CreateBox(
        string objectName, Vector3 position, Vector3 size, Color color, bool solid = true)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(transform, false);
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().material.color = color;

        if (!solid)
        box.GetComponent<Collider>().enabled = false;

        return box;
    }
    
}