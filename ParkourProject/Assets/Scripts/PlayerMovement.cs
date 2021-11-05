using System;
using TMPro;  //Import TextMeshPro Package
using UnityEngine;
using UnityEngine.SceneManagement;  //No Scene Management implemented yet

public class PlayerMovement : MonoBehaviour 
{
    //Assingables
    public Transform playerCam;
    public Transform orientation;
    
    //Rigid Bodies
    private Rigidbody rb;

    //Rotation and Look
    public bool lockLook;
    private float xRotation;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;

    //Others
    public int health, regen;
    int maxHealth;

    //Movement
    public Vector3 inputVector;
    public float moveSpeed = 4500;
    /*public float maxSpeed = 20;*/   //Not needed now that I've implemented "baseSpeed" variable
    public bool grounded, onSlope, fullAirControl;
    public LayerMask whatIsGround;
    
    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    //Base Speed Handling
    public float startBaseSpeed = 15f, baseSpeed, maxBaseSpeed;
    public float baseSpeedAccel, baseSpeedDeccel;
    public float bSAccelPoint, bSDeccelPoint, slowDownPoint;
    public float dragToSlowDown;

    //Crouching & Sliding
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideForce = 400;
    public float slideCounterMovement = 0.2f;
    public float crouchGravityMultiplier;

    //Jumping
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 550f;

    //Double Jumping
    public int startDoubleJumps = 1;
    int doubleJumpsLeft;
    
    //Input
    public float x, y;
    bool jumping, sprinting, crouching;

    //Air Control
    public float airForwardForce;

    //Air Dash
    public float dashForce, dashTime;
    bool readyToDash;
    int wTapTimes = 0;
    
    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;
    public float slopeDownwardForce;

    //WallRunning
    public LayerMask whatIsWall;
    RaycastHit wallHitR, wallHitL;
    public bool isWallRight, isWallLeft;
    public float maxWallrunTime;
    public float wallrunForce, wallrunUpwardForce, wallSpeedAdd;
    public int wallJumps, wallJumpsLeft;
    public bool readyToWallrun, isWallRunning;
    public bool resetDoubleJumpsOnWall;
    public GameObject lastWall;

    //CamTilt
    public float maxWallRunCameraTilt;
    public float wallRunCameraTilt = 0;

    //Climbing
    public float climbForce, climbSpeedAdd;
    public LayerMask whatIsLadder;
    bool alreadyStoppedAtLadder;

    //TODO Slow Motion
        //Yet to be Implemented

    //Animation
        //private Animator anim;

    void Awake() 
    {
        rb = GetComponent<Rigidbody>();
        maxHealth = health;
        baseSpeed = startBaseSpeed;
        //anim = GetComponent<Animator>();
    }

    void Start() 
    {
        playerScale =  transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        allowDrag = true;  //Implement Drag in the scene [Implemented]
    }

    private void FixedUpdate() 
    {
        Movement();

        /* Sprint Feature [Not needed since the movement is already agile enough]
        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = moveSpeed * 1.2f;
        }*/
    }

    private void Update() 
    {
        MyInput();
        if (!lockLook) Look();
        CheckForWall();  //Implement a check for Walls [Implemented]

        //Trail renderer for the Player {Not Needed, Only used it for debugging}
        //if (rb.velocity.magnitude <= 25) GetComponent<TrailRenderer>().startWidth
    }

    //It's more efficient to make a different script for input manager
    private void MyInput() 
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        crouching = Input.GetKey(KeyCode.LeftShift);
      
