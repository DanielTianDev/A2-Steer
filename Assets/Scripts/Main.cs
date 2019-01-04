using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

/*
 * 
 * Note this program assumes the map has the same number of characters per line, and won't handle files that dont meet this criteria.
 * 
 * Files are read in from a folder named: "_map_files", which exists at the root directory of this project.
 * The file is chosen by typing in the file path.
 * 
 * 
 * */
class Node
{
    public int X, Y, F, G, H;
    public Node Parent;
}

public class Path
{
    public List<Vector3> nodes = new List<Vector3>();

    public void AddNode(Vector3 node)
    {
        nodes.Add(node);
    }

    public List<Vector3> GetNodes()
    {
        return nodes;
    }
}


public class Main : MonoBehaviour {

    public GameObject floor, wall, start, goal;
    public Material successMaterial, progressMaterial;
    public bool animate;
    public float yieldTime = 0.1f;

    string[] Map; //data of map read in from text file
    GameObject[,] gameMap; //map of game objects used to visually represent the map text file
    Path path; //path if found
    List<Vector3> obstacles = new List<Vector3>();
    InputField fileNameInput;

    string[] TestMap = new string[] //x y = Map[y][x]
    {
        "                         X                 X               X",
        "                                G          X             XXX",
        "XXXXXXXXXXXXXXXX             XXXXXXXX              XXXX     ",
        "                                                            ",
        "                                                            ",
        "                                                           X",
        "             XXXXXXXXX  XXXXXXXXXXXXXX   XXXXXXXXXXXXXX    X",
        "XX       XXXXX          XXXXXXXXXXXXXX                     X",
        "                        XXXXXXXXXXXXXX                     X",
        "XX                                                          ",
        "                                                            ",
        "                                                            ",
        "S                                                           "
    };

    
    void Start () {
        fileNameInput = GameObject.Find("InputField").GetComponent<InputField>();
    }

    

    public void Init()
    {
        if (isRunning) return;  //if path finding is taking place, return

        if(gameMap!= null)  //if theres a game map, destroy it first
            foreach (var go in gameMap) Destroy(go);

        obstacles.Clear();

        string fileName = fileNameInput.text;

        if (fileName.Length == 0) return;

        if (!ReadFile(fileName))
        {
            print("please use a valid map file, or make sure that it exists.");
            return;
        }

        path = new Path();
        gameMap = new GameObject[Map.Length, Map[0].Length];
        SpawnMap();

        Node start = new Node();
        Node target = new Node();
        StartCoroutine(PathfindAStar(Map, start, target));
    }

