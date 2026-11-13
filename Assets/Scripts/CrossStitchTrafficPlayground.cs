using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class CrossStitchTrafficPlayground : MonoBehaviour
{
    private const int Capacity = 6;
    private const int LaneCount = 3;
    private const float HalfLength = 12f;
    private const float LaneWidth = 2.2f;

    private const float CarLength = 2.5f;
    private const float MinSpacing = CarLength + 0.35f;
    private const float AutoSpeed = 4f;
    private const float MaxSpeed = 9f;

    [SerializeField] private Camera viewCamera;

    private sealed class Car
    {
        public Rigidbody Body;
        public int Lane;
        public float X;

        public float Speed;
    }

    private readonly Car[] cars = new Car[Capacity];

    private Transform generated;
    private Camera createdCamera;
    private Car selected;
    private Car driven;

    private bool running;
    private bool accelerate;

    private bool brake;

    private string status = "Click a lane to place traffic.";
    private readonly float[] nextX = new float[Capacity];

    private void Start()
    {
        generated = new GameObject("Generated Traffic Playground").transform;
        generated.SetParent(transform, false);
        //Box("Road", new Vector3(0f, -0.2f, 0f),
        new Vector3(2f * HalfLength, 0.4f, LaneCount * LaneWidth);

      //  for (int i = 0; i <= LaneCount; i++)
    //    {
     //       GameObject stripe = Box("Lane Boundary", new Vector3(0f, 0.013f, (i - LaneCount * 0.5f) * LaneWidth),
//        new Vector3(2f * HalfLength, 0.025f, 0.05f));
            //stripe.GetComponent<Collider>().enabled = false;


       // }

        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null)
        {
            createdCamera = new GameObject("Traffic Camera").AddComponent<Camera>();
            viewCamera = createdCamera;
            viewCamera.orthographic = true;
            viewCamera.orthographicSize = 9f;
            viewCamera.transform.position = transform.TransformPoint(new Vector3(0f, 20f, -8f));
            viewCamera.transform.LookAt(transform.position, transform.forward);

        }

        for (int i = 0; i < Capacity; i++)
        {
            GameObject vehicle = Box("Car " + (i + 1), Vector3.zero, new Vector3(CarLength, 0.8f, 1.45f));
            Rigidbody body = vehicle.AddComponent<Rigidbody>();

            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            vehicle.SetActive(false);
            cars[i] = new Car{ Body = body };
        }
    }

    private GameObject Box(string objectName, Vector3 localPosition, Vector3 size)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(generated, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;
        return box;
    }

    private void Update()
    {

        
        bool toggleRun, toggleDrive, leftClick, rightClick;
        Vector2 pointer;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        toggleRun = keyboard != null && keyboard.tabKey.wasPressedThisFrame;
        toggleDrive = keyboard != null && keyboard.eKey.wasPressedThisFrame;
        accelerate = keyboard != null && keyboard.wKey.isPressed;
        brake = keyboard != null && keyboard.sKey.isPressed;
        leftClick = mouse != null && mouse.leftButton.wasPressedThisFrame;
        rightClick = mouse != null && mouse.rightButton.wasPressedThisFrame;
        pointer = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
        turnLeft = keyboard != null && keyboard.aKey.wasPressedThisFrame;
        turnRight = keyboard != null && keyboard.dKey.wasPressedThisFrame;

#elif ENABLE_LEGACY_INPUT_MANAGER
        turnLeft = Input.GetKeyDown(KeyCode.A);
        turnRight = Input.GetKeyDown(KeyCode.D);
        toggleRun = Input.GetKeyDown(KeyCode.Tab);
        toggleDrive = Input.GetKeyDown(KeyCode.E);
        accelerate = Input.GetKey(KeyCode.W);
        brake = Input.GetKey(KeyCode.S);
        leftClick = Input.GetMouseButtonDown(0);
        rightClick = Input.GetMouseButtonDown(1);
        pointer = Input.mousePosition;
#else
        return;
#endif
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
        if (toggleRun)
        {
            running = !running;
            if (!running) driven = null;
            status = running ? "Click a car, then press E to drive." : "Click a lane to place the traffic onto the road";

        }
        if (leftClick || rightClick)
        {
            Ray ray = viewCamera.ScreenPointToRay(pointer);
            Car hitCar = FindCar(ray);
            if (rightClick && !running && hitCar != null)
            {
                hitCar.Body.gameObject.SetActive(false);

                if (selected == hitCar)
                {
                    selected = null;
                }
              

            
                status = "Car removed.";
            
            
            }
        else if (leftClick && hitCar != null)
        {
            selected = hitCar;
            status = "Selected! " + selected.Body.name + ".";


        }
        else if (leftClick && !running)
        {
            Plane road = new Plane(transform.up, transform.position);
            
            if (road.Raycast(ray, out float distance))
            {
                Spawn(transform.InverseTransformPoint(ray.GetPoint(distance)));
            }
        }
        }
           if (toggleDrive && running && selected != null)
           {
            if (driven != null)
            {
                driven = null;
                status = "Automatic traffic resumed.";
            }
            else
            {
                driven = selected;
                status = "Driving " + driven.Body.name + ". W accelerated, S brakes. : Turn Left - A, Turn Right - D.";

            }
           }

           if (running && driven != null && turnLeft != turnRight)
           {
            int offset = turnLeft ? driven.Direction : -driven.Direction;
            TryChangeLane(driven, driven.Lane + offset);
           }
#endif
    }
