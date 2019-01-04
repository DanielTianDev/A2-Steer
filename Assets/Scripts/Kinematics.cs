using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Kinematics : MonoBehaviour {

    public Transform target;
    public Path path; //assigned from main program when path is found.
    public List<Vector3> obstacles = new List<Vector3>();

    public float mass = 50f;
    
    public float max_force = 10.4f, max_speed = 3.0f;
    public float pathRadius = .1f;
    public float slowingRadius = 5f;

    //collision avoidancee
    public float MAX_SEE_AHEAD = 5.0f;


    public int currentNode;
    public float obstacleRadius = 1.0f;

    int pathDir = 1;
    Vector3 velocity;
    Vector3 ahead, ahead2;


    void Start()
    {
        StartCoroutine(WaitForPath());
    }


    Vector3 CollisionAvoidance()
    {
        float dynamic_length = velocity.magnitude / max_speed;
        ahead = transform.position + Vector3.Normalize(velocity) * dynamic_length;
        //ahead = transform.position + Vector3.Normalize(velocity) * MAX_SEE_AHEAD;
        ahead2 = transform.position + Vector3.Normalize(velocity) * MAX_SEE_AHEAD * 0.5f;


        var mostThreateningObstacle = FindMostThreateningObstacle();
        var avoidance = Vector3.zero;

        if(mostThreateningObstacle.magnitude != 0){ //means we found an obstacle in line of sight
            //should be center.x as they are spheres, but for simpler purposes the x coordinate shall do.
            avoidance = ahead - mostThreateningObstacle; 

            avoidance = Vector3.Normalize(avoidance);
            avoidance = Vector3.Scale(avoidance, new Vector3(MAX_SEE_AHEAD, MAX_SEE_AHEAD, MAX_SEE_AHEAD));
        }
        else
        {
            avoidance = Vector3.Scale(avoidance, Vector3.zero); //nullify the avoidance force
        }

        return avoidance;
    }

    Vector3 FindMostThreateningObstacle()
    {
        Vector3 mostThreatening = Vector3.zero;

        for(int i = 0; i < obstacles.Count; i++)
        {
            bool collision = LineIntersectsCircle(ahead, ahead2, obstacles[i]);

            //position is this object's current position
            if(collision && (mostThreatening.magnitude == 0 || Vector3.Distance(transform.position, obstacles[i]) < Vector3.Distance(transform.position, mostThreatening)))
            {
                mostThreatening = obstacles[i];
            }
        }

        return mostThreatening;
    }

    //obstacle radiuses
    bool LineIntersectsCircle(Vector3 ahead, Vector3 ahead2, Vector3 obstacle)
    {
        return (Vector3.Distance(obstacle, ahead) <= obstacleRadius || Vector3.Distance(obstacle, ahead2) <= obstacleRadius);
    }

    Vector3 Seek(Vector3 _target)
    {
        Vector3 desiredVelocity = Vector3.Normalize(_target - transform.position) * max_speed;
        Vector3 steering = desiredVelocity - velocity;
        steering = Vector3.ClampMagnitude(steering, max_force);
        steering /= mass;
        velocity = Vector3.ClampMagnitude(velocity + steering, max_speed);
        return velocity;
    }


    
    Vector3 Arrive(Vector3 _target)
    {
        
        Vector3 desiredVelocity = Vector3.Normalize(_target - transform.position) * max_speed;
        float distance = desiredVelocity.magnitude;

        if(distance < slowingRadius)
        
            desiredVelocity = Vector3.Normalize(desiredVelocity) * max_speed * (distance / slowingRadius);
        else
            desiredVelocity = Vector3.Normalize(desiredVelocity) * max_speed;
        
        return desiredVelocity - velocity;

    }


    public Vector3 FollowPath(Path path)
    {
        Vector3 target = Vector3.zero;

        if (path != null)
        {
            var nodes = path.GetNodes();

            target = nodes[currentNode];
            transform.LookAt(target);

            if (Vector3.Distance(transform.position, target) <= pathRadius)
            {
                currentNode += pathDir;

                if (currentNode >= nodes.Count || currentNode < 0)
                {
                    pathDir *= -1;
                    currentNode += pathDir;
                }
            }
        }
        
       return (target.magnitude != 0) ? Arrive(target) : Vector3.zero;
    }


    IEnumerator WaitForPath()   //basically an update loop
    {
        while (path==null) yield return new WaitForSeconds(0.1f);

        while (path!=null)
        {
            Vector3 steering = Vector3.zero;
            steering += FollowPath(path);
            steering += CollisionAvoidance();
            //steering += Arrive(arriveTarget);

            steering = Vector3.ClampMagnitude(steering, max_force);
            steering /= mass;

            velocity = Vector3.ClampMagnitude(velocity + steering, max_speed);
            transform.position += velocity;

            yield return null;
        }
    }

}