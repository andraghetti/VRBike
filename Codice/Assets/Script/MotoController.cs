using UnityEngine;
using GloveLibrary;
using System.Threading;
using System.IO;

public class MotoController : MonoBehaviour {

    private GloveLibrary.ThreadDTWSlideWindow dtw;
    private float pedalata;
    private float deltaTimePedalata;

    public string PortName = "COM5";
    
    public float deltaTimeSoglia = 0.71f;
    public float maxSpeedPedalata = 50.0f;

    private Rigidbody rigid;

    public WheelCollider FrontWheelCollider;
    public WheelCollider RearWheelCollider;
    public Transform FrontWheelTransform;
    public Transform RearWheelTransform;
	public Transform SteeringHandlebar;
    public Transform Centro;

    //Gearbox
    public float gearShiftRate = 10.0f;

    //Bike Body Lean
    public GameObject body;
    public float bodyVerticalLean = 4.0f;
    public float bodyHorizontalLean = 4.0f;
    private float horizontalLean = 0.0f;
    private float verticalLean = 0.0f;

    //Configurations
    public float EngineTorque = 1500f;
    public float MaxEngineRPM = 6000f;
    public float MinEngineRPM = 1000f;
    public float SteerAngle = 40f;
    [HideInInspector]
    public float Speed;
    public float highSpeedSteerAngle = 5f;
    public float highSpeedSteerAngleAtSpeed = 80f;
    public float maxSpeed = 180f;
    public float Brake = 2500f;
    public float friction = 6f;

    //private float EngineRPM = 0f;
    private float motorInput = 0f;
    private float defsteerAngle = 0f;
    private float RotationValue1 = 0f;
    private float RotationValue2 = 0f;

    [HideInInspector]
    public float steerInput = 0f;
    private bool reversing = false;
    private float sterzata;
    private int lastInputSterzata;
    private Vector3 euler;
    private Thread thread;

    void Start()
    {
        //Rigidbody
        rigid = GetComponent<Rigidbody>();
        rigid.constraints = RigidbodyConstraints.FreezeRotationZ;
        rigid.centerOfMass = new Vector3(Centro.localPosition.x * transform.localScale.x, Centro.localPosition.y * transform.localScale.y, Centro.localPosition.z * transform.localScale.z);
        rigid.maxAngularVelocity = 2f;
        
        defsteerAngle = SteerAngle;
        lastInputSterzata = 7;

        dtw = new ThreadDTWSlideWindow(PortName);
        dtw.PedalataTrovata += Dtw_PedalataTrovata; ;
        thread = new Thread(dtw.start);

        euler = SteeringHandlebar.localEulerAngles;

        thread.Start();
    }

    private void Dtw_PedalataTrovata(int inputSterzo, float inputPedalata)
    {
        if (lastInputSterzata == 0 && inputSterzo == 15)
            inputSterzo = 0;
            
        pedalata = inputPedalata/maxSpeedPedalata;

        sterzata = (inputSterzo - 7.0f) / 8;

        lastInputSterzata = inputSterzo;

        //msgToDebug("Sterzata: "+arg1+" -> "+sterzata);
        //msgToDebug("Velocità: "+arg2+" -> "+pedalata);
    }

    void FixedUpdate()
    {

        Inputs();
        Engine();
        Braking();

    }

    void Update()
    {
        Inputs();
        WheelAlign();
        Lean();

    }

    void Inputs()
    {

        Speed = rigid.velocity.magnitude * 3.6f;

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);

        //questo serviva per frenare dopo un certo tempo ma non serve per niente
        /*
        deltaTimePedalata += Time.deltaTime;

        if (deltaTimePedalata > deltaTimeSoglia)
        {
            pedalata = 0;
            msgToDebug("pedalata è 0 per via del delta");
        }
        */
        
        
        
        //msgToDebug("set input: "+motorInput+" ---- "+steerInput);