private Car FindCar(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return null;
        }
        foreach (Car car in cars)
        
        if (car.Body == hit.rigidbody) return car;
        
        return null;
    }

    private void Spawn(Vector3 point)
    {
        int lane = Mathf.FloorToInt(point.z / LaneWidth + LaneCount * 0.5f);
        if (lane < 0 || lane >= LaneCount || Mathf.Abs(point.x) > HalfLength - CarLength * 0.5f)
        {
            status = "Click inside of a lane, so taht it is away from the end of the road.";
            return;
        }

        Car slot = null;
        foreach (Car car in cars)
        {
            if (!car.Body.gameObject.activeSelf)
            {
                if (slot == null) slot = car;
                continue;
            }

            if (car.Lane == lane && Mathf.Abs(car.X - point.x) < MinSpacing)
            {
                status = "Provide spacing between the cars.";
                return;
            }
        }
            if (slot == null)
            {
                status = "Six Cars have been Placed. Please Right-Click a car to free the lane presented.";
                return;
            }

            slot.Lane = lane;
            slot.Direction = lane % 2 == 0 ? 1 : -1;
            slot.X = point.x;
            slot.Speed = AutoSpeed;
            slot.Body.transform.position = WorldPosition(slot, slot.X);
            slot.Body.gameObject.SetActive(true);
            slot.Body.position = WorldPosition(slot, slot.X);
            selected = slot;
            status = "Placed " + slot.Body.name + ".";
        }

        private Vector3 WorldPosition(Car car, float x)
    {
        float z = (car.Lane - (LaneCount - 1) * 0.5f) * LaneWidth;
        return transform.TransformPoint(new Vector3(x, 0.4f, z));
    }

    private void FixedUpdate()
    {
        if (!running) return;

        float dt = Time.fixedDeltaTime;
        float edge = HalfLength + CarLength * 0.5f;

        float loopLength = 2f * edge;

        for (int i =0; i < cars.Length; i++)
        {
            Car car = cars[i];
            if (!car.Body.gameObject.activeSelf) continue;

            float targetSpeed = AutoSpeed;
            float response = 3f;
            if (car == driven)
            {
                targetSpeed = brake ? 0f : accelerate ? MaxSpeed : car.Speed;
                response = brake ? 8f : 4f;
               float direction = car.Direction;
            }

            car.Speed = Mathf.MoveTowards(car.Speed, targetSpeed, response * dt);
            float direction = car.Lane % 2 == 0 ? 1f : -1f;
            float travel = car.Speed * dt;
            foreach (Car ahead in cars)
            {
                if (ahead == car || ahead.Lane != car.Lane || !ahead.Body.gameObject.activeSelf)
                continue;
                float gap = Mathf.Repeat((ahead.X - car.X) * direction, loopLength);
                travel = Mathf.Min(travel, Mathf.Max(0f, gap - MinSpacing));
            }

            nextX[i] = car.X + direction * travel;
            if (travel < car.Speed * dt) car.Speed = travel / dt;
        }

        for (int i = 0; i < cars.Length; i++)
        {
            Car car = cars[i];
            if (!car.Body.gameObject.activeSelf) continue;

    
            float x = nextX[i];
            
            if (x > edge || x < -edge)
            {
                car.X = Mathf.Repeat(x + edge, loopLength) - edge;
            car.Body.position = WorldPosition(car, car.X);
            }
            else
            {
                car.X = x;
                car.Body.MovePosition(WorldPosition(car, car.X));
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12, 12, 400, 130), GUI.skin.box);
        GUILayout.Label(running ? "Traffic running | Tab to Edit" : "Build | Tab Runs Traffic");
        if (!running) GUILayout.Label("Left-Click lane to spawn | Right-Click car to remove");
        else GUILayout.Label("Click the Car to Select | E to take or release the Control");
        if (selected != null) GUILayout.Label("Selected: " + selected.Body.name);
        if (driven != null) GUILayout.Label($"Speed: {driven.Speed:F1} m/s | W accelerate | S brake");
        GUILayout.Label(status);
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (generated != null) Destroy(generated.gameObject);
        if (createdCamera != null) Destroy(createdCamera.gameObject);
    }
    }