    bool ReadFile(string fileName)
    {
        int counter = 0;
        string line;

        string dir = Directory.GetCurrentDirectory() + "\\_map_files\\" + fileName;

        print(dir);
        try
        {
            // Read the file and display it line by line.  
            System.IO.StreamReader file =
                new System.IO.StreamReader(dir); 

            List<string> lines = new List<string>();

            while ((line = file.ReadLine()) != null)
            {
                lines.Add(line);
                counter++;
            }


            file.Close();

            int firstLineLength = lines[0].Length;
            Map = new string[counter];

            counter = 0;
            foreach (var l in lines)
            {
                if(l.Length != firstLineLength) return false;
                
                Map[counter++] = l;
            }
        }
        catch(Exception e)
        {
            if(e.Message!= null)
                print("Error (perhaps the map file's dimensions are off): " + e.Message);
            return false;
        }

        return true;
       // print("number of lines: " + counter);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isRunning) return;
            foreach (var go in gameMap) Destroy(go);
            SpawnMap();
            Node start = new Node();
            Node target = new Node();
            StartCoroutine(PathfindAStar(Map, start, target));
        }
    }

    bool isRunning;
    IEnumerator PathfindAStar(string[] graph, Node start, Node target)
    {
        isRunning = true;
        bool pathFound = false;

        Node current = null;
        SetStartLocation(ref start);
        SetTargetLocation(ref target);

        //Initialize open and closed lists
        var openList = new List<Node>{start};
        var closedList = new List<Node>();

        //Iterate through processing each node
        while(openList.Count > 0)
        {
            //Find the smallest element in the open list (get square with lowest F score)
            int lowest = openList.Min(l => l.F);
            current = openList.First(l => l.F == lowest);

            openList.Remove(current);
            closedList.Add(current);

            
            gameMap[current.Y, current.X].GetComponent<MeshRenderer>().material = progressMaterial;
            if(animate) yield return new WaitForSeconds(yieldTime); //animate progress

            //If it is the goal node, then terminate
            if (current.X == target.X && current.Y == target.Y)
            {
                pathFound = true;
                break;
            }
            
            var connections = GetConnections(current.X, current.Y, Map); //Get outgoing connections of current node
            
            foreach (var endNode in connections) //Loop through each connection
            {
                //if this node is already in the closed list (visited)
                if (closedList.FirstOrDefault(l => l.X == endNode.X && l.Y == endNode.Y) != null) continue;

                if (openList.FirstOrDefault(l => l.X == endNode.X && l.Y == endNode.Y) == null)
                {
                    endNode.Parent = current;
                    endNode.G = 1 + current.G;
                    endNode.H = ComputeHScore(endNode.X, endNode.Y, target.X, target.Y);
                    endNode.F = endNode.G + endNode.H;
                    openList.Add(endNode);
                    
                }
            }
        }

        if (pathFound)
        {
            while (current != null)
            {
                path.AddNode(new Vector3(current.Y, 0, current.X));

                gameMap[current.Y, current.X].GetComponent<MeshRenderer>().material = successMaterial;
                yield return new WaitForSeconds(yieldTime);
                current = current.Parent;
            }

            path.nodes.Reverse();

            GameObject.Find("character").GetComponent<Kinematics>().path = this.path;
            GameObject.Find("character").GetComponent<Kinematics>().obstacles = this.obstacles;
            GameObject.Find("character").transform.position = new Vector3(start.Y, 0, start.X);
        }
        else
        {
            //NOT FOUND
            GameObject go = Instantiate(wall);
            go.transform.localScale = new Vector3(1, 10, 1);
            go.GetComponent<MeshRenderer>().material = progressMaterial;
            go.transform.position = new Vector3(current.Y, 5f, current.X); //x z 
            Destroy(go, 5f);
        }

        isRunning = false;
    }


    List<Node> GetConnections(int x, int y, string[] map)
    {
        var proposedNodes = new List<Node>(); //checks to see if there are available paths to the north, east, south and west
        int x1 = x, y1 = y - 1;
        int x2 = x, y2 = y + 1;
        int x3 = x - 1, y3 = y;
        int x4 = x + 1, y4 = y;

        if (IsIndexWithinBounds(x1, y1, map))
        {
            Node n = new Node { X = x1, Y = y1 };
            proposedNodes.Add(n);
        }

        if (IsIndexWithinBounds(x2, y2, map))
        {
            Node n = new Node { X = x2, Y = y2 };
            proposedNodes.Add(n);
        }
        if (IsIndexWithinBounds(x3, y3, map))
        {
            Node n = new Node { X = x3, Y = y3 };
            proposedNodes.Add(n);
        }
        if (IsIndexWithinBounds(x4, y4, map))
        {
            Node n = new Node { X = x4, Y = y4 };
            proposedNodes.Add(n);
        }


        return proposedNodes.Where(l => map[l.Y][l.X] == ' ' || map[l.Y][l.X] == 'G').ToList();
    }

    int ComputeHScore(int x, int y, int targetX, int targetY)
    {
        return Math.Abs(targetX - x) + Math.Abs(targetY - y);
    }

    void SetStartLocation(ref Node node)
    {
        for(int y = 0; y < Map.Length; y++)
        {
            for(int x = 0; x < Map[y].Length; x++)
            {
                if (Map[y][x] == 'S')
                {
                    node.X = x;
                    node.Y = y;
                }
            }
        }
    }

    void SetTargetLocation(ref Node node)
    {
        for (int y = 0; y < Map.Length; y++)
        {
            for (int x = 0; x < Map[y].Length; x++)
            {
                if (Map[y][x] == 'G')
                {
                    node.X = x;
                    node.Y = y;
                }
            }
        }
    }

    bool IsIndexWithinBounds(int x, int y, string[] map)
    {
        return (y < map.Length && y >= 0 && x >= 0 && x < map[y].Length);
    }



    void SpawnMap()
    {
  

        for(int y = 0; y < Map.Length; y++)
        {
            for(int x = 0; x < Map[y].Length; x++)
            {

                if(Map[y][x]==' ') //floor
                {
                    GameObject go = Instantiate(floor);
                    go.transform.position = new Vector3(y, 0 , x); //x z 
                    gameMap[y,x] = go;
                }else if(Map[y][x] == 'S') //Start
                {
                    GameObject go = Instantiate(start);
                    go.transform.position = new Vector3(y, 0, x); //x z 
                    gameMap[y, x] = go;
                }else if(Map[y][x] == 'G') //Goal
                {
                    GameObject go = Instantiate(goal);
                    go.transform.position = new Vector3(y, 0, x); //x z 
                    gameMap[y, x] = go;
                }
                else //wall
                {
                    GameObject go = Instantiate(wall);
                    go.transform.position = new Vector3(y, 0.45f, x); //x z 
                    obstacles.Add(new Vector3(y, 0, x));
                    gameMap[y, x] = go;
                    
                }

            }
        }

    }

}