        //pedalata e sterzata derivano dalla cyclette, mentre input.getaxis deriva dalle frecce della tastiera
        motorInput = pedalata; // Input.GetAxis("Vertical");
        steerInput = sterzata; // Input.GetAxis("Horizontal");

        if (motorInput < 0 && transform.InverseTransformDirection(rigid.velocity).z < 0)
            reversing = true;
        else
            reversing = false;

    }

    void Engine()
    {

        //questo inibisce la sterzata ad alte velocità
        SteerAngle = Mathf.Lerp(defsteerAngle, highSpeedSteerAngle, (Speed / highSpeedSteerAngleAtSpeed));

        FrontWheelCollider.steerAngle = SteerAngle * steerInput;

        //EngineRPM = Mathf.Clamp((((Mathf.Abs((FrontWheelCollider.rpm + RearWheelCollider.rpm)) * gearShiftRate) + MinEngineRPM)), MinEngineRPM, MaxEngineRPM);//  / (currentGear + 1)

        if (Speed > maxSpeed)
        {
            RearWheelCollider.motorTorque = 0;
        }
        else if (!reversing)
        {
            RearWheelCollider.motorTorque = EngineTorque * Mathf.Clamp(motorInput, 0f, 1f);
        }

        if (reversing)
        {
            if (Speed < maxSpeed)//test con 10
            {
                RearWheelCollider.motorTorque = (EngineTorque * motorInput) / 5f;
            }
            else
            {
                RearWheelCollider.motorTorque = 0;
            }
        }

    }

    public void Braking()
    {

        // Deceleration.

        if (Mathf.Abs(pedalata) <= .05f)
        {
            FrontWheelCollider.brakeTorque = (Brake) / friction; // 25f;
            RearWheelCollider.brakeTorque = (Brake) / friction; // 25f;
        }
        else if (motorInput < 0 && !reversing)
        {
            FrontWheelCollider.brakeTorque = (Brake) * (Mathf.Abs(motorInput) / 5f);
            RearWheelCollider.brakeTorque = (Brake) * (Mathf.Abs(motorInput));
        }
        else
        {
            FrontWheelCollider.brakeTorque = 0;
            RearWheelCollider.brakeTorque = 0;
        }

    }

    void WheelAlign()
    {

        RaycastHit hit;
        WheelHit CorrespondingGroundHit;
        float extension_F;
        float extension_R;

        Vector3 ColliderCenterPointFL = FrontWheelCollider.transform.TransformPoint(FrontWheelCollider.center);
        FrontWheelCollider.GetGroundHit(out CorrespondingGroundHit);

        if (Physics.Raycast(ColliderCenterPointFL, -FrontWheelCollider.transform.up, out hit, (FrontWheelCollider.suspensionDistance + FrontWheelCollider.radius) * transform.localScale.y))
        {
            if (hit.transform.gameObject.layer != LayerMask.NameToLayer("Bike"))
            {
                FrontWheelTransform.transform.position = hit.point + (FrontWheelCollider.transform.up * FrontWheelCollider.radius) * transform.localScale.y;
                extension_F = (-FrontWheelCollider.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - FrontWheelCollider.radius) / FrontWheelCollider.suspensionDistance;
                Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + FrontWheelCollider.transform.up * (CorrespondingGroundHit.force / 8000), extension_F <= 0.0f ? Color.magenta : Color.white);
                Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - FrontWheelCollider.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
                Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - FrontWheelCollider.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
            }
        }
        else
        {
            FrontWheelTransform.transform.position = ColliderCenterPointFL - (FrontWheelCollider.transform.up * FrontWheelCollider.suspensionDistance) * transform.localScale.y;
        }
        RotationValue1 += FrontWheelCollider.rpm * (6) * Time.deltaTime;
        FrontWheelTransform.transform.rotation = FrontWheelCollider.transform.rotation * Quaternion.Euler(RotationValue1, FrontWheelCollider.steerAngle, FrontWheelCollider.transform.rotation.z);

        Vector3 ColliderCenterPointRL = RearWheelCollider.transform.TransformPoint(RearWheelCollider.center);
        RearWheelCollider.GetGroundHit(out CorrespondingGroundHit);

        if (Physics.Raycast(ColliderCenterPointRL, -RearWheelCollider.transform.up, out hit, (RearWheelCollider.suspensionDistance + RearWheelCollider.radius) * transform.localScale.y))
        {
            if (hit.transform.gameObject.layer != LayerMask.NameToLayer("Bike"))
            {
                RearWheelTransform.transform.position = hit.point + (RearWheelCollider.transform.up * RearWheelCollider.radius) * transform.localScale.y;
                extension_R = (-RearWheelCollider.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - RearWheelCollider.radius) / RearWheelCollider.suspensionDistance;
                Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + RearWheelCollider.transform.up * (CorrespondingGroundHit.force / 8000), extension_R <= 0.0f ? Color.magenta : Color.white);
                Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - RearWheelCollider.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
                Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - RearWheelCollider.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
            }
        } 
        else
        {
            RearWheelTransform.transform.position = ColliderCenterPointRL - (RearWheelCollider.transform.up * RearWheelCollider.suspensionDistance) * transform.localScale.y;
        }
        RotationValue2 += RearWheelCollider.rpm * (6) * Time.deltaTime;
        RearWheelTransform.transform.rotation = RearWheelCollider.transform.rotation * Quaternion.Euler(RotationValue2, RearWheelCollider.steerAngle, RearWheelCollider.transform.rotation.z);

        //Steering Wheel transforms
		if (SteeringHandlebar) {
            //ottengo la differenza di angolo tra la posizione corrente (euler.z) e l'angolo della wheelcollider
            float angleDifference = euler.z - FrontWheelCollider.steerAngle;
            
            euler.z = (euler.z - angleDifference) % 360; //applico la differenza
            SteeringHandlebar.localEulerAngles = euler;  // aggiorno la rotazione

            // SteeringHandlebar.transform.rotation = FrontWheelCollider.transform.rotation * Quaternion.Euler(270, FrontWheelCollider.steerAngle, FrontWheelCollider.transform.rotation.z+90);

        }
    }

    void Lean()
    {

        verticalLean = Mathf.Clamp(Mathf.Lerp(verticalLean, transform.InverseTransformDirection(rigid.angularVelocity).x * bodyVerticalLean, Time.deltaTime * 5f), -10.0f, 10.0f);

        WheelHit CorrespondingGroundHit;
        FrontWheelCollider.GetGroundHit(out CorrespondingGroundHit);

        float normalizedLeanAngle = Mathf.Clamp(CorrespondingGroundHit.sidewaysSlip, -1f, 1f);

        if (transform.InverseTransformDirection(rigid.velocity).z > 0f)
            normalizedLeanAngle = -1;
        else
            normalizedLeanAngle = 1;

        horizontalLean = Mathf.Clamp(Mathf.Lerp(horizontalLean, (transform.InverseTransformDirection(rigid.angularVelocity).y * normalizedLeanAngle) * bodyHorizontalLean, Time.deltaTime * 3f), -50.0f, 50.0f);

        Quaternion target = Quaternion.Euler(verticalLean, body.transform.localRotation.y + (rigid.angularVelocity.z), horizontalLean);
        body.transform.localRotation = target;

        rigid.centerOfMass = new Vector3((Centro.localPosition.x) * transform.localScale.x, (Centro.localPosition.y) * transform.localScale.y, (Centro.localPosition.z) * transform.localScale.z);

    }

    void OnApplicationQuit()
    {
        Debug.Log("Chiusura del programma: " + (dtw == null));
        thread.Abort();
        dtw.stopThread();
    }

    public static void msgToDebug(string txt)
    {
        Debug.Log(txt);
    }
}

