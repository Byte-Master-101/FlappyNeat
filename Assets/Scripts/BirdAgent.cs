using System.Collections;
using System.Collections.Generic;
using UNeaty;
using UnityEngine;

public class BirdAgent : NeatAgent
{
    [SerializeField] float GravityForce = 1;
    [SerializeField] float JumpForce = 1;
    [SerializeField] float BackgroundSpeed = 1f;
    [SerializeField] Sprite WingUp;
    [SerializeField] Sprite WingDown;
    [SerializeField] Transform Background;
    [SerializeField] Transform Columns;
    [SerializeField] SpriteRenderer Floor1;
    [SerializeField] SpriteRenderer Floor2;
    [SerializeField] SpriteRenderer Sky1;
    [SerializeField] SpriteRenderer Sky2;
    [SerializeField] SpriteRenderer Bounds;

    System.Random TheRand;

    Vector3 BirdStartPos;

    SpriteRenderer TheSpriteRenderer;

    List<float> ColumnStartPositionsX;

    float VelocityY;

    Transform Back1;
    Transform Back2;

    float Back1StartPos;

    bool Back1OnRightSide = true;

    public override void AgentStart()
    {
        BirdStartPos = transform.localPosition;

        TheSpriteRenderer = GetComponent<SpriteRenderer>();

        Back1 = Background.GetChild(0);
        Back2 = Background.GetChild(1);

        Back1StartPos = Back1.localPosition.x;

        ColumnStartPositionsX = new List<float>();
        Transform LastColumn = null;
        foreach (Transform aColumn in Columns)
        {
            LastColumn = aColumn;
            ColumnStartPositionsX.Add(aColumn.localPosition.x);
        }

        DestroyImmediate(LastColumn.gameObject);
    }

    public override void AgentReset()
    {
        if(TheNeatAcademy.ShouldPreviewOnly)
            TheRand = new System.Random();
        else
            TheRand = new System.Random(60);
        
        Reward = 0;
        VelocityY = 0;
        transform.localPosition = BirdStartPos;

        int ColumnIndex = 0;
        foreach (Transform aColumn in Columns)
        {
            aColumn.localPosition = new Vector3(ColumnStartPositionsX[ColumnIndex] + ColumnStartPositionsX[0],
                    ((float)TheRand.NextDouble() - 0.5f) * 4f, aColumn.localPosition.z);
            ColumnIndex++;
        }
    }

    public override void AgentStep(double[] Action)
    {
        Reward++;

        MoveBackground();

        ApplyGravity();

        DetectHit();

        Jump(Action[0]);

        if (Reward >= 8500 && !TheNeatAcademy.ShouldPreviewOnly)
            KillAgent();
    }
    
    public override double[] CollectState()
    {
        double[] State = new double[4];

        Transform NextColumn = null;
        foreach (Transform aColumn in Columns)
            if (aColumn.localPosition.x > -1)
            {
                if (NextColumn == null)
                    NextColumn = aColumn;
                else if (aColumn.localPosition.x < NextColumn.localPosition.x)
                    NextColumn = aColumn;
            }

        State[0] = NextColumn.localPosition.y + 2;
        State[1] = transform.localPosition.y + 2.5;
        State[2] = NextColumn.localPosition.x / 2;
        State[3] = 0.5 - (VelocityY * 4);

        return State;
    }

    void MoveBackground()
    {
        Vector3 Offset = new Vector3(BackgroundSpeed / 30, 0, 0);

        foreach (Transform aColumn in Columns)
        {
            aColumn.localPosition += Offset;
            if (aColumn.localPosition.x <= ColumnStartPositionsX[ColumnStartPositionsX.Count - 1])
                aColumn.localPosition = new Vector3(ColumnStartPositionsX[0], ((float)TheRand.NextDouble()-0.5f)*4f, aColumn.localPosition.z);
        }

        Back1.localPosition += Offset;
        Back2.localPosition += Offset;

        if (Back1.localPosition.x <= 0 && Back1OnRightSide)
        {
            Back2.localPosition = new Vector3(Back1StartPos, Back2.localPosition.y, Back2.localPosition.z);
            Back1OnRightSide = false;
        }
        if (Back2.localPosition.x <= 0 && !Back1OnRightSide)
        {
            Back1.localPosition = new Vector3(Back1StartPos, Back1.localPosition.y, Back1.localPosition.z);
            Back1OnRightSide = true;
        }
        //print(CurrentBackSpeed);
    }

    void ApplyGravity()
    {
        VelocityY -= (GravityForce / 100);
        transform.localPosition += new Vector3(0, VelocityY, 0);
    }

    void DetectHit()
    {
        foreach (Transform aColumn in Columns)
            foreach (Transform aColumnSide in aColumn)
                if (aColumnSide.GetComponent<SpriteRenderer>().bounds.Intersects(Bounds.bounds))
                {
                    KillAgent();
                    return;
                }

        if (Bounds.bounds.Intersects(Floor1.bounds) || Bounds.bounds.Intersects(Floor2.bounds) ||
            !(Bounds.bounds.Intersects(Sky1.bounds) || Bounds.bounds.Intersects(Sky2.bounds)))
            KillAgent();
    }

    void Jump(double Action)
    {
        if (Action > 0.5f)
            VelocityY = (JumpForce / 8);

        if (VelocityY > 0)
            TheSpriteRenderer.sprite = WingDown;
        else
            TheSpriteRenderer.sprite = WingUp;
    }
}
