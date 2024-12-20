using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Replay{
    public List<double> states;
    public double reward;

    public Replay(double xr, double ballz, double ballvx, double r)
    {
        this.states = new List<double>();
        this.states.Add(xr);
        this.states.Add(ballz);
        this.states.Add(ballvx);
        this.reward = r;
    }
}

public class Brain : MonoBehaviour
{
    public GameObject ball;

    ANN ann;
    float reward=0.0f;
    List<Replay> replayMemmory = new List<Replay>();
    int mCapacity = 10000;

    float discount = 0.99f;
    float exploreRate =100.0f;
    float maxExploreRate=100.0f;
    float minExploreRate = 0.01f;
    float exploreDecay =0.0001f;

    Vector3 ballStartPos;
    int failCount=0;
    float tiltSpeed = 0.5f;

    float timer=0;
    float maxBalanceTime=0;

    // Start is called before the first frame update
    void Start()
    {
        ann=new ANN(3,2,1,6,0.2f);
        ballStartPos=ball.transform.position;
        Time.timeScale=5.0f;
    }

    GUIStyle guiStyle=new GUIStyle();
    void OnGUI(){
        guiStyle.fontSize=25;
        guiStyle.normal.textColor=Color.white;
        GUI.BeginGroup(new Rect(10,10,600,150));
        GUI.Box(new Rect(0,0,140,140),"Stats",guiStyle);
        GUI.Label(new Rect(10,25,500,30),"Fails: "+failCount,guiStyle);
        GUI.Label(new Rect(10,50,500,30),"Decay Rate: "+exploreRate,guiStyle);
        GUI.Label(new Rect(10,75,500,30),"Last Best Balance: "+maxBalanceTime,guiStyle);
        GUI.Label(new Rect(10,100,500,30),"This Balance: "+timer,guiStyle);
        GUI.EndGroup();
    }

    void FixedUpdate() {
        timer+=Time.deltaTime;
        List<double> states = new List<double>();
        List<double> qs=new List<double>();

        states.Add(this.transform.position.x);
        states.Add(ball.transform.position.z);
        states.Add(ball.GetComponent<Rigidbody>().angularVelocity.x);

        qs= SoftMax(ann.CalcOutput(states));
        double maxQ=qs.Max();
        int maxQIndex=qs.ToList().IndexOf(maxQ);
        exploreRate=Mathf.Clamp(exploreRate - exploreDecay, minExploreRate,maxExploreRate);
        if(maxQIndex == 0)
            this.transform.Rotate(Vector3.right,tiltSpeed* (float)qs[maxQIndex]);
        else if(maxQIndex == 1)
            this.transform.Rotate(Vector3.right, -tiltSpeed* (float)qs[maxQIndex]);
            
        if(ball.GetComponent<BallState>().dropped)
            reward=-1.0f;
        else
            reward=0.1f;
            
        Replay lastMemmory=new Replay(this.transform.rotation.x,
        ball.transform.position.z,
        ball.GetComponent<Rigidbody>().angularVelocity.x,
        reward); 

        if(replayMemmory.Count > mCapacity)
            replayMemmory.RemoveAt(0);

        replayMemmory.Add(lastMemmory);

        if(ball.GetComponent<BallState>().dropped){
            for(int i =  replayMemmory.Count -1; i>=0; i--){
                List<double> toutputsOld = new List<double>();
                List<double> toutputsNew = new List<double>();
                toutputsOld= SoftMax(ann.CalcOutput(replayMemmory[i].states));

                double maxQOld = toutputsOld.Max();
                int action = toutputsOld.ToList().IndexOf(maxQOld);
                double feedback;
                if(i == replayMemmory.Count -1 || replayMemmory[i].reward == -1)
                {
                    feedback=replayMemmory[i].reward;
                }else 
                {
                    toutputsNew = SoftMax(ann.CalcOutput(replayMemmory[i+1].states));
                    maxQ=toutputsNew.Max();
                    feedback=(replayMemmory[i].reward + discount*maxQ);

                }
                toutputsOld[action]=feedback;
                ann.Train(replayMemmory[i].states, toutputsOld);
            }
            if(timer > maxBalanceTime)
            {
                maxBalanceTime=timer;
                //exploreRate=maxExploreRate;
            }
            timer=0;
            ball.GetComponent<BallState>().dropped = false;
            this.transform.rotation=Quaternion.identity;
            ResetBall();
            replayMemmory.Clear();
            failCount++;
        }
    }

    void ResetBall(){
        ball.transform.position=ballStartPos;
        ball.GetComponent<Rigidbody>().velocity=new Vector3(0,0,0);
        ball.GetComponent<Rigidbody>().angularVelocity=new Vector3(0,0,0);
    }
    List<double> SoftMax(List<double> values){
        double max=values.Max();
        float scale= 0.0f;
        for(int i=0;i < values.Count;i++)
            scale += Mathf.Exp((float)(values[i] - max));
        List<double> result= new List<double>();
        for(int i=0;i < values.Count; i++)
            result.Add(Mathf.Exp((float)(values[i] - max))/scale);
        
        return result;
    }
    
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("space"))
            ResetBall();
    }
}
