using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class TownGenerator : MonoBehaviour
{

    //각 버텍스의 위치
    public List<Transform> node=new List<Transform>();
    public List<Vector2> outnode=new List<Vector2>();
    

    public List<Road> RoadList=new List<Road>();


    public List<circumcircle> circumcircles = new List<circumcircle>();


    public int loopCount=0;

    public float t=0;

    public void start()
    {
        for(int i=0;i<node.Count;i++)
            outnode.Add( new Vector2( node[i].position.x ,node[i].position.z ) );
    }

    public void Update() {
        t+=Time.deltaTime;
        if(t>1)
        {
            GenerateTown();
            t=0;
        }
    }

    public void GenerateTown() {
        outnode=new List<Vector2>();
        RoadList=new List<Road>();



        for (int i=0;i<node.Count;i++)
            outnode.Add( new Vector2( node[i].position.x ,node[i].position.z ) );

        
        GenerateVoronoy();

        new Thread(()=>{
            for(int i=0;i<loopCount;i++)
            {
                GenerateVoronoy();
            }
        
            GenerateLinde();
        }).Start();
    }


    public void GenerateVoronoy()
    {

        //모든 외접원을 저장
        circumcircles = new List<circumcircle>();

        Combination comb=new Combination(outnode.Count,3);


        int nodeCount=outnode.Count;

        int[] c;

        while((c=comb.next())!=null)
        {
            circumcircle Circumcircle =new circumcircle(new Vector2[]{outnode[c[0]],outnode[c[1]],outnode[c[2]]});

            for(int j=0;j<nodeCount;j++){
                float dis = ( outnode[j]-Circumcircle.center ).sqrMagnitude;
                if( Circumcircle.sqrRadius - dis > 0 &&j!=c[0]&&j!=c[1]&&j!=c[2]){
                    break;
                }
                else if( j==nodeCount-1 ){
                    circumcircles.Add( Circumcircle );
                    break;
                }
            }

        }

        for(int i=0;i<circumcircles.Count;i++){
            outnode.Add( circumcircles[i].center );
        }

    }

    public void GenerateVoronoy2()
    {

        //테셀레이션
        List<circumcircle> circumcirclesout = new List<circumcircle>();

        for(int i=0;i<circumcircles.Count;i++){
            Vector2[] circleNode=new Vector2[4]{ circumcircles[i].center,circumcircles[i].points[0],circumcircles[i].points[1],circumcircles[i].points[2] };

            Combination comb=new Combination(4,3);

            int nodeCount=outnode.Count;

            int[] c;

            while((c=comb.next())!=null)
            {
                circumcircle Circumcircle =new circumcircle(new Vector2[]{circleNode[c[0]],circleNode[c[1]],circleNode[c[2]]});

                //circumcirclesout.Add( Circumcircle );

                for(int j=0;j<nodeCount;j++){
                    float dis = ( outnode[j]-Circumcircle.center ).sqrMagnitude;
                    if( Circumcircle.sqrRadius - dis > 0.05f ){
                        break;
                    }
                    else if( j==nodeCount-1 ){
                        circumcirclesout.Add( Circumcircle );
                        break;
                    }
                }
                
            }
        }
        
        for(int i=0;i<circumcirclesout.Count;i++){
            outnode.Add( circumcirclesout[i].center );
        }

        circumcircles=circumcirclesout;

    }


    public void GenerateLinde()
    {
        //모든 지점으로 이각형 생성
        for(int i=0;i<circumcircles.Count;i++){
            for(int j=i+1;j<circumcircles.Count;j++)
            {
                Vector2 dis_0=(circumcircles[j].center-circumcircles[i].center);
                for(int k=0;k<3;k++)
                {
                    Vector2 dis_1=new Vector2();
                    for(int l=0;l<3;l++)
                    {
                        if((circumcircles[i].perpendicularBisector[k]-circumcircles[j].perpendicularBisector[l]).sqrMagnitude<0.01)
                        {
                            dis_1=(circumcircles[i].perpendicularBisector[k]-circumcircles[i].center).normalized;
                            break;
                        }
                    }
                    if(dis_1.sqrMagnitude>0&&dis_0.sqrMagnitude<1000000)
                    {
                        RoadList.Add(new Road(circumcircles[i].center,circumcircles[j].center));
                        break;
                    }
                }
            }
        }
    }

    
    //DrawVoronoiDiagram
    public void OnDrawGizmos() {
        
        Gizmos.color=Color.red;
        for(int i=0;i<circumcircles.Count;i++){

            Gizmos.DrawSphere( new Vector3(circumcircles[i].center.x,0,circumcircles[i].center.y ) , 2f );
        }

        Gizmos.color=Color.green;
        for(int i=0;i<RoadList.Count;i++){
            Gizmos.DrawLine( new Vector3( RoadList[i].start.x,0,RoadList[i].start.y ) , new Vector3( RoadList[i].end.x,0,RoadList[i].end.y ) );
        }
        
    }



    //외심원
    public class circumcircle{
        public Vector2 center;

        public float sqrRadius;
        
        public Vector2[] points=new Vector2[3];

        public Vector2[] perpendicularBisector=new Vector2[3];

        public circumcircle(Vector2[] points)
        {
            
            this.points=points;

            float A=points[1].x-points[0].x;
            float B=points[1].y-points[0].y;

            float C=points[2].x-points[0].x;
            float D=points[2].y-points[0].y;

            float E=A*(points[0].x+points[1].x)+B*(points[0].y+points[1].y);
            float F=C*(points[0].x+points[2].x)+D*(points[0].y+points[2].y);

            float G=2*(A*(points[2].y-points[1].y)-B*(points[2].x-points[1].x));

            //외심 지정
            center.x=(D*E-B*F)/G;
            center.y=(A*F-C*E)/G;


            //sqrRadius지정
            sqrRadius=Mathf.Pow(center.x-points[0].x,2)+Mathf.Pow(center.y-points[0].y,2);

            //중점 지점
            perpendicularBisector[0]=(points[1]+points[2])/2;//0
            perpendicularBisector[1]=(points[2]+points[0])/2;//1
            perpendicularBisector[2]=(points[0]+points[1])/2;//2

        }
    }

    public class Road
    {
        public Vector2 start;
        public Vector2 end;

        public Road(Vector2 start,Vector2 end)
        {
            this.start=start;
            this.end=end;
        }
    }

    //조합 알고리즘
    public class Combination
    {
        private int[] result;
        private Stack<int> stack;
        private int r, n;
    

        public Combination(int n, int r)
        {   // nCr 계산
            this.r = r;
            this.n = n;
            result = new int[r];
            stack = new Stack<int>();
            stack.Push(0);
        }
    

        public int[] next()
        {   // null 리턴하면 더 이상 없음
            while (stack.Count > 0)
            {
                int index = stack.Count - 1;
                int value = stack.Pop();
    

                while (value < n)
                {
                    result[index++] = value++;
                    stack.Push(value);
                    if (index == r)
                    {
                        return result;
                    }
                }
            }
            return null;
        }
    }

}
