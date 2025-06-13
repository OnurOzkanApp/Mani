using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardTile
{
    // X and Y indices of the tile
    private int x;
    private int y;

    // Flag to check if the tile is empty
    private bool isEmpty;
    // Game object inside the Board Tile
    private GameObject objectInside;

    /// <summary>
    /// Initializes a new Board Tile with given coordinates, occupancy state, and object.
    /// </summary>
    /// <param name="x">The X index of the tile.</param>
    /// <param name="y">The Y index of the tile.</param>
    /// <param name="isEmpty">The flag for whether the tile is empty or not.</param>
    /// <param name="objectInside">The GameObject placed inside the tile, if any.</param>
    public BoardTile(int x, int y, bool isEmpty, GameObject objectInside)
    {
        this.x = x;
        this.y = y;
        this.isEmpty = isEmpty;
        this.objectInside = objectInside;
    }

    /// <summary>
    /// Changes the empty state of the Board Tile as empty if true is given, and full if false is given.
    /// </summary>
    /// <param name="isEmpty">True if the tile should be marked as empty; false if it should be full.</param>
    private void SetEmpty(bool isEmpty)
    {
        this.isEmpty = isEmpty;
    }

    /// <summary>
    /// Returns the empty state of the Board Tile.
    /// </summary>
    public bool GetEmpty()
    {
        return isEmpty;
    }

    /// <summary>
    /// Puts the given Game Object inside the Board Tile and updates the tile as not empty.
    /// </summary>
    /// <param name="objectInside">The Game Object to put inside the Board Tile.</param>
    public void SetObjectInside(GameObject objectInside)
    {
        this.objectInside = objectInside;
        SetEmpty(false);
    }

    /// <summary>
    /// Removes the object inside the Board Tile and updates the tile as empty.
    /// </summary>
    public void RemoveObjectInside()
    {
        this.objectInside = null;
        SetEmpty(true);
    }

    /// <summary>
    /// Returns the object inside the Board Tile.
    /// </summary>
    public GameObject GetObjectInside()
    {
        return objectInside;
    }
}
