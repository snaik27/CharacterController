using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class GridSpawn : MonoBehaviour
{

    public Vector2 gridDimensions;
    public List<Transform> gridTransforms;

    private int sideSize = 4;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ClearGrid()
    {
        gridTransforms = new List<Transform>();

        foreach (Transform child in transform.parent)
        {
            if (child.transform.position != transform.position)
                DestroyImmediate(child.gameObject);
        }
        
    }

    public void SpawnGrid()
    {
        for (int i = 0; i < gridDimensions.x; i++)
        {
            for (int j = 0; j < gridDimensions.y; j++)
            {
                Transform newOne = Instantiate(this.transform, new Vector3(transform.position.x + i*sideSize, transform.position.y, transform.position.z + j*sideSize), Quaternion.identity, this.transform.parent);
                gridTransforms.Add(newOne);
            }
        }
    }
}