        //Crouching
        if (Input.GetKeyDown(KeyCode.LeftShift))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.LeftShift))
            StopCrouch();

        //Conditions to make a jump
        if (readyToJump && jumping && grounded) Jump();

        //Double Jumping
        if (Input.GetButtonDown("Jump") && !grounded && doubleJumpsLeft >= 1)
        {
            Jump();
        }

        //Wall Jumping
        if (Input.GetButtonDown("Jump") && wallJumpsLeft >= 1 && isWallRunning)
        {
            Jump();
        }

        //Dashing
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) /*|| Input.GetKeyDown(KeyCode.M)*/) && wTapTimes <= 1)
        {
            wTapTimes++;
            Invoke("ResetTapTimes", 0.3f);
        }

        //Dash Movement
        if (wTapTimes == 2 && readyToDash) Dash();   //Implement Dashing [Implemented]

        //Wall Running
        if (isWallRight && !grounded && readyToWallrun) StartWallrun();   //Implement WallRun [Implemented]
        if (isWallLeft && !grounded && readyToWallrun) StartWallrun();   //Implement WallRun [Implemented]

        //Reset WallRun
        if (!isWallRight && !isWallLeft && !readyToWallrun) readyToWallrun = true;

        //Climbing
        if (Physics.Raycast(transform.position, orientation.forward, 1, whatIsLadder) && y > .9f)
            Climb();  //Implement Climbing Feature [Implemented]
        else alreadyStoppedAtLadder = false;

        //Slow Motion Conditions
        //Implement Slow Motion for rigid bodies.
        ///if (Input.GetKeyDown(KeyCode.LeftControl) && readyForSlowMo) StartSlowMo();
    }

    private void ResetTapTimes()
    {
        wTapTimes = 0;
    }

    //==============================================================================================================================================================

    //<Summary>
    //Implement a better crouch mechanics, because this shortens the hitboxes
    //Bad for fps game
    //For both StartCrouch() and StopCrouch()
    //</Summary>
    private void StartCrouch() 
    {
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.velocity.magnitude > 0.5f) 
        {
            if (grounded) 
            {
                rb.AddForce(orientation.transform.forward * slideForce);
            }
        }
    }

    private void StopCrouch() {
        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    //==============================================================================================================================================================

    private void Movement() 
    {
        //Extra gravity
        //For better and faster ground check
        float gravityMultiplier = 10f;
        if (crouching) gravityMultiplier = crouchGravityMultiplier;
        rb.AddForce(Vector3.down * Time.deltaTime * gravityMultiplier);
        
        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement - fishy movement lol
        CounterMovement(x, y, mag);

        //Jumping Conditions
        //if (readyToJump && jumping) 
        //Jump();

        //Air Control
        //if (!grounded) 
        //rb.AddForce(orientation.forward * Time.deltaTime * airForwardForce);

        //Reset Stuff when touching ground
        if (grounded)
        {
            readyToDash = true;
            doubleJumpsLeft = startDoubleJumps;
            wallJumpsLeft = wallJumps;
        }

        //Set maximum speed
        float maxSpeed = this.baseSpeed;
        
        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump) 
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //Build up momentum on Slopes
        if (crouching && onSlope)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * slopeDownwardForce);
        }

        //If speed is larger than maxspeed, cancel out the input so the player doesn't go over maximum speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Slow down, don't allow going too fast
        //if (rb.velocity.magnitude > baseSpeed) SlowDown();   //Implemented in a better way using slowDownPoints below

        //Movement multipliers
        float multiplier = 1f, multiplierV = 1f;
        
        // Movement in air
        if (!grounded && !fullAirControl) 
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        if(fullAirControl)
        {
            multiplier = 0.35f;
        }
        
        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);

        //Base speed handling
        if (rb.velocity.magnitude > baseSpeed + bSAccelPoint) IncreaseBaseSpeed();  //Implement Increase Base Speeds [Implemented]
        if (rb.velocity.magnitude < baseSpeed - bSDeccelPoint) DecreaseBaseSpeed();   ////Implement Decrease Base Speeds [Implemented]

        //Slow down if current velocity reaches slowDownPoint
        if (rb.velocity.magnitude > baseSpeed + slowDownPoint) SlowDown();  ////Implement Slow Downs [Implemented]
        else rb.drag = 0;

    }

    private void Jump() 
    {
        if (grounded /*&& readyToJump*/)  //Check this out later how it affects the game during runtime
        {
            readyToJump = false;

            //Add jump forces
            rb.AddForce(Vector3.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);
            
            //If jumping while falling, reset y-axis velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0) 
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
            
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (!grounded && !isWallRunning && doubleJumpsLeft >= 1)
        {
            readyToJump = false;
            doubleJumpsLeft--;

            //Debug.Log("DoubleJump");  //To check if the jump is working correctly

            //Add jump forces
            rb.AddForce(orientation.forward * jumpForce * 1f);  //Forward force on double jump
            rb.AddForce(Vector2.up * jumpForce * 1.7f);
            rb.AddForce(normalVector * jumpForce * 0.7f);

            //Dampen y velocity
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.4f, rb.velocity.z);

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        //Wallrun
        if(isWallRunning && wallJumpsLeft >= 1)
        {
            //Debug.Log("WallJump");   //To check if the Walljump is working correctly

            readyToJump = false;
            wallJumpsLeft--;

            //Normal Jump
            //<Summary>
            //orientation.forward when included with addforce is always forward force
            //</Summary>
            rb.AddForce(Vector2.up * jumpForce * 0.85f);
            rb.AddForce(normalVector * jumpForce * 0.5f);
            rb.AddForce(orientation.forward * jumpForce * 0.5f);
            if (isWallRight) rb.AddForce(-orientation.right * jumpForce * 1.5f);
            if (isWallLeft) rb.AddForce(orientation.right * jumpForce * 1.5f);

            /*Sidewards Wall Hopping [Redacted]
            if (isWallRight||isWallLeft && Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) rb.AddForce(-orientation.up * jumpForce * 1f);
            if (isWallRight && Input.GetKey(KeyCode.A)) rb.AddForce(-orientation.right * jumpForce * 3.2f);
            if (isWallLeft && Input.GetKey(KeyCode.D)) rb.AddForce(orientation.right * jumpForce * 3.2f);
            */

            //Always add forward force
            rb.AddForce(orientation.forward * jumpForce * 1f);

            //Reset Velocity
            rb.velocity = Vector3.zero;

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump() 
    {
        readyToJump = true;
    }

    //Implement a better Input settings for the Dash()
    private void Dash()
    {
        readyToDash = false;
        wTapTimes = 0;

        //Add force for the dash
        if(Input.GetKeyDown(KeyCode.W))
        {
            rb.AddForce(orientation.forward * dashForce);
            rb.AddForce(orientation.up * dashForce * 0.5f);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            rb.AddForce(-orientation.forward * dashForce);
            rb.AddForce(orientation.up * dashForce * 0.5f);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            rb.AddForce(-orientation.right * dashForce);
            rb.AddForce(orientation.up * dashForce * 0.5f);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            rb.AddForce(orientation.right * dashForce);
            rb.AddForce(orientation.up * dashForce * 0.5f);
        }
        //Diagonal dash movement
        /*else if (Input.GetKeyDown(KeyCode.M))
        {
            rb.AddForce(orientation.forward * dashForce);
            rb.AddForce(orientation.right * dashForce);
            rb.AddForce(orientation.up * dashForce * 0.5f);
        }*/
        
    }

    float elapsedWallTime;

    private void StartWallrun()
    {
        //Debug.Log("Wallrunning");   //To check if the Wallrun is working correctly

        //When to stop
        if (grounded) StopWallRun();

        //Count up timer
        elapsedWallTime += Time.deltaTime;

        //Leave Wallrun when the timer expires
        if (elapsedWallTime > maxWallrunTime)
        {
            //StopWallRun();   //Check how this affects during the runtime
        }

        //rb.useGravity = false;
        isWallRunning = true;

        //Add an upward Force on the player
        rb.AddForce(orientation.up * wallrunUpwardForce * Time.deltaTime);

        if (rb.velocity.magnitude <= baseSpeed + wallSpeedAdd)
        {
            rb.AddForce(orientation.forward * wallrunForce * Time.deltaTime);

            //Make sure char sticks to wall
            if (isWallRight)
                rb.AddForce(orientation.right * wallrunForce / 5 * Time.deltaTime);
            else
                rb.AddForce(-orientation.right * wallrunForce / 5 * Time.deltaTime);
        }
    }

    private void StopWallRun()
    {
        isWallRunning = false;
        readyToWallrun = false;

        //Reset timer
        elapsedWallTime = 0;
    }

    //Checks if the player is on a wall using raycast
    private void CheckForWall()
    {
        isWallRight = Physics.Raycast(transform.position, orientation.right, out wallHitR, 1f, whatIsGround);
        isWallLeft = Physics.Raycast(transform.position, -orientation.right, out wallHitL, 1f, whatIsGround);

        //if (!isWallLeft && !isWallRight) wallJumpsLeft = wallJumps;
        if (!isWallLeft && !isWallRight && isWallRunning) StopWallRun();
        if ((isWallLeft || isWallRight) && resetDoubleJumpsOnWall) 
            doubleJumpsLeft = startDoubleJumps;
    }

    private void Climb()
    {
        //Makes possible to climb even when falling down fast - basically grab the ladder
        Vector3 vel = rb.velocity;
        if (rb.velocity.y < 0.5f && !alreadyStoppedAtLadder)
        {
            rb.velocity = new Vector3(vel.x, 0, vel.z);

            //Make sure player gets at the wall
            alreadyStoppedAtLadder = true;
            rb.AddForce(orientation.forward * 500 * Time.deltaTime);
        }

        //Push player up
        if (rb.velocity.magnitude < baseSpeed + climbSpeedAdd)
            rb.AddForce(orientation.up * climbForce * Time.deltaTime);

        //Doesn't Push into the wall
        if (!Input.GetKey(KeyCode.S)) y = 0;
    }

    /* Implements Slow Motion on rigid bodies. 
     * But check out Steven's method on pausing physics and rigid bodies differently.
    private void StartSlowMo()
    {
        readyForSlowMo = false;
        slowMoPlane.SetActive(true);

        Time.timeScale = slowMoStrength;

        Invoke(nameof(StopSlowMo), slowMoTime * slowMoStrength);
    }
    private void StopSlowMo()
    {
        slowMoPlane.SetActive(false);

        Time.timeScale = 1f;

        Invoke(nameof(ResetSlowMo), slowMoCooldown);
    }
    private void ResetSlowMo()
    {
        readyForSlowMo = true;
    }*/

    private float desiredX;
    private void Look() 
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;
        
        //Rotate, and also make sure we dont over- or under- rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Performs the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, wallRunCameraTilt);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);

        //While Wallrunning [Tilts camera in 0.5 seconds]
        if (Math.Abs(wallRunCameraTilt) < maxWallRunCameraTilt && isWallRunning && isWallRight)
            wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * 2;
        if (Math.Abs(wallRunCameraTilt) < maxWallRunCameraTilt && isWallRunning && isWallLeft)
            wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * 2;

        //[Tilts camera back again to original position]
        if (wallRunCameraTilt > 0 && !isWallRight && !isWallLeft)
            wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * 2;
        if (wallRunCameraTilt < 0 && !isWallRight && !isWallLeft)
            wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * 2;
    }

    float timer1, timer2;
    float extraBaseDeccel; //Exponentially decreases the base speed

    private void IncreaseBaseSpeed()
    {
        if (baseSpeed >= maxBaseSpeed) return;

        ///Debug.Log("Decreasing BaseSpeed");    //To check if the IncreaseSpeed is working correctly

        //Only increase in .1 ticks
        timer1 += Time.deltaTime * baseSpeedAccel;

        extraBaseDeccel = 0;

        if (timer1 > 1f)
        {
            baseSpeed += 0.1f;
            timer1 = 0;
        }
    }

    private void DecreaseBaseSpeed()
    {
        if (baseSpeed <= startBaseSpeed) return;

        ///Debug.Log("Increasing BaseSpeed");   //To check if the DecreaseSpeed is working correctly

        //Only decrease in .1 ticks
        timer2 += Time.deltaTime * baseSpeedDeccel * extraBaseDeccel;
        extraBaseDeccel += Time.deltaTime * 0.5f;

        if (timer2 > 1f)
        {
            baseSpeed -= 0.1f;
            timer2 = 0;
        }
    }

    private bool allowDrag = true;

    private void SlowDown()
    {
        //Debug.Log("SlowingDown");   //To check if the SlowDown is working correctly
        //Vector3 baseVelVector = rb.velocity.normalized * baseSpeed;
        //rb.AddForce(-rb.velocity * 1f * Time.deltaTime, ForceMode.Impulse);
        //Debug.Log("Drag = 1");   //To check if the Drag is working correctly

        if (allowDrag) rb.drag = 1;
    }

    private void CounterMovement(float x, float y, Vector2 mag) 
    {
        /*Limit diagonal running. Only when holding down W and D or W and A
        if (x != 0 && y != 0)
        {
            if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > baseSpeed)
            {
                float fallspeed = rb.velocity.y;
                Vector3 n = rb.velocity.normalized * baseSpeed;
                rb.velocity = new Vector3(n.x, fallspeed, n.z);
            }
        } */

        //if (!grounded || jumping) return;
        if (!grounded || jumping || isWallRunning) return;

        //Slow down sliding
        if (crouching) 
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        //Counters when no Input and still moving || Input in opposite direction then velocity
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > baseSpeed) 
        {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * baseSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    //Find the velocity relative to where the player is looking
    //Useful for vectors calculations regarding movement and limiting movement
    public Vector2 FindVelRelativeToLook() 
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);
        
        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v) {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    //Handle ground detection

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(Vector3.Angle(transform.up, collision.contacts[0].normal));
        //Checks the ground detection
    }

    private void OnCollisionStay(Collision other) 
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++) 
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal)) 
            {
                onSlope = false;
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
            else
            {
                onSlope = true;
            }

            //Save the lastWall instance
            if(isWallRunning)
            {
                if (lastWall != other.gameObject)
                {
                    //Debug.Log("WallChanged!");   //To check the wall changes during wallruns
                    lastWall = other.gameObject;
                    wallJumpsLeft = wallJumps;
                }
            }
        }

        //Invoke ground or wall cancel, since we can't check normals with Collision Exit
        float delay = 3f;
        if (!cancellingGrounded) 
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void StopGrounded() 
    {
        grounded = false;
    }

    #region abilityFunctions

    public void DashInDirection(Vector3 dir, float force)
    {
        rb.AddForce(dir * force, ForceMode.Impulse);
    }

    public void PreventDrag(float time)
    {
        allowDrag = false;
        Invoke(nameof(ResetAllowDrag), time);
    }
    private void ResetAllowDrag()
    {
        allowDrag = true;
    }

    #endregion
}