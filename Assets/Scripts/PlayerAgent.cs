using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PlayerAgent : Agent
{
    public new Rigidbody2D rigidbody { get; private set; }
    public Bullet bulletPrefab;

    public float thrustSpeed = 1f;
    public bool thrusting { get; private set; }

    public float turnDirection { get; private set; } = 0f;
    public float rotationSpeed = 0.1f;

    public float respawnDelay = 3f;
    public float respawnInvulnerability = 3f;

    public bool screenWrapping = true;
    private Bounds screenBounds;
    private bool canTakeRewardForAvoidingBeingHit = true;
    private bool canShoot = false;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    public override void Initialize()
    {
        GameObject[] boundaries = GameObject.FindGameObjectsWithTag("Boundary");

        // Disable all boundaries if screen wrapping is enabled
        for (int i = 0; i < boundaries.Length; i++) {
            boundaries[i].SetActive(!screenWrapping);
        }

        // Convert screen space bounds to world space bounds
        screenBounds = new Bounds();
        screenBounds.Encapsulate(Camera.main.ScreenToWorldPoint(Vector3.zero));
        screenBounds.Encapsulate(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0f)));
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(transform.rotation.z);
        sensor.AddObservation(rigidbody.velocity);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        thrusting = actions.DiscreteActions[0] == 1;
        turnDirection = 0f;

        if (actions.DiscreteActions[1] == 1)
        {
            turnDirection = 1f;
        }
        else if (actions.DiscreteActions[1] == 2)
        {
            turnDirection = -1f;
        }

        if (actions.DiscreteActions[2] == 1 && !canShoot) {
            canShoot = true;
            shootBasedOnScore();
        } else if (actions.DiscreteActions[2] == 0) {
            canShoot = false;
        }
    }

    private void shootBasedOnScore()
    {
        if (GameManager.Instance.score < 5000)
        {
            Shoot(transform.up);
        } else if (GameManager.Instance.score >= 5000 && GameManager.Instance.score < 15000)
        {
            Shoot(transform.up);
            Shoot(0.75f * transform.up);
        } else if (GameManager.Instance.score >= 15000)
        {
            Shoot(Quaternion.Euler(0f, 0f, 30f) * transform.up);
            Shoot(transform.up);
            Shoot(Quaternion.Euler(0f, 0f, -30f) * transform.up);
        }
    }

    private void Shoot(Vector3 direction)
    {
        Bullet bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
        bullet.Shoot(direction);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        bool forwardInput = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        discreteActions[0] = forwardInput ? 1 : 0;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
            discreteActions[1] = 1;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
            discreteActions[1] = 2;
        }
        else {
            discreteActions[1] = 0;
        }

        bool shootInput = Input.GetKey(KeyCode.Space) || Input.GetMouseButtonDown(0);
        discreteActions[2] = shootInput ? 1 : 0;
    }

    private void FixedUpdate()
    {
        if (thrusting) {
            rigidbody.AddForce(transform.up * thrustSpeed);
        }

        if (turnDirection != 0f) {
            rigidbody.AddTorque(rotationSpeed * turnDirection);
        }

        if (screenWrapping) {
            ScreenWrap();
        }

        if (canTakeRewardForAvoidingBeingHit) {
            AddReward(0.0002f);
        }
    }

    private void ScreenWrap()
    {
        // Move to the opposite side of the screen if the player exceeds the bounds
        if (rigidbody.position.x > screenBounds.max.x + 0.5f) {
            rigidbody.position = new Vector2(screenBounds.min.x - 0.5f, rigidbody.position.y);
        }
        else if (rigidbody.position.x < screenBounds.min.x - 0.5f) {
            rigidbody.position = new Vector2(screenBounds.max.x + 0.5f, rigidbody.position.y);
        }
        else if (rigidbody.position.y > screenBounds.max.y + 0.5f) {
            rigidbody.position = new Vector2(rigidbody.position.x, screenBounds.min.y - 0.5f);
        }
        else if (rigidbody.position.y < screenBounds.min.y - 0.5f) {
            rigidbody.position = new Vector2(rigidbody.position.x, screenBounds.max.y + 0.5f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        canTakeRewardForAvoidingBeingHit = false;

        if (collision.gameObject.CompareTag("Asteroid"))
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = 0f;

            GameManager.Instance.OnPlayerDeath();
        } 
        else if (collision.gameObject.CompareTag("Boundary"))
        {
            AddReward(-0.1f);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Boundary"))
        {
            AddReward(-0.001f);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        canTakeRewardForAvoidingBeingHit = true;
    }
}